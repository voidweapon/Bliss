using Bliss.Core;
using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    [EntitySystemOf(typeof(KinematicRigidbodyMover))]
    public static partial class KinematicRigidbodyMoverSystems
    {
            
        static void Awake(this KinematicRigidbodyMover self)
        {
            self.Rigidbody = self.Parent.GetComponent<GameplayRigidbody>();
            self.Transform = self.Parent.GetComponent<TransformComponent>();
            var transform = self.GetParentTransform();

            self.TransientPosition = transform.Location;
            self.TransientRotation = transform.Rotation;
            self.InitialSimulationPosition = self.TransientPosition;
            self.InitialSimulationRotation = self.TransientRotation;
            self.LatestInterpolationPosition = transform.Location;
            self.LatestInterpolationRotation = transform.Rotation;
            
            var kinematicMoverManager = self.GetLevelGameplayWorld()?.GetComponent<KinematicMoverManager>();
            kinematicMoverManager?.RegisterPhysicsMover(self);
        }
        private static void Destroy(this KinematicRigidbodyMover self)
        {
            self.Transform = default;
            
            self.LatestInterpolationPosition = float3.zero;
            self.LatestInterpolationRotation = quaternion.identity;
            
            var kinematicMoverManager = self.GetLevelGameplayWorld()?.GetComponent<KinematicMoverManager>();
            kinematicMoverManager?.UnregisterPhysicsMover(self);
        }
        
        /// <summary>
        /// Sets the mover's position directly
        /// </summary>
        public static void SetPosition(this KinematicRigidbodyMover self, float3 position)
        {
            var transformCmp = (TransformComponent)self.Transform;
            transformCmp.SetPosition(position);
            //Rigidbody.position = position;
            self.InitialSimulationPosition = position;
            self.TransientPosition = position;
        }

        /// <summary>
        /// Sets the mover's rotation directly
        /// </summary>
        public static void SetRotation(this KinematicRigidbodyMover self, quaternion rotation)
        {
            var transformCmp = (TransformComponent)self.Transform;
            transformCmp.SetRotation(rotation);
            //Rigidbody.rotation = rotation;
            self.InitialSimulationRotation = rotation;
            self.TransientRotation = rotation;
        }

        /// <summary>
        /// Sets the mover's position and rotation directly
        /// </summary>
        public static void SetPositionAndRotation(this KinematicRigidbodyMover self, float3 position, quaternion rotation)
        {
            var transformCmp = (TransformComponent)self.Transform;
            transformCmp.SetPositionAndRotation(position, rotation);
            // Rigidbody.position = position;
            // Rigidbody.rotation = rotation;
            self.InitialSimulationPosition = position;
            self.InitialSimulationRotation = rotation;
            self.TransientPosition = position;
            self.TransientRotation = rotation;
        }
        
        /// <summary>
        /// Returns all the state information of the mover that is pertinent for simulation
        /// </summary>
        public static  PhysicsMoverState GetState(this KinematicRigidbodyMover self)
        {
            PhysicsMoverState state = new PhysicsMoverState();

            state.Position = self.TransientPosition;
            state.Rotation = self.TransientRotation;
            state.Velocity = self.Velocity;
            state.AngularVelocity = self.AngularVelocity;

            return state;
        }
        
        /// <summary>
        /// Applies a mover state instantly
        /// </summary>
        public static void ApplyState(this KinematicRigidbodyMover self, PhysicsMoverState state)
        {
           self.SetPositionAndRotation(state.Position, state.Rotation);
           self.Velocity = state.Velocity;
           self.AngularVelocity = state.AngularVelocity;
        }
        
        /// <summary>
        /// Caches velocity values based on deltatime and target position/rotations
        /// </summary>
        public static void VelocityUpdate(this KinematicRigidbodyMover self, float deltaTime)
        {
            self.InitialSimulationPosition = self.TransientPosition;;
            self.InitialSimulationRotation = self.TransientRotation;

            self.MoverController.UpdateMovement(out self.TransientPosition, out self.TransientRotation, deltaTime);

            if (deltaTime > 0f)
            {
                self.Velocity = (self.TransientPosition - self.InitialSimulationPosition) / deltaTime;

                quaternion rotationFromCurrentToGoal = math.mul(self.TransientRotation, (math.inverse(self.InitialSimulationRotation)));
                self.AngularVelocity = (math.radians(math.Euler(rotationFromCurrentToGoal))) / deltaTime;
            }
        }
    }
}