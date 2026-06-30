using Unity.Mathematics;

namespace Bliss.CharacterMovement
{
    public interface ICharacterMovementController
    {
        /// <summary>
        /// This is called when the motor wants to know what its rotation should be right now
        /// </summary>
        void UpdateRotation(ref quaternion currentRotation, float deltaTime);
        /// <summary>
        /// This is called when the motor wants to know what its velocity should be right now
        /// </summary>
        void UpdateVelocity(ref float3 currentVelocity, float deltaTime);
        /// <summary>
        /// This is called before the motor does anything
        /// </summary>
        void BeforeCharacterUpdate(float deltaTime);
        /// <summary>
        /// This is called after the motor has finished its ground probing, but before PhysicsMover/Velocity/etc.... handling
        /// </summary>
        void PostGroundingUpdate(float deltaTime);
        /// <summary>
        /// This is called after the motor has finished everything in its update
        /// </summary>
        void AfterCharacterUpdate(float deltaTime);
        /// <summary>
        /// This is called after when the motor wants to know if the collider can be collided with (or if we just go through it)
        /// </summary>
        bool IsColliderValidForCollisions(GameplayCollider coll);
        /// <summary>
        /// This is called when the motor's ground probing detects a ground hit
        /// </summary>
        void OnGroundHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// This is called when the motor's movement logic detects a hit
        /// </summary>
        void OnMovementHit(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// This is called after every move hit, to give you an opportunity to modify the HitStabilityReport to your liking
        /// </summary>
        void ProcessHitStabilityReport(GameplayCollider hitCollider, float3 hitNormal, float3 hitPoint, float3 atCharacterPosition, quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport);
        /// <summary>
        /// This is called when the character detects discrete collisions (collisions that don't result from the motor's capsuleCasts when moving)
        /// </summary>
        void OnDiscreteCollisionDetected(GameplayCollider hitCollider);
        
        long GetLogicTimeStamp();
    }
}