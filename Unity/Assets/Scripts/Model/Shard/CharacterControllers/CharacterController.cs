using System;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Gameplay.GameplayTags;
using Bliss.Mathematics;
using ET;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    [SystemPriority(typeof(IEarlyUpdate), 1000)]
    public class CharacterController : Entity, IAwake, IDestroy, IEarlyUpdate, IUpdate
    {
        public InputIntentProcessPipeline InputIntentProcessPipeline;
        
        public KinematicCharacterComponent KinematicCharacterComponent;
        public CharacterMovementComponent MovementComponent;

        public CharacterRuntimeData RuntimeData;
        public long PreviousTimeStamp;
        public float CurrentDeltaTime;   
        public long CurrentDeltaTimeMS;   
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