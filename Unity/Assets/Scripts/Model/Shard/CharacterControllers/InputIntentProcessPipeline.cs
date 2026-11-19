using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    /// <summary>Collects character input during the owning GameplayWorld's EarlyUpdate.</summary>
    [SystemPriority(typeof(IEarlyUpdate), 1000)]
    public class InputIntentProcessPipeline : GamePlayEntity, IAwake, IDestroy, IEarlyUpdate
    {
        public IInputSource InputSource;
        public FixedCircularArray<InputRecord> InputRecords = new FixedCircularArray<InputRecord>(64);
        public List<GameplayInputActionStatus> TempList = new List<GameplayInputActionStatus>();

        public long JumpInputGraceTimeMS = 100;
        
        public CharacterRuntimeData  RuntimeDataRef;

        /// <summary>Whether a jump edge is still waiting for an enabled World tick.</summary>
        public bool PendingJump;
    }

    public class InputRecord
    {
        public long StartTimestamp;
        public float3 Movement;
        public float3 Look;
        public List<EGameplayInputAction> InputActions = new List<EGameplayInputAction>();
        public Dictionary<EGameplayInputAction, EInputActionStatus> InputActionStatus = new Dictionary<EGameplayInputAction, EInputActionStatus>();

        public void Clear()
        {
            StartTimestamp = long.MinValue;
            Movement = float3.zero;
            Look = float3.zero;
            InputActions.Clear();
            InputActionStatus.Clear();
        }

        public bool HasActionDown(EGameplayInputAction inputAction)
        {
            if (InputActionStatus.TryGetValue(inputAction, out EInputActionStatus status))
            {
                return status.HasFlag(EInputActionStatus.Down);
            }
            return false;
        }
    }
}