using System;
using Bliss.Base.Collections;
using Bliss.Core;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    [EntitySystemOf(typeof(KinematicMoverManager))]
    public static partial class KinematicMoverManagerSystems
    {
        private static readonly Action<Entity, float> logicUpdateCallback = dispatchLogicUpdate;

        private static void Awake(this KinematicMoverManager self, KinematicMoverManagerSettings settings)
        {
            self.Enable = false;
            self.Settings = settings;
            self.CharacterMotors.Clear();
            self.PhysicsMovers.Clear();
            self.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, logicUpdateCallback);
        }

        private static void dispatchLogicUpdate(Entity entity, float deltaTime)
        {
            ((KinematicMoverManager)entity).logicUpdate(deltaTime);
        }

        private static void Destroy(this KinematicMoverManager self)
        {
            self.CharacterMotors.Clear();
            self.PhysicsMovers.Clear();
        }

        private static void logicUpdate(this KinematicMoverManager self, float deltaTime)
        {
            if (!self.isLevelEnabled())
            {
                return;
            }

            if (self.Settings.AutoSimulation && self.Enable)
            {
                if (self.Settings.Interpolate)
                {
                    self.PreSimulationInterpolationUpdate(deltaTime);
                }
                
                self.Simulate(deltaTime, self.CharacterMotors, self.PhysicsMovers);

                if (self.Settings.Interpolate)
                {
                    self.PostSimulationInterpolationUpdate(deltaTime);
                }
            }
        }
        
        private static void LateUpdate(this KinematicMoverManager self)
        {
            if (self.isLevelEnabled() && self.Settings.Interpolate)
            {
                self.CustomInterpolationUpdate();
            }
        }

        private static bool isLevelEnabled(this KinematicMoverManager self)
        {
            LogicScene scene = self.Scene();
            return scene.SceneType != ELogicSceneType.Level ||
                scene.GetComponent<LevelGameWorldComponent>().IsEnabled;
        }

        /// <summary>
        /// Registers a KinematicCharacterMotor into the system
        /// </summary>
        public static void RegisterCharacterMotor(this KinematicMoverManager self, KinematicCharacterComponent motor)
        {
            self.CharacterMotors.Add(motor.InstanceId, motor);
        }

        /// <summary>
        /// Unregisters a KinematicCharacterMotor from the system
        /// </summary>
        public static void UnregisterCharacterMotor(this KinematicMoverManager self, KinematicCharacterComponent motor)
        {
            self.CharacterMotors.Remove(motor.InstanceId);
        }

        /// <summary>
        /// Registers a PhysicsMover into the system
        /// </summary>
        public static void RegisterPhysicsMover(this KinematicMoverManager self, KinematicRigidbodyMover mover)
        {
            self.PhysicsMovers.Add(mover.InstanceId, mover);
        }

        /// <summary>
        /// Unregisters a PhysicsMover from the system
        /// </summary>
        public static void UnregisterPhysicsMover(this KinematicMoverManager self, KinematicRigidbodyMover mover)
        {
            self.PhysicsMovers.Remove(mover.InstanceId);
        }

        public static void PreSimulationInterpolationUpdate(this KinematicMoverManager self, float deltaTime)
        {
            foreach (var motorRef in self.CharacterMotors)
            {
               var motor = (KinematicCharacterComponent)motorRef;
               if (motor == null)
               {
                   continue;
               }
               
               motor.InitialTickPosition = motor.TransientPosition;
               motor.InitialTickRotation = motor.TransientRotation;
               
               using var scopedPreventMoveView = ScopedPreventViewMove.Create(motor.TransformComponent);
               motor.TransformComponent.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation, false);
            }

            foreach (var moverRef in self.PhysicsMovers)
            {
                var mover =  (KinematicRigidbodyMover)moverRef;
                if (mover == null)
                {
                    continue;
                }
                
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;
                var transformCmp = (TransformComponent)mover.Transform; 
                
                using var scopedPreventMoveView = ScopedPreventViewMove.Create(transformCmp);
                transformCmp.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation, false);
                
                var moverRigidBody = (GameplayRigidbody)mover.Rigidbody;
                moverRigidBody.Position = mover.TransientPosition;
                moverRigidBody.Rotation = mover.TransientRotation;
            }
        }

        public static void Simulate(this KinematicMoverManager self, float deltaTime, IndexList<long, EntityRef<KinematicCharacterComponent>> motors, IndexList<long, EntityRef<KinematicRigidbodyMover>> movers)
        {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;
            
            // Update PhysicsMover velocities
            for (int i = 0; i < physicsMoversCount; i++)
            {
                var mover = (KinematicRigidbodyMover)movers[i];
                if (mover == null)
                {
                    continue;
                }
                mover.VelocityUpdate(deltaTime);
            }
            
            // Character controller update phase 1
            for (int i = 0; i < characterMotorsCount; i++)
            {
                var motor = (KinematicCharacterComponent)motors[i];
                if (motor == null)
                {
                    continue;
                }
                motor.UpdatePhase1(deltaTime);
            }
            
            bool interpolateMover = self.Settings.Interpolate && self.Settings.InterpolateMover;
            // Simulate PhysicsMover displacement
            for (int i = 0; i < physicsMoversCount; i++)
            {
                var mover = (KinematicRigidbodyMover)movers[i];
                if (mover == null)
                {
                    continue;
                }
                var transformCmp = (TransformComponent)mover.Transform;
                transformCmp.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation, !interpolateMover);
                
                var moverRigidBody = (GameplayRigidbody)mover.Rigidbody;
                moverRigidBody.Position = mover.TransientPosition;
                moverRigidBody.Rotation = mover.TransientRotation;
            }
            
            bool interpolateMotor = self.Settings.Interpolate && self.Settings.InterpolateMotor;
            // Character controller update phase 2 and move
            for (int i = 0; i < characterMotorsCount; i++)
            {
                var motor = (KinematicCharacterComponent)motors[i];
                if (motor == null)
                {
                    continue;
                }
                motor.UpdatePhase2(deltaTime);
                using var scopedPreventMoveView = ScopedPreventViewMove.Create(!interpolateMotor ? null : motor.TransformComponent);
               motor.TransformComponent.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation, !interpolateMotor);
            }
        }

        public static void PostSimulationInterpolationUpdate(this KinematicMoverManager self, float deltaTime)
        {
            self.LastCustomInterpolationStartTime = TimeInfo.Instance.ClientNow();
            self.LastCustomInterpolationDeltaTime = deltaTime;
            
            for (int i = 0; i < self.CharacterMotors.Count; i++)
            {
                var motor = (KinematicCharacterComponent)self.CharacterMotors[i];
                if (motor == null)
                {
                    continue;
                }
                
                motor.SmoothTransformComponent.SetLogicTransform(motor.CharacterMovementController.GetLogicTimeStamp(), motor.Velocity,
                    motor.InitialTickPosition, motor.InitialTickRotation, 
                    motor.TransientPosition , motor.TransientRotation);
                
                // using var scopedPreventMoveView = ScopedPreventViewMove.Create(motor.TransformComponent);
                // motor.TransformComponent.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation, false);
            }

            for (int i = 0; i < self.PhysicsMovers.Count; i++)
            {
                var mover = (KinematicRigidbodyMover)self.PhysicsMovers[i];
                if (mover == null)
                {
                    continue;
                }

                var moverRigidBody = (GameplayRigidbody)mover.Rigidbody;
                if (mover.MoveWithPhysics)
                {
                    moverRigidBody.Position = mover.InitialTickPosition;
                    moverRigidBody.Rotation = mover.InitialTickRotation;

                    moverRigidBody.MovePosition(mover.TransientPosition);
                    moverRigidBody.MoveRotation(mover.TransientRotation);
                }
                else
                {
                    moverRigidBody.Position = (mover.TransientPosition);
                    moverRigidBody.Rotation = (mover.TransientRotation);
                }
            }
        }

        private static void CustomInterpolationUpdate(this KinematicMoverManager self)
        {
            float interpolationFactor = math.clamp((TimeInfo.Instance.ClientNow() - self.LastCustomInterpolationStartTime) / (1000f * self.LastCustomInterpolationDeltaTime), 0f, 1f);

            /*
            if (self.Settings.InterpolateMotor)
            {
                for (int i = 0; i < self.CharacterMotors.Count; i++)
                {
                    var motor = (KinematicCharacterComponent)self.CharacterMotors[i];
                    if (motor == null)
                    {
                        continue;
                    }
                    
                    motor.TransformComponent.SetPositionAndRotation(
                        math.lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor), 
                        math.slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
                }
            }
            */


            if (self.Settings.InterpolateMover)
            {
                for (int i = 0; i < self.PhysicsMovers.Count; i++)
                {
                    var mover = (KinematicRigidbodyMover)self.PhysicsMovers[i];
                    if (mover == null)
                    {
                        continue;
                    }
                
                    var transformCmp = (TransformComponent)mover.Transform;
                    transformCmp.SetPositionAndRotation(
                        math.lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                        math.slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));


                    float3 newPosition = transformCmp.Transform.Location;
                    quaternion newRotation = transformCmp.Transform.Rotation;
                    mover.PositionDeltaFromInterpolation = newPosition - mover.LatestInterpolationPosition;
                    mover.RotationDeltaFromInterpolation = math.mul(math.inverse(mover.LatestInterpolationRotation), newRotation);
                    mover.LatestInterpolationPosition = newPosition;
                    mover.LatestInterpolationRotation = newRotation;
                }
            }
        }
    }
}