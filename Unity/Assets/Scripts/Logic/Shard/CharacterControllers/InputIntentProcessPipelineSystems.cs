using Bliss.Core;
using Bliss.Mathematics;
using Bliss.Pool;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    [EntitySystemOf(typeof(InputIntentProcessPipeline))]
    public static partial class InputIntentProcessPipelineSystems
    {
        private static void Awake(this InputIntentProcessPipeline self)
        {
            self.InputRecords.Clear();
        }
        private static void Destroy(this InputIntentProcessPipeline self)
        {
            self.InputRecords.Clear();
        }
        
        public static void SetInputSource(this InputIntentProcessPipeline self, IInputSource inputSource)
        {
            self.InputSource = inputSource;
        }

        public static void SetRuntimeData(this InputIntentProcessPipeline self, CharacterRuntimeData runtimeData)
        {
            self.RuntimeDataRef =  runtimeData;
        }

        public static void LogicUpdate(this InputIntentProcessPipeline self, float deltaTime)
        {

        }

        public static void ViewEarlyUpdate(this InputIntentProcessPipeline self, float deltaTime)
        {
            if (self.InputSource == null)
            {
                return;
            }
            
            float3 inputMovement = self.InputSource.GetMovementInput();
            float3 inputLook = self.InputSource.GetLookInput();
            quaternion cameraRotation = self.InputSource.GetCameraRotation();
            long timeStamp = TimeInfo.Instance.ClientFrameTime();
            
            // Calculate camera direction and rotation on the character plane
            float3 cameraPlanarDirection = math.normalizesafe(mathEx.ProjectOnPlane(math.mul(cameraRotation, mathEx.Forward), self.RuntimeDataRef.CharacterUp));
            if (math.lengthsq(cameraPlanarDirection) == 0f)
            {
                cameraPlanarDirection = math.normalizesafe(mathEx.ProjectOnPlane(math.mul(cameraRotation , mathEx.Up), self.RuntimeDataRef.CharacterUp));
            }
            quaternion cameraPlanarRotation = quaternion.LookRotation(cameraPlanarDirection, self.RuntimeDataRef.CharacterUp);
            var adjustedMovement = math.mul(cameraPlanarRotation, inputMovement);
            
            self.TempList.Clear();
            self.InputSource.GetActionStatus(self.TempList);
            if (self.TempList.Count > 0)
            {
                long previousInputTimeStamp = 0;
                InputRecord oldRecord = null;
                foreach (GameplayInputActionStatus status in self.TempList)
                {
                    if (previousInputTimeStamp != status.TimStamp)
                    {
                        previousInputTimeStamp = status.TimStamp;
                        oldRecord = ObjectPool.Instance.Fetch<InputRecord>();
                        oldRecord.Clear();
                        oldRecord.StartTimestamp = timeStamp;
                        oldRecord.Movement = adjustedMovement;
                        oldRecord.Look = inputLook;
                        self.InputRecords.Add(oldRecord, out var remove);
                        if (remove != null)
                        {
                            remove.Clear();
                            ObjectPool.Instance.Recycle(remove);
                        }
                    }

                    if (!oldRecord.InputActionStatus.TryGetValue(status.Action, out var actionStatus))
                    {
                        actionStatus = status.Status;
                        oldRecord.InputActionStatus.Add(status.Action, status.Status);
                    }

                    oldRecord.InputActionStatus[status.Action] = actionStatus | status.Status;
                }
            }
            else
            {
                InputRecord newRecord = ObjectPool.Instance.Fetch<InputRecord>();
                newRecord.Clear();
                
                newRecord.StartTimestamp = timeStamp;
                newRecord.Movement = adjustedMovement;
                newRecord.Look = inputLook;
                
                self.InputRecords.Add(newRecord, out var removedItem);
                if (removedItem != null)
                {
                    removedItem.Clear();
                    ObjectPool.Instance.Recycle(removedItem);
                }
            }
            self.ProcessInputIntent(timeStamp);
        }
        

        private static void ProcessInputIntent(this InputIntentProcessPipeline self, long timeStamp)
        {
            var latestInputRecord = self.InputRecords.TailItem();

            //movement intent
            //if (la3ewtestInputRecord.StartTimestamp == timeStamp)
            {
                self.RuntimeDataRef.IntentMovement = latestInputRecord.Movement;
                self.RuntimeDataRef.IntentLook = latestInputRecord.Look;
            }
            
            
            // self.RuntimeDataRef.WantsToEvade = self.IsActionPress(EGameplayInputAction.Evade, timeStamp, 100);
            // self.RuntimeDataRef.WantsToCrouch = self.IsActionPress(EGameplayInputAction.Crouch, timeStamp, 100);

#pragma warning disable CS0219 // Variable is assigned but its value is never used
            bool jumpInput = false;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            if (latestInputRecord.HasActionDown(EGameplayInputAction.Jump))
            {
                jumpInput = true;
                self.RuntimeDataRef.JumpConsumed = false;
                self.RuntimeDataRef.JumpConsumedTimeStamp = 0;
            }
            
            long jumpInputGraceTime = self.JumpInputGraceTimeMS;
            if (self.RuntimeDataRef.JumpConsumed)
            {
                  jumpInputGraceTime = math.min(self.JumpInputGraceTimeMS, timeStamp - self.RuntimeDataRef.JumpConsumedTimeStamp);
            }
            
#if Client
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat($"Input", "JumpInput", jumpInput, new float4(0, 0, 0, 1));
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat($"Input", "jumpInputGraceTime", jumpInputGraceTime, new float4(0, 0, 0, 1));
            Bliss.Debugger.GameDebugStats.GameDebugStates.Instance.SetDebugStat($"Input", "ConsumeDtMS", timeStamp - self.RuntimeDataRef.JumpConsumedTimeStamp, new float4(0, 0, 0, 1));
#endif
            
            self.RuntimeDataRef.WantsToJump = self.IsActionStatus(EGameplayInputAction.Jump, EInputActionStatus.Down, timeStamp, jumpInputGraceTime);
        }

        private static bool IsActionStatus(this InputIntentProcessPipeline self, EGameplayInputAction inputAction, EInputActionStatus matchStatus, long timeStamp, long graceTime)
        {
            for (int i = self.InputRecords.Count - 1; i >= 0; i--)
            {
                var record = self.InputRecords[i];
                if (record.StartTimestamp < timeStamp - graceTime)
                {
                    break;
                }

                if (record.InputActionStatus.TryGetValue(inputAction, out EInputActionStatus status))
                {
                    if (status.HasFlag(matchStatus))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    
    
        // private static bool IsActionPress(this InputIntentProcessPipeline self, EGameplayInputAction inputAction, long timeStamp, long graceTime)
        // {
        //     for (int i = self.InputRecords.Count - 1; i >= 0; i--)
        //     {
        //         var record = self.InputRecords[i];
        //         if (record.StartTimestamp < timeStamp - graceTime)
        //         {
        //             break;
        //         }
        //
        //         if (record.InputActions.Contains(inputAction))
        //         {
        //             return true;
        //         }
        //     }
        //
        //     return false;
        // }

    }
}