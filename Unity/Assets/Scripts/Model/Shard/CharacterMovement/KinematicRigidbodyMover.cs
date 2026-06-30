using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public class KinematicRigidbodyMover : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// The mover's Rigidbody
        /// </summary>
        public EntityRef<GameplayRigidbody> Rigidbody;

        public EntityRef<TransformComponent> Transform;
        
        /// <summary>
        /// Determines if the platform moves with rigidbody.MovePosition (when true), or with rigidbody.position (when false)
        /// </summary>
        public bool MoveWithPhysics = true;
        
        public IMoverController MoverController;
        
        /// <summary>
        /// Remembers latest position in interpolation
        /// </summary>
        public float3 LatestInterpolationPosition;
        /// <summary>
        /// Remembers latest rotation in interpolation
        /// </summary>
        public quaternion LatestInterpolationRotation;
        /// <summary>
        /// The latest movement made by interpolation
        /// </summary>
        public float3 PositionDeltaFromInterpolation;
        /// <summary>
        /// The latest rotation made by interpolation
        /// </summary>
        public quaternion RotationDeltaFromInterpolation;
        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public float3 InitialTickPosition { get; set; }
        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        public quaternion InitialTickRotation { get; set; }
        /// <summary>
        /// The Mover's position before the movement calculations began
        /// </summary>
        public float3 InitialSimulationPosition { get; set; }
        /// <summary>
        /// The Mover's rotation before the movement calculations began
        /// </summary>
        public quaternion InitialSimulationRotation { get; set; }
        
        public float3 Velocity = float3.zero;
        public float3 AngularVelocity = float3.zero;


        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public float3 TransientPosition = float3.zero;

        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public quaternion TransientRotation = quaternion.identity;
    }
    
    public struct PhysicsMoverState
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 Velocity;
        public float3 AngularVelocity;
    }
}