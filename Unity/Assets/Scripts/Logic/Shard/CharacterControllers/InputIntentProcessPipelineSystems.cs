using System;
using Bliss.Core;
using Bliss.Mathematics;
using Bliss.Pool;
using Unity.Mathematics;

namespace Bliss.CharacterControllers
{
    [EntitySystemOf(typeof(InputIntentProcessPipeline))]
    public static partial class InputIntentProcessPipelineSystems
    {
        private static readonly Action<Entity, float> logicUpdateCallback = dispatchLogicUpdate;

        private static void Awake(this InputIntentProcessPipeline self)
        {
            self.InputRecords.Clear();
            self.PendingJump = false;
            self.RegisterLogicUpdate(GameplayWorldLogicPriority.CHARACTER_INPUT, logicUpdateCallback);
        }
        private static void Destroy(this InputIntentProcessPipeline self)
        {
            self.InputRecords.Clear();
            self.InputSource = null;
            self.RuntimeDataRef = null;
            self.PendingJump = false;
        }

        private static void dispatchLogicUpdate(Entity entity, float deltaTime)
        {
            InputIntentProcessPipeline self = (InputIntentProcessPipeline)entity;
            LogicScene scene = self.Scene();
            if (scene.SceneType == ELogicSceneType.Level &&
                !scene.GetComponent<LevelGameWorldComponent>().IsEnabled)
            {
                return;
            }

            // Keep WantsToJump intact for movement callbacks later in this tick.
            self.PendingJump = false;
        }
        
        public static void SetInputSource(this InputIntentProcessPipeline self, IInputSource inputSource)
        {
            self.InputSource = inputSource;
        }

        public static void SetRuntimeData(this InputIntentProcessPipeline self, CharacterRuntimeData runtimeData)
        {
            self.RuntimeDataRef =  runtimeData;
        }

        private static void EarlyUpdate(this InputIntentProcessPipeline self)
        {
            LogicScene scene = self.Scene();
            if (scene.SceneType == ELogicSceneType.Level &&
                !scene.GetComponent<LevelGameWorldComponent>().IsEnabled)
            {
                return;
            }
            if (self.InputSource == null)
            {
                return;
            }

            long timeStamp = TimeInfo.Instance.ClientFrameTime();
            quaternion cameraRotation = self.collectInput(timeStamp);
            self.ProcessInputIntent(timeStamp, cameraRotation);
        }

        private static quaternion collectInput(this InputIntentProcessPipeline self, long timeStamp)
        {
            float3 inputMovement = self.InputSource.GetMovementInput();
            float3 inputLook = self.InputSource.GetLookInput();
            quaternion cameraRotation = self.InputSource.GetCameraRotation();
            
            self.TempList.Clear();
            self.InputSource.GetActionStatus(self.TempList);
            if (self.TempList.Count > 0)
            {
                long previousInputTimeStamp = 0;
                InputRecord oldRecord = null;
                foreach (GameplayInputActionStatus status in self.TempList)
                {
                    if (oldRecord == null || previousInputTimeStamp != status.TimStamp)
                    {
                        previousInputTimeStamp = status.TimStamp;
                        oldRecord = ObjectPool.Instance.Fetch<InputRecord>();
                        oldRecord.Clear();
                        oldRecord.StartTimestamp = timeStamp;
                        oldRecord.Movement = inputMovement;
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
                newRecord.Movement = inputMovement;
                newRecord.Look = inputLook;
                
                self.InputRecords.Add(newRecord, out var removedItem);
                if (removedItem != null)
                {
                    removedItem.Clear();
                    ObjectPool.Instance.Recycle(removedItem);
                }
            }
            return cameraRotation;
        }
        

        private static void ProcessInputIntent(this InputIntentProcessPipeline self, long timeStamp, quaternion cameraRotation)
        {
            var latestInputRecord = self.InputRecords.TailItem();

            float3 cameraPlanarDirection = math.normalizesafe(mathEx.ProjectOnPlane(math.mul(cameraRotation, mathEx.Forward), self.RuntimeDataRef.CharacterUp));
            if (math.lengthsq(cameraPlanarDirection) == 0f)
            {
                cameraPlanarDirection = math.normalizesafe(mathEx.ProjectOnPlane(math.mul(cameraRotation, mathEx.Up), self.RuntimeDataRef.CharacterUp));
            }
            quaternion cameraPlanarRotation = quaternion.LookRotation(cameraPlanarDirection, self.RuntimeDataRef.CharacterUp);
            self.RuntimeDataRef.IntentMovement = math.mul(cameraPlanarRotation, latestInputRecord.Movement);
            self.RuntimeDataRef.IntentLook = latestInputRecord.Look;

            bool jumpInput = false;
            // Only inspect the new batch: replaying historical Down records would reset consumed jumps.
            foreach (GameplayInputActionStatus status in self.TempList)
            {
                if (status.Action == EGameplayInputAction.Jump &&
                    (status.Status & EInputActionStatus.Down) != 0)
                {
                    jumpInput = true;
                    break;
                }
            }

            if (jumpInput)
            {
                self.PendingJump = true;
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
            
            self.RuntimeDataRef.WantsToJump = self.PendingJump ||
                self.IsActionStatus(EGameplayInputAction.Jump, EInputActionStatus.Down, timeStamp, jumpInputGraceTime);
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