using System;
using System.Collections.Generic;
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
                self.beginDispatch(self.CharacterMotors, self.PhysicsMovers);
                try
                {
                    if (self.Settings.Interpolate)
                    {
                        self.preSimulationInterpolationUpdate(self.MotorSnapshot, self.MoverSnapshot);
                    }

                    self.simulate(deltaTime, self.MotorSnapshot, self.MoverSnapshot);

                    if (!self.IsDisposed && self.Settings.Interpolate)
                    {
                        self.postSimulationInterpolationUpdate(deltaTime, self.MotorSnapshot, self.MoverSnapshot);
                    }
                }
                finally
                {
                    self.endDispatch();
                }
            }
        }
        
        private static void LateUpdate(this KinematicMoverManager self)
        {
            if (self.isLevelEnabled() && self.Settings.Interpolate)
            {
                self.beginDispatch(self.CharacterMotors, self.PhysicsMovers);
                try
                {
                    self.CustomInterpolationUpdate(self.MoverSnapshot);
                }
                finally
                {
                    self.endDispatch();
                }
            }
        }

        private static void beginDispatch(this KinematicMoverManager self,
            IndexList<long, EntityRef<KinematicCharacterComponent>> motors,
            IndexList<long, EntityRef<KinematicRigidbodyMover>> movers)
        {
            if (self.IsDispatching)
            {
                throw new InvalidOperationException("Kinematic manager dispatch cannot be reentered.");
            }

            self.IsDispatching = true;
            self.MotorSnapshot.AddRange(motors);
            self.MoverSnapshot.AddRange(movers);
        }

        private static void endDispatch(this KinematicMoverManager self)
        {
            self.MotorSnapshot.Clear();
            self.MoverSnapshot.Clear();
            self.IsDispatching = false;
        }

        private static bool isLevelEnabled(this KinematicMoverManager self)
        {
            LogicScene scene = self.Scene();
            return scene.SceneType != ELogicSceneType.Level ||
                scene.GetComponent<LevelGameWorldComponent>().IsEnabled;
        }

        private static bool isParticipantEnabled(Entity participant)
        {
            if (participant == null)
            {
                return false;
            }

            if (participant is KinematicCharacterComponent motor && motor.RegisteredInstanceId != motor.InstanceId ||
                participant is KinematicRigidbodyMover mover && mover.RegisteredInstanceId != mover.InstanceId)
            {
                return false;
            }

            LogicScene scene = participant.Scene();
            return scene.SceneType != ELogicSceneType.Level ||
                scene.GetComponent<LevelGameWorldComponent>().IsEnabled;
        }

        private static bool isMotorEnabled(KinematicCharacterComponent motor)
        {
            if (!isParticipantEnabled(motor))
            {
                return false;
            }

#if Client
            NetRoleComponent role = motor.Parent.GetComponent<NetRoleComponent>();
            if (role != null && role.LocalNetRole == ENetRole.SimulatedProxy)
            {
                return false;
            }
#endif
            return true;
        }

        /// <summary>Registers a live character motor for subsequent manager dispatches.</summary>
        /// <param name="self">The manager owning the registration.</param>
        /// <param name="motor">The motor to register.</param>
        public static void RegisterCharacterMotor(this KinematicMoverManager self, KinematicCharacterComponent motor)
        {
            motor.RegisteredInstanceId = motor.InstanceId;
            self.CharacterMotors.Add(motor.InstanceId, motor);
        }

        /// <summary>Unregisters a motor and excludes it from any in-progress dispatch.</summary>
        /// <param name="self">The manager owning the registration.</param>
        /// <param name="motor">The motor to unregister.</param>
        public static void UnregisterCharacterMotor(this KinematicMoverManager self, KinematicCharacterComponent motor)
        {
            if (self.IsDispatching && !motor.IsDisposed)
            {
                for (int index = 0; index < self.MotorSnapshot.Count; index++)
                {
                    if ((KinematicCharacterComponent)self.MotorSnapshot[index] == motor)
                    {
                        self.MotorSnapshot[index] = default;
                    }
                }
            }
            self.CharacterMotors.Remove(motor.RegisteredInstanceId);
            motor.RegisteredInstanceId = default;
        }

        /// <summary>Registers a live physics mover for subsequent manager dispatches.</summary>
        /// <param name="self">The manager owning the registration.</param>
        /// <param name="mover">The mover to register.</param>
        public static void RegisterPhysicsMover(this KinematicMoverManager self, KinematicRigidbodyMover mover)
        {
            mover.RegisteredInstanceId = mover.InstanceId;
            self.PhysicsMovers.Add(mover.InstanceId, mover);
        }

        /// <summary>Unregisters a mover and excludes it from any in-progress dispatch.</summary>
        /// <param name="self">The manager owning the registration.</param>
        /// <param name="mover">The mover to unregister.</param>
        public static void UnregisterPhysicsMover(this KinematicMoverManager self, KinematicRigidbodyMover mover)
        {
            if (self.IsDispatching && !mover.IsDisposed)
            {
                for (int index = 0; index < self.MoverSnapshot.Count; index++)
                {
                    if ((KinematicRigidbodyMover)self.MoverSnapshot[index] == mover)
                    {
                        self.MoverSnapshot[index] = default;
                    }
                }
            }
            self.PhysicsMovers.Remove(mover.RegisteredInstanceId);
            mover.RegisteredInstanceId = default;
        }

        /// <summary>Captures interpolation starting poses for the current participants.</summary>
        /// <param name="self">The manager owning the participants.</param>
        /// <param name="deltaTime">The simulation step duration in seconds.</param>
        /// <exception cref="InvalidOperationException">The manager is already dispatching.</exception>
        public static void PreSimulationInterpolationUpdate(this KinematicMoverManager self, float deltaTime)
        {
            self.beginDispatch(self.CharacterMotors, self.PhysicsMovers);
            try
            {
                self.preSimulationInterpolationUpdate(self.MotorSnapshot, self.MoverSnapshot);
            }
            finally
            {
                self.endDispatch();
            }
        }

        private static void preSimulationInterpolationUpdate(this KinematicMoverManager self,
            List<EntityRef<KinematicCharacterComponent>> motors, List<EntityRef<KinematicRigidbodyMover>> movers)
        {
            for (int index = 0; index < motors.Count; index++)
            {
               if (self.IsDisposed) return;
               var motor = (KinematicCharacterComponent)motors[index];
               if (!isMotorEnabled(motor))
               {
                   continue;
               }
               
               motor.InitialTickPosition = motor.TransientPosition;
               motor.InitialTickRotation = motor.TransientRotation;
               
               using var scopedPreventMoveView = ScopedPreventViewMove.Create(motor.TransformComponent);
               motor.TransformComponent.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation, false);
            }

            for (int index = 0; index < movers.Count; index++)
            {
                if (self.IsDisposed) return;
                var mover =  (KinematicRigidbodyMover)movers[index];
                if (!isParticipantEnabled(mover))
                {
                    continue;
                }
                
                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;
                var transformCmp = (TransformComponent)mover.Transform; 
                
                using var scopedPreventMoveView = ScopedPreventViewMove.Create(transformCmp);
                transformCmp.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation, false);
                if (self.IsDisposed || !isParticipantEnabled((KinematicRigidbodyMover)movers[index]))
                {
                    continue;
                }
                
                var moverRigidBody = (GameplayRigidbody)mover.Rigidbody;
                moverRigidBody.Position = mover.TransientPosition;
                moverRigidBody.Rotation = mover.TransientRotation;
            }
        }

        /// <summary>Simulates a stable snapshot of the supplied motors and movers.</summary>
        /// <param name="self">The manager dispatching the simulation.</param>
        /// <param name="deltaTime">The simulation step duration in seconds.</param>
        /// <param name="motors">Character motors keyed by their registered instance identifier.</param>
        /// <param name="movers">Physics movers keyed by their registered instance identifier.</param>
        /// <exception cref="InvalidOperationException">The manager is already dispatching.</exception>
        public static void Simulate(this KinematicMoverManager self, float deltaTime, IndexList<long, EntityRef<KinematicCharacterComponent>> motors, IndexList<long, EntityRef<KinematicRigidbodyMover>> movers)
        {
            self.beginDispatch(motors, movers);
            try
            {
                self.simulate(deltaTime, self.MotorSnapshot, self.MoverSnapshot);
            }
            finally
            {
                self.endDispatch();
            }
        }

        private static void simulate(this KinematicMoverManager self, float deltaTime,
            List<EntityRef<KinematicCharacterComponent>> motors, List<EntityRef<KinematicRigidbodyMover>> movers)
        {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;
            
            // Update PhysicsMover velocities
            for (int i = 0; i < physicsMoversCount; i++)
            {
                if (self.IsDisposed) return;
                var mover = (KinematicRigidbodyMover)movers[i];
                if (!isParticipantEnabled(mover))
                {
                    continue;
                }
                mover.VelocityUpdate(deltaTime);
            }
            
            // Character controller update phase 1
            for (int i = 0; i < characterMotorsCount; i++)
            {
                if (self.IsDisposed) return;
                var motor = (KinematicCharacterComponent)motors[i];
                if (!isMotorEnabled(motor))
                {
                    continue;
                }
                motor.UpdatePhase1(deltaTime);
            }
            
            bool interpolateMover = self.Settings.Interpolate && self.Settings.InterpolateMover;
            // Simulate PhysicsMover displacement
            for (int i = 0; i < physicsMoversCount; i++)
            {
                if (self.IsDisposed) return;
                var mover = (KinematicRigidbodyMover)movers[i];
                if (!isParticipantEnabled(mover))
                {
                    continue;
                }
                var transformCmp = (TransformComponent)mover.Transform;
                transformCmp.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation, !interpolateMover);
                if (self.IsDisposed || !isParticipantEnabled((KinematicRigidbodyMover)movers[i]))
                {
                    continue;
                }
                
                var moverRigidBody = (GameplayRigidbody)mover.Rigidbody;
                moverRigidBody.Position = mover.TransientPosition;
                moverRigidBody.Rotation = mover.TransientRotation;
            }
            
            bool interpolateMotor = self.Settings.Interpolate && self.Settings.InterpolateMotor;
            // Character controller update phase 2 and move
            for (int i = 0; i < characterMotorsCount; i++)
            {
                if (self.IsDisposed) return;
                var motor = (KinematicCharacterComponent)motors[i];
                if (!isMotorEnabled(motor))
                {
                    continue;
                }
                motor.UpdatePhase2(deltaTime);
                if (self.IsDisposed || !isMotorEnabled((KinematicCharacterComponent)motors[i]))
                {
                    continue;
                }
                using var scopedPreventMoveView = ScopedPreventViewMove.Create(!interpolateMotor ? null : motor.TransformComponent);
               motor.TransformComponent.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation, !interpolateMotor);
#if DOTNET
                motor.syncPhysicsPose(motor.TransientPosition, motor.TransientRotation);
#endif
            }
        }

        /// <summary>Publishes the current participants' interpolation targets after simulation.</summary>
        /// <param name="self">The manager owning the participants.</param>
        /// <param name="deltaTime">The simulation step duration in seconds.</param>
        /// <exception cref="InvalidOperationException">The manager is already dispatching.</exception>
        public static void PostSimulationInterpolationUpdate(this KinematicMoverManager self, float deltaTime)
        {
            self.beginDispatch(self.CharacterMotors, self.PhysicsMovers);
            try
            {
                self.postSimulationInterpolationUpdate(deltaTime, self.MotorSnapshot, self.MoverSnapshot);
            }
            finally
            {
                self.endDispatch();
            }
        }

        private static void postSimulationInterpolationUpdate(this KinematicMoverManager self, float deltaTime,
            List<EntityRef<KinematicCharacterComponent>> motors, List<EntityRef<KinematicRigidbodyMover>> movers)
        {
            self.LastCustomInterpolationStartTime = TimeInfo.Instance.ClientNow();
            self.LastCustomInterpolationDeltaTime = deltaTime;
            
            for (int i = 0; i < motors.Count; i++)
            {
                if (self.IsDisposed) return;
                var motor = (KinematicCharacterComponent)motors[i];
                if (!isMotorEnabled(motor))
                {
                    continue;
                }
                
                motor.SmoothTransformComponent.SetLogicTransform(motor.CharacterMovementController.GetLogicTimeStamp(), motor.Velocity,
                    motor.InitialTickPosition, motor.InitialTickRotation, 
                    motor.TransientPosition , motor.TransientRotation);
                
                // using var scopedPreventMoveView = ScopedPreventViewMove.Create(motor.TransformComponent);
                // motor.TransformComponent.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation, false);
            }

            for (int i = 0; i < movers.Count; i++)
            {
                if (self.IsDisposed) return;
                var mover = (KinematicRigidbodyMover)movers[i];
                if (!isParticipantEnabled(mover))
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

        private static void CustomInterpolationUpdate(this KinematicMoverManager self, List<EntityRef<KinematicRigidbodyMover>> movers)
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
                for (int i = 0; i < movers.Count; i++)
                {
                    if (self.IsDisposed) return;
                    var mover = (KinematicRigidbodyMover)movers[i];
                    if (!isParticipantEnabled(mover))
                    {
                        continue;
                    }
                
                    var transformCmp = (TransformComponent)mover.Transform;
                    transformCmp.SetPositionAndRotation(
                        math.lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                        math.slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));
                    if (self.IsDisposed || !isParticipantEnabled((KinematicRigidbodyMover)movers[i]))
                    {
                        continue;
                    }

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