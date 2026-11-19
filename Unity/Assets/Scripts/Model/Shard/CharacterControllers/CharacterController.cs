using System;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Gameplay.GameplayTags;
using Bliss.Mathematics;
using ET;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    /// <summary>Owns initialization and cleanup of a character's dedicated components.</summary>
    public class CharacterController : GamePlayEntity, IAwake, IDestroy
    {
        public InputIntentProcessPipeline InputIntentProcessPipeline;
        
        public KinematicCharacterComponent KinematicCharacterComponent;
        public CharacterMovementComponent MovementComponent;

        /// <summary>The capsule created for this controller, including during partial initialization.</summary>
        public GameplayCapsuleCollider OwnedCapsule;
        /// <summary>The rigidbody created for this controller, including during partial initialization.</summary>
        public GameplayRigidbody OwnedRigidbody;

        public CharacterRuntimeData RuntimeData;
        public ETCancellationToken InitComponentCancellationToken =  new ETCancellationToken();
    }

    public class CharacterRuntimeData : IDisposable
    {
        public float3 CharacterUp = mathEx.Up;
        
        public float3 IntentMovement = float3.zero;
        public float3 IntentLook  = float3.zero;
        
        public bool WantsToEvade;
        public bool WantsToCrouch;


        public bool WantsToJump = false;
        public bool JumpedThisFrame = false;
        public bool JumpConsumed = false;
        public long JumpConsumedTimeStamp;
        public bool InJumpableEnvironment = true;
        public float TimeSinceLastInJumpableEnvironment = 0;

        public void Dispose()
        {
            CharacterUp = default;
            
            IntentMovement =  float3.zero;
            IntentLook = float3.zero;
            
            WantsToEvade = false;
            WantsToCrouch = false;
            
            WantsToJump = false;
            JumpedThisFrame = false;
            JumpConsumed = false;
            InJumpableEnvironment = true;
            TimeSinceLastInJumpableEnvironment = 0;
        }
    }
}