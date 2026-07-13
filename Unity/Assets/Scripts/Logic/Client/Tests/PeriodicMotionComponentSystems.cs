using System;
using System.Collections.Generic;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Debugger.GameDebugStats;
using Bliss.Mathematics;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace Bliss
{
    [EntitySystemOf(typeof(PeriodicMotionComponent))]
    public static partial class PeriodicMotionComponentSystems
    {
        private static Dictionary<int, TransformSyncMode> UpdateTransformSyncMode =
            new Dictionary<int, TransformSyncMode>()
            {
                {
                    0, new TransformSyncMode()
                    {
                        OnEndSimulate = SimpleNetSample,
                        OnReceiveMsg = SimpleSync
                    }
                },
                {
                    1, new TransformSyncMode()
                    {
                        OnBeforeSimulate = OnBeforeSimulate_1,
                        OnEndSimulate = OnEndSimulate_1,
                        OnReceiveMsg = SimpleSync,
                    }
                },
                {
                    2, new TransformSyncMode()
                    {
                        OnBeforeSimulate = OnBeforeSimulate_1,
                        OnEndSimulate = OnEndSimulate_UE,
                        OnReceiveMsg = OnReceiveTransformMessage_UE,
                        OnSimulateUpdate = OnSimulateUpdate_UE,
                    }
                },
            };
        private static void Awake(this PeriodicMotionComponent self, PeriodicMotionComponent.InitParam  param)
        {
            self.StarTime = TimeInfo.Instance.ClientNow();
            self.StartPosition = self.Parent.GetComponent<TransformComponent>().Transform.Location;
            self.NetRole = self.Parent.GetComponent<NetRoleComponent>().LocalNetRole;
            self.RemoteNetRole = self.Parent.GetComponent<NetRoleComponent>().RemoteNetRole;
            self.ClientCharacterMovement2Data = new NetworkPredictionData_Client_PeriodicMotionComponent();
            self.ClientCharacterMovement2Data.MaxSmoothNetUpdateDist = self.NetworkMaxSmoothUpdateDistance;
            self.ClientCharacterMovement2Data.NoSmoothNetUpdateDist = self.NetworkNoSmoothUpdateDistance;
            self.NetworkSmoothingMode = ENetworkSmoothingMode.Linear;

            self.IsNetworkSmoothingComplete = true;
            self.InitState = param;
            self.DistanceMag = param.Distance * param.Distance;
            self.CurrentVelocity = param.Velocity;
            self.LastSyncTime = 0;
            self.OnewayTimer = TimeInfo.Instance.ClientNow();
            self.EnterMode2Time = self.InitState.Mode2Interval;
            
            self.CurrentAngularVelocity = param.AngularVelocity;
            self.EnterMode2Time = self.InitState.ChangeRotationInterval;
            self.RotateRandom = new Unity.Mathematics.Random((uint)self.InstanceId);
            self.EnablePredication = true;
            
            self.TransformSyncTestMode = 2;

            if (self.NetRole == ENetRole.SimulatedProxy)
            {
                GameDebugStates.Instance.SetDebugStat("TsmSync", nameof(self.NetworkSmoothingMode), self.NetworkSmoothingMode, new float4(0,0,0,1));
                GameDebugStates.Instance.SetDebugStat("TsmSync", nameof(self.SyncInterval), self.SyncInterval, new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<NetworkPredictionData_Client_PeriodicMotionComponent, long>("TsmSync", self.ClientCharacterMovement2Data, nameof(NetworkPredictionData_Client_PeriodicMotionComponent.SmoothingClientTimeStamp), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<NetworkPredictionData_Client_PeriodicMotionComponent, long>("TsmSync", self.ClientCharacterMovement2Data, nameof(NetworkPredictionData_Client_PeriodicMotionComponent.SmoothingServerTimeStamp), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<NetworkPredictionData_Client_PeriodicMotionComponent, long>("TsmSync", self.ClientCharacterMovement2Data, nameof(NetworkPredictionData_Client_PeriodicMotionComponent.LastCorrectionTime), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<NetworkPredictionData_Client_PeriodicMotionComponent, long>("TsmSync", self.ClientCharacterMovement2Data, nameof(NetworkPredictionData_Client_PeriodicMotionComponent.LastCorrectionDelta), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<NetworkPredictionData_Client_PeriodicMotionComponent, float3>("TsmSync", self.ClientCharacterMovement2Data, nameof(NetworkPredictionData_Client_PeriodicMotionComponent.MeshTranslationOffset), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<PeriodicMotionComponent, float3>("TsmSync", self, nameof(PeriodicMotionComponent.CurrentAngularVelocity), new float4(0,0,0,1));
                GameDebugStates.Instance.AddDebugStat<PeriodicMotionComponent, bool>("TsmSync", self, nameof(PeriodicMotionComponent.IsNetworkSmoothingComplete), new float4(0,0,0,1), true);
                GameDebugStates.Instance.AddDebugStat<PeriodicMotionComponent, long>("TsmSync", self, nameof(PeriodicMotionComponent.ReplicatedServerLastTransformUpdateTimeStamp), new float4(0,0,0,1));
            }
            if (self.NetRole == ENetRole.SimulatedProxy)
            {
                GameDebugStates.Instance.AddDebugStat<PeriodicMotionComponent, long>("TsmSync", self, nameof(PeriodicMotionComponent.ServerLastTransformUpdateTimeStamp), new float4(0,0,0,1));
            }
        }
        private static void Destroy(this PeriodicMotionComponent self)
        {

        }


        public static void ReceiveTransformSync(this PeriodicMotionComponent self, TransformSimpleSyncMessage message)
        {
            if (UpdateTransformSyncMode.TryGetValue(self.TransformSyncTestMode, out var syncMode))
            {
                syncMode.OnReceiveMsg(self, message);
            }
        }

        public static void ReceivePropertySync(this PeriodicMotionComponent self, PropertySync_Long message)
        {
            self.ReplicatedServerLastTransformUpdateTimeStamp = message.Value;
        }
        
        private static void Update(this PeriodicMotionComponent self)
        {
            long now = TimeInfo.Instance.ClientNow();

            if (self.NetRole == ENetRole.SimulatedProxy)
            {
                self.SimulateUpdate(now);
            }
            else if (self.NetRole == ENetRole.AutonomousProxy)
            {
                
            }
            else if (self.NetRole == ENetRole.Authority)
            {
                self.AuthorityUpdate(now);
            }
            self.OnewayTimer = now;
        }

        private static void SimulateUpdate(this PeriodicMotionComponent self, long now)
        {
            if (UpdateTransformSyncMode.TryGetValue(self.TransformSyncTestMode, out var syncMode))
            {
                if(syncMode.OnSimulateUpdate != null)
                    syncMode.OnSimulateUpdate(self, now);
            }
        }

        private static void AuthorityUpdate(this PeriodicMotionComponent self, long now)
        {
            long dtMS = now - self.OnewayTimer;
            float dt = dtMS / 1000f;
            
            if (UpdateTransformSyncMode.TryGetValue(self.TransformSyncTestMode, out var syncModeBefore))
            {
                if(syncModeBefore.OnBeforeSimulate != null)
                    syncModeBefore.OnBeforeSimulate(self, now);
            }
            

            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            float3 newLocation = transformCmp.Transform.Location;
            if (self.OnewayDistance + dt * math.length(self.CurrentVelocity) < self.InitState.Distance)
            {
                var dtMove = self.CurrentVelocity * dt;
                self.OnewayDistance += math.length(dtMove);
                newLocation += dtMove;
            }
            else
            {
                float remainDistance = self.InitState.Distance - self.OnewayDistance;
                newLocation += math.normalize(self.CurrentVelocity) * remainDistance;
                self.OnewayDistance = self.InitState.Distance;
                
                float remainTime = dt - remainDistance / math.length(self.CurrentVelocity);
                if (remainTime > 0)
                {
                    self.CurrentVelocity = new float3(-self.CurrentVelocity.x, self.CurrentVelocity.y, self.CurrentVelocity.z);
                    self.OnewayDistance = math.length(self.CurrentVelocity * remainTime);
                    newLocation += self.CurrentVelocity * remainTime;
                }
            }

            bool enterMode2 = false;
            bool exitMode2 = false;

            self.Mode2Timer += dt;
            if (self.InMode2)
            {
                if (self.Mode2Timer - self.EnterMode2Time >= self.InitState.Mode2Duration)
                {
                    self.InMode2 = false;
                    exitMode2 = true;
                    self.EnterMode2Time = self.Mode2Timer 
                                          + self.InitState.Mode2Interval
                                          + (RandomGenerator.RandFloat() - 0.5f) * 2f * self.InitState.Mode3IntervalVariance;
                }
            }
            else
            {
                if (self.Mode2Timer > self.EnterMode2Time)
                {
                    self.InMode2 = true;
                    enterMode2 = true;
                    self.EnterMode2Time = self.Mode2Timer;
                }
            }


            if (enterMode2)
            {
                newLocation += new float3(0, 1, 0);
            }
            if (exitMode2)
            {
                newLocation += new float3(0, -1, 0);
            }
            
            transformCmp.SetPosition(newLocation);


            self.RotationTimer += dt;
            if (self.InStableRotation)
            {
                if (self.RotationTimer - self.EnterRotationTimer >= self.InitState.StableRotationDuration)
                {
                    self.CurrentAngularVelocity = self.InitState.AngularVelocity;
                    //self.CurrentAngularVelocity = self.RotateRandom.NextFloat3Direction() * math.length(self.InitState.AngularVelocity);
                    self.InStableRotation = false;
                    self.EnterRotationTimer = self.InitState.ChangeRotationInterval
                                              + (RandomGenerator.RandFloat() - 0.5f) * 2f * self.InitState.ChangeRotationIntervalVariance;
                }
            }
            else
            {
                if (self.RotationTimer > self.EnterRotationTimer)
                {
                    self.CurrentAngularVelocity = float3.zero;
                    self.InStableRotation = true;
                    self.EnterRotationTimer = self.RotationTimer = 0f;
                }
            }

            if (!self.InStableRotation)
            {
                var deltaRotation = quaternion.EulerZXY(self.CurrentAngularVelocity * dt);
                transformCmp.SetRotation(math.mul(transformCmp.Transform.Rotation, deltaRotation));
            }

            if (UpdateTransformSyncMode.TryGetValue(self.TransformSyncTestMode, out var syncMode))
            {
                if(syncMode.OnEndSimulate != null)
                    syncMode.OnEndSimulate(self, now);
            }
        }

        #region 最基础的同步逻辑
        //简单的按间隔同步下transform，势必会有卡顿
        private static void SimpleNetSample(this PeriodicMotionComponent self, long now)
        {
            if (now - self.LastSyncTime < self.SyncInterval)
            {
                return;
            }
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            self.LastSyncTime = now;

            using var transformSyncMsg = TransformSimpleSyncMessage.Create(true);
            transformSyncMsg.ActorId = self.Parent.Id;
            transformSyncMsg.Position = transformCmp.Transform.Location;
            transformSyncMsg.Rotation = transformCmp.Transform.Rotation.value;
            transformSyncMsg.Velocity = self.CurrentVelocity;
            transformSyncMsg.AngularVelocity = self.CurrentAngularVelocity;
            
            self.Scene<LogicScene>().GetComponent<LevelPlayerMgrComponent>().Broadcast(transformSyncMsg);
        }

        private static void SimpleSync(this PeriodicMotionComponent self, TransformSimpleSyncMessage message)
        {
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            transformCmp.SetPositionAndRotation(message.Position, message.Rotation);
        }

        #endregion

        #region 精确识别关键状态变更

        private static void OnBeforeSimulate_1(this PeriodicMotionComponent self, long now)
        {
            long dtMS = now - self.OnewayTimer;
            self.NewMove = new SavedMove_Character_PeriodicMotionComponent();
            self.NewMove.InitSaveMove(now, dtMS, self);
        }
        
        private static void OnEndSimulate_1(this PeriodicMotionComponent self, long now)
        {
            self.NewMove.PostInitSaveMove(self);

            bool canDelaySend = !self.NewMove.IsForceNoCombine;

            if (canDelaySend)
            {
                if (now - self.LastSyncTime < self.SyncInterval)
                {
                    self.PendingMove = self.NewMove;
                    return;
                }
            }
            self.LastSyncTime = now;

            var sendingMoveData = self.NewMove;
            
            using var transformSyncMsg = TransformSimpleSyncMessage.Create(true);
            transformSyncMsg.ActorId = self.Parent.Id;
            transformSyncMsg.Position = sendingMoveData.EndPosition;
            transformSyncMsg.Rotation = sendingMoveData.EndRotation.value;
            transformSyncMsg.Velocity = sendingMoveData.EndVelocity;
            transformSyncMsg.AngularVelocity = self.CurrentAngularVelocity;
            
            self.Scene<LogicScene>().GetComponent<LevelPlayerMgrComponent>().Broadcast(transformSyncMsg);

            self.PendingMove = null;
        }

        public static void InitSaveMove(ref this SavedMove_Character_PeriodicMotionComponent self, 
            long nowTimeMS,
            long deltaTimeMS,
            PeriodicMotionComponent mover)
        {
            self.TimeStampMS =  nowTimeMS;
            self.DeltaTimeMS =  deltaTimeMS;
            
            var transformCmp = mover.Parent.GetComponent<TransformComponent>();
            
            self.StartPosition = transformCmp.Transform.Location;
            self.StartRotation =  transformCmp.Transform.Rotation;
            self.StartVelocity = mover.CurrentVelocity;
            self.StartAcceleration = float3.zero;
            self.StartAngularVelocity = mover.CurrentAngularVelocity;
            self.StartMoveMode = !mover.InMode2 ? 0 : 1;
        }

        public static void PostInitSaveMove(ref this SavedMove_Character_PeriodicMotionComponent self, PeriodicMotionComponent mover)
        {
            var transformCmp = mover.Parent.GetComponent<TransformComponent>();
            self.EndPosition = transformCmp.Transform.Location;
            self.EndRotation =  transformCmp.Transform.Rotation;
            self.EndVelocity = mover.CurrentVelocity;
            self.EndAcceleration = float3.zero;
            self.EndAngularVelocity = mover.CurrentAngularVelocity;
            self.EndMoveMode = !mover.InMode2 ? 0 : 1;

            if (self.StartVelocity.Equals(float3.zero) != self.EndVelocity.Equals(float3.zero))
            {
                self.IsForceNoCombine = true;
            }
            
            if (self.StartAngularVelocity.Equals(float3.zero) != self.EndAngularVelocity.Equals(float3.zero))
            {
                self.IsForceNoCombine = true;
            }
            
            if (self.StartMoveMode != self.EndMoveMode)
            {
                self.IsForceNoCombine = true;
            }
        }

        #endregion

        #region 关键状态变更精确识别 + UE Smooth 方案 （不预测服务器时间）

        private static void OnEndSimulate_UE(this PeriodicMotionComponent self, long now)
        {
            self.ServerLastTransformUpdateTimeStamp = now;
            if (now - self.ServerTimeStampSyncTimer > self.ServerTimeStampSyncInterval)
            {
                self.ServerTimeStampSyncTimer = now;
                using var timestampMessage = PropertySync_Long.Create(true);
                timestampMessage.ActorId = self.Parent.Id;
                timestampMessage.Value = self.ServerLastTransformUpdateTimeStamp;
                self.Scene<LogicScene>().GetComponent<LevelPlayerMgrComponent>().Broadcast(timestampMessage);
            }
            
            self.NewMove.PostInitSaveMove(self);

            bool canDelaySend = !self.NewMove.IsForceNoCombine;

            if (canDelaySend)
            {
                if (now - self.LastSyncTime < self.SyncInterval)
                {
                    self.PendingMove = self.NewMove;
                    return;
                }
            }
            self.LastSyncTime = now;

            var sendingMoveData = self.NewMove;
            
            using var transformSyncMsg = TransformSimpleSyncMessage.Create(true);
            transformSyncMsg.ActorId = self.Parent.Id;
            transformSyncMsg.Position = sendingMoveData.EndPosition;
            transformSyncMsg.Rotation = sendingMoveData.EndRotation.value;
            transformSyncMsg.Velocity = sendingMoveData.EndVelocity;
            transformSyncMsg.AngularVelocity = self.CurrentAngularVelocity;
            
            self.Scene<LogicScene>().GetComponent<LevelPlayerMgrComponent>().Broadcast(transformSyncMsg);

            self.PendingMove = null;
        }
        
        private static void OnReceiveTransformMessage_UE(this PeriodicMotionComponent self, TransformSimpleSyncMessage message)
        {
            self.CurrentVelocity = message.Velocity;
            self.CurrentAngularVelocity = message.AngularVelocity;
            
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            float3 oldPosition = transformCmp.Transform.Location;
            quaternion oldRotation = transformCmp.Transform.Rotation;
            
            if (self.NetRole == ENetRole.SimulatedProxy)
            {
                self.SmoothCorrection(oldPosition, oldRotation, message.Position,  message.Rotation);
            }
        }

        private static void OnSimulateUpdate_UE(this PeriodicMotionComponent self, long now)
        {
            long dtMS = now - self.OnewayTimer;
            float dt = dtMS / 1000f;
            
            //TODO:  ENetworkSmoothingMode.Linear 模式下预测旋转有问题
            if (self.NetworkSmoothingMode != ENetworkSmoothingMode.Disabled && self.EnablePredication)
            {
                var transformCmp = self.Parent.GetComponent<TransformComponent>();
                float3 saveViewRelativeLocation = transformCmp.ViewRelativeTransform.Location;
                quaternion savedViewRelativeRotation = transformCmp.ViewRelativeTransform.Rotation;
                quaternion savedRotation = transformCmp.Transform.Rotation;
                bool preventViewMove = !self.IsNetworkSmoothingComplete;
                using (var _ = ScopedPreventViewMove.Create(preventViewMove ? transformCmp : null))
                {
                    self.SimulateMovement_UE(dt);
                }

                // With Linear smoothing we need to know if the rotation changes, since the mesh should follow along with that (if it was prevented above).
                // This should be rare that rotation changes during simulation, but it can happen when ShouldRemainVertical() changes, or standing on a moving base.
                bool validateRotation = preventViewMove && (self.NetworkSmoothingMode == ENetworkSmoothingMode.Linear);
                if (validateRotation)
                {
                    // Same mesh with different rotation?
                    quaternion newRotation = transformCmp.Transform.Rotation;
                    if (!newRotation.Equals(savedRotation, 1e-6f) && self.ClientCharacterMovement2Data != null)
                    {
                        // Smoothing should lerp toward this new rotation target, otherwise it will just try to go back toward the old rotation.
                        self.ClientCharacterMovement2Data.MeshRotationTarget = newRotation;
                        transformCmp.SynViewRelativeTranslationAndRotation(saveViewRelativeLocation, savedViewRelativeRotation);
                    }
                }
            }
            
            
            if (!self.IsNetworkSmoothingComplete)
            {
                self.SmoothClientPosition(dtMS);
            }
        }

        private static void SimulateMovement_UE(this PeriodicMotionComponent self, float dt)
        {
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            var newPosition = transformCmp.Transform.Location + self.CurrentVelocity * dt;

            var oldRotation = transformCmp.Transform.Rotation;
            if (self.NetworkSmoothingMode == ENetworkSmoothingMode.Linear)
            {
                //我不认为在UpdateComponent的Rotation上直接插值的Linear模式，直接用UpdateComponent的Rotation这个插值中间值是对的，
                //预测的目的是赶上对端，那么Linear模式下，要么用在MeshRotationTarget的基础上更新Rotation然后更新MeshRotationTarget
                oldRotation = self.ClientCharacterMovement2Data.MeshRotationTarget;
            }
            
            var deltaRotation = quaternion.EulerZXY(self.CurrentAngularVelocity * dt);
            var newRotation = math.mul(oldRotation, deltaRotation);
            transformCmp.SetPositionAndRotation(newPosition, newRotation);
        }
        
        private static void SmoothCorrection(this PeriodicMotionComponent self,
            float3 oldPosition, quaternion oldRotation,
            float3 newPosition, quaternion newRotation)
        {
            if (self.ClientCharacterMovement2Data == null)
            {
                return;
            }
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            
            self.IsNetworkSmoothingComplete = false;

            if (self.NetworkSmoothingMode == ENetworkSmoothingMode.Disabled)
            {
                transformCmp.SetPositionAndRotation(newPosition, newRotation);
                self.IsNetworkSmoothingComplete = true;
                return;
            }

            float3 newToOldVector = (oldPosition - newPosition);
            float dist = math.lengthsq(newToOldVector);
            if (dist > math.square(self.ClientCharacterMovement2Data.MaxSmoothNetUpdateDist))
            {
                if (dist > math.square(self.ClientCharacterMovement2Data.NoSmoothNetUpdateDist))
                {
                    self.ClientCharacterMovement2Data.MeshTranslationOffset = float3.zero;
                }
                else
                {
                    self.ClientCharacterMovement2Data.MeshTranslationOffset += math.normalizesafe(newToOldVector) * self.ClientCharacterMovement2Data.MaxSmoothNetUpdateDist;
                }
            }
            else
            {
                self.ClientCharacterMovement2Data.MeshTranslationOffset += newToOldVector;
            }
            

            if (self.NetworkSmoothingMode == ENetworkSmoothingMode.Linear)
            {
                self.ClientCharacterMovement2Data.OriginalMeshTranslationOffset = self.ClientCharacterMovement2Data.MeshTranslationOffset;
                
                // Remember the current and target rotation, we're going to lerp between them
                self.ClientCharacterMovement2Data.OriginalMeshRotationOffset = oldRotation;
                self.ClientCharacterMovement2Data.MeshRotationOffset = oldRotation;
                self.ClientCharacterMovement2Data.MeshRotationTarget = newRotation;
                
                if (!oldPosition.Equals(newPosition))
                {
                    using var _ = ScopedPreventViewMove.Create(transformCmp);
                    transformCmp.SetPosition(newPosition);
                }
            }
            else
            {
                // Calc rotation needed to keep current world rotation after UpdatedComponent moves.
                // Take difference between where we were rotated before, and where we're going
                self.ClientCharacterMovement2Data.MeshRotationOffset = math.mul(math.mul(math.inverse(newRotation), oldRotation), self.ClientCharacterMovement2Data.MeshRotationOffset);
                self.ClientCharacterMovement2Data.MeshRotationTarget = quaternion.identity;
                
                using var _ = ScopedPreventViewMove.Create(transformCmp);
                transformCmp.SetPositionAndRotation(newPosition, newRotation);
            }
            
            //////////////////////////////////////////////////////////////////////////
            // Update smoothing timestamps
            if (self.ClientCharacterMovement2Data.SmoothingClientTimeStamp > self.ClientCharacterMovement2Data.SmoothingServerTimeStamp)
            {
                var oldClientTimeStamp = self.ClientCharacterMovement2Data.SmoothingClientTimeStamp;
                self.ClientCharacterMovement2Data.SmoothingClientTimeStamp = (self.ClientCharacterMovement2Data.SmoothingServerTimeStamp + oldClientTimeStamp) / 2;
                //Log.Info($"SmoothCorrection: Pull back client from ClientTimeStamp {oldClientTimeStamp} to {self.ClientData.SmoothingServerTimeStamp}, ServerTimeStamp {self.ClientData.SmoothingServerTimeStamp}");
            }
            
            var oldServerTimeStamp = self.ClientCharacterMovement2Data.SmoothingServerTimeStamp;
            if (self.NetRole == ENetRole.SimulatedProxy)
            {
                self.ServerLastTransformUpdateTimeStamp = self.ReplicatedServerLastTransformUpdateTimeStamp;
            }
            self.ClientCharacterMovement2Data.SmoothingServerTimeStamp = self.ServerLastTransformUpdateTimeStamp;
            
            // Initial update has no delta.
            if (self.ClientCharacterMovement2Data.LastCorrectionTime == 0)
            {
                self.ClientCharacterMovement2Data.SmoothingClientTimeStamp =  self.ClientCharacterMovement2Data.SmoothingServerTimeStamp;
                oldServerTimeStamp = self.ClientCharacterMovement2Data.SmoothingServerTimeStamp;
            }
            
            // Don't let the client fall too far behind or run ahead of new server time.
            var serverDeltaTime = self.ClientCharacterMovement2Data.SmoothingServerTimeStamp - oldServerTimeStamp;
            var maxOffset = self.ClientCharacterMovement2Data.MaxClientSmoothingDeltaTime;
            var minOffset = math.min(self.ClientCharacterMovement2Data.SmoothNetUpdateTime, maxOffset);
            
            var maxDelta = math.clamp((long)(serverDeltaTime * 1.25f), minOffset, maxOffset);
            self.ClientCharacterMovement2Data.SmoothingClientTimeStamp = math.clamp(self.ClientCharacterMovement2Data.SmoothingClientTimeStamp, 
                self.ClientCharacterMovement2Data.SmoothingServerTimeStamp - maxDelta, 
                self.ClientCharacterMovement2Data.SmoothingServerTimeStamp );
            
            // Compute actual delta between new server timestamp and client simulation.
            self.ClientCharacterMovement2Data.LastCorrectionDelta = self.ClientCharacterMovement2Data.SmoothingServerTimeStamp - self.ClientCharacterMovement2Data.SmoothingClientTimeStamp;
            self.ClientCharacterMovement2Data.LastCorrectionTime = TimeInfo.Instance.ClientNow();
        }

        private static void SmoothClientPosition(this PeriodicMotionComponent self, long deltaTimeMS)
        {
            if (self.NetRole == ENetRole.SimulatedProxy || self.RemoteNetRole == ENetRole.AutonomousProxy)
            {
                self.SmoothClientPosition_Interpolate(deltaTimeMS);
                self.SmoothClientPosition_UpdateVisuals();
            }
        }

        private static void SmoothClientPosition_Interpolate(this PeriodicMotionComponent self, long deltaTimeMS)
        {
            var clientData = self.ClientCharacterMovement2Data;
            if (clientData == null) return;

            if (self.NetworkSmoothingMode == ENetworkSmoothingMode.Linear)
            {
                // Increment client position.
                clientData.SmoothingClientTimeStamp += deltaTimeMS;
                double lerpPercent = 0f;
                double lerpLimit = 1.15f;
                var targetDeltaTime = clientData.LastCorrectionDelta;

                if (targetDeltaTime > 0)
                {
                    // Don't let the client get too far ahead (happens on spikes). But we do want a buffer for variable network conditions.
                    double maxClientTimeAheadPercent = 0.15f;
                    var maxTimeAhead = (long)(targetDeltaTime * maxClientTimeAheadPercent);
                    clientData.SmoothingClientTimeStamp = math.min(clientData.SmoothingClientTimeStamp,
                        clientData.SmoothingServerTimeStamp + maxTimeAhead);
                    
                    // Compute interpolation alpha based on our client position within the server delta. We should take TargetDelta seconds to reach alpha of 1.
                    var remainingTime = clientData.SmoothingServerTimeStamp - clientData.SmoothingClientTimeStamp;
                    var currentSmoothTime = targetDeltaTime - remainingTime;
                    lerpPercent = math.clamp((double)currentSmoothTime / targetDeltaTime, 0f, lerpLimit);
                }
                else
                {
                    lerpPercent = 1f;
                }

                if (lerpPercent >= 1f - double.Epsilon)
                {
                    if (self.CurrentVelocity.Equals(float3.zero))
                    {
                        clientData.MeshTranslationOffset = float3.zero;
                        clientData.SmoothingClientTimeStamp = clientData.SmoothingServerTimeStamp;
                        self.IsNetworkSmoothingComplete = true;
                    }
                    else
                    {
                        // Allow limited forward prediction.
                        clientData.MeshTranslationOffset = math.lerp(clientData.OriginalMeshTranslationOffset, float3.zero, (float)lerpPercent);
                        self.IsNetworkSmoothingComplete = lerpPercent >= lerpLimit;
                    }
                    clientData.MeshRotationOffset = clientData.MeshRotationTarget;
                }
                else
                {
                    clientData.MeshTranslationOffset = math.lerp(clientData.OriginalMeshTranslationOffset, float3.zero, (float)lerpPercent);
                    clientData.MeshRotationOffset = math.nlerp(clientData.OriginalMeshRotationOffset, clientData.MeshRotationTarget, (float)lerpPercent);
                }
            }
            else if (self.NetworkSmoothingMode ==  ENetworkSmoothingMode.Exponential)
            {
                // Smooth interpolation of mesh translation to avoid popping of other client pawns unless under a low tick rate.
                // Faster interpolation if stopped.
                long smoothLocationTime = self.CurrentVelocity.Equals(float3.zero)
                    ?  clientData.SmoothNetUpdateTime / 2
                    : clientData.SmoothNetUpdateTime;

                if (deltaTimeMS < smoothLocationTime)
                {
                    // Slowly decay translation offset
                    clientData.MeshTranslationOffset *= (float)(1f - (double)deltaTimeMS / smoothLocationTime);
                }
                else
                {
                    clientData.MeshTranslationOffset = float3.zero;
                }

                GameDebugStates.Instance.SetDebugStat("TsmSync", nameof(deltaTimeMS), deltaTimeMS,
                    deltaTimeMS < smoothLocationTime ? new float4(0, 0, 0, 1) : new float4(1, 0, 0, 1));

                
                // Smooth rotation
                var meshRotationTarget = clientData.MeshRotationTarget;
                if (deltaTimeMS < clientData.SmoothNetUpdateRotationTime)
                {
                    // Slowly decay rotation offset
                    float lerpPercent = (float)((double)deltaTimeMS / clientData.SmoothNetUpdateRotationTime);
                    clientData.MeshRotationOffset = math.nlerp(clientData.MeshRotationOffset, meshRotationTarget, lerpPercent);
                    //clientData.MeshRotationOffset = mathEx.FastLerp(clientData.MeshRotationOffset, meshRotationTarget, lerpPercent);
                }
                else
                {
                    clientData.MeshRotationOffset = meshRotationTarget;
                }
                
                // Check if lerp is complete
                if (clientData.MeshTranslationOffset.IsNearlyZero(1e-2f) &&
                    clientData.MeshRotationOffset.Equals(clientData.MeshRotationTarget, 1e-5f))
                {
                    self.IsNetworkSmoothingComplete = true;
                    
                    clientData.MeshTranslationOffset = float3.zero;
                    clientData.MeshRotationOffset = meshRotationTarget;
                }
            }
            else
            {
                // Unhandled mode
            }
        }
        private static void SmoothClientPosition_UpdateVisuals(this PeriodicMotionComponent self)
        {
            var clientData = self.ClientCharacterMovement2Data;
            if (clientData == null) return;

            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            
            if (self.NetworkSmoothingMode == ENetworkSmoothingMode.Linear)
            {
                float3 newRealLocation = clientData.MeshRotationOffset.UnrotateVector(clientData.MeshTranslationOffset);
                if (!transformCmp.Transform.Rotation.Equals(clientData.MeshRotationOffset, float.Epsilon))
                {
                    quaternion oldRotation = transformCmp.Transform.Rotation;
                    transformCmp.ViewRelativeTransform.Location = newRealLocation;
                    transformCmp.SetRotation(clientData.MeshRotationOffset, true);

                    if (transformCmp.Transform.Rotation.Equals(oldRotation))
                    {
                        transformCmp.ViewRelativeTransform.Location = newRealLocation;
                        transformCmp.SynViewRelativeTranslation(newRealLocation);
                    }
                }
                else
                {
                    transformCmp.SynViewRelativeTranslation(newRealLocation);
                }
            }
            else if(self.NetworkSmoothingMode == ENetworkSmoothingMode.Exponential)
            {
                float3 newRealLocation = transformCmp.Transform.InverseTransformVectorNoScale(clientData.MeshTranslationOffset);
                quaternion newRealRotation = clientData.MeshRotationOffset;
                transformCmp.SynViewRelativeTranslationAndRotation(newRealLocation, newRealRotation);
            }
            else
            {
                // Unhandled mode
            }
        }
        
        #endregion
        
        private struct TransformSyncMode
        {
            public Action<PeriodicMotionComponent, long> OnBeforeSimulate;
            public Action<PeriodicMotionComponent, long> OnEndSimulate;
            public Action<PeriodicMotionComponent, TransformSimpleSyncMessage> OnReceiveMsg;
            
            public Action<PeriodicMotionComponent, long> OnSimulateUpdate;
            
        }
    }
}