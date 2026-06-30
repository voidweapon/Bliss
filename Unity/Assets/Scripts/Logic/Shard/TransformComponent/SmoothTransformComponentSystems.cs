using Bliss.Core;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(SmoothTransformComponent))]
    public static partial class SmoothTransformComponentSystems
    {
        private static void Awake(this SmoothTransformComponent self, SmoothTransformComponentInitParam  param)
        {
            self.ViewTimeStamp = TimeInfo.Instance.ClientNow();
            
            self.SmoothingMode = param.SmoothingMode;
            
            self.SyncData = new LogicViewTransformSyncData()
            {
                MaxSmoothNetUpdateDist = param.MaxSmoothNetUpdateDist,
                NoSmoothNetUpdateDist = param.NoSmoothNetUpdateDist,
            };

            self.IsSmoothingComplete = true;
        }
        
        private static void Update(this SmoothTransformComponent self)
        {
            long now = TimeInfo.Instance.ClientNow();
            
            long dtMS = now - self.ViewTimeStamp;
            
            if (!self.IsSmoothingComplete)
            {
                self.SmoothViewPose(dtMS);
            }
            
            self.ViewTimeStamp = now;
            
        }

        public static void SetLogicTransform(this SmoothTransformComponent self, 
            long logicTimeStamp, float3 velocity,
            float3 oldPosition, quaternion oldRotation,
            float3 newPosition, quaternion newRotation)
        {
            var transformCmp = self.Parent.GetComponent<TransformComponent>();

            if (self.SmoothingMode == ENetworkSmoothingMode.Disabled)
            {
                transformCmp.SetPositionAndRotation(newPosition, newRotation);
                self.IsSmoothingComplete = true;
                return;
            }
            
            if (self.SyncData == null)
            {
                return;
            }
            
                        
            self.IsSmoothingComplete = false;
            self.CurrentVelocity = velocity;
            
            //更新view位置和logic位置的差值
            float3 newToOldVector = (oldPosition - newPosition);
            float dist = math.lengthsq(newToOldVector);
            if (dist > math.square(self.SyncData.MaxSmoothNetUpdateDist))
            {
                if (dist > math.square(self.SyncData.NoSmoothNetUpdateDist))
                {
                    self.SyncData.MeshTranslationOffset = float3.zero;
                }
                else
                {
                    self.SyncData.MeshTranslationOffset += math.normalizesafe(newToOldVector) * self.SyncData.MaxSmoothNetUpdateDist;
                }
            }
            else
            {
                self.SyncData.MeshTranslationOffset += newToOldVector;
            }
            
            self.SyncData.OriginalMeshTranslationOffset = self.SyncData.MeshTranslationOffset;
                
            // Remember the current and target rotation, we're going to lerp between them
            self.SyncData.OriginalMeshRotationOffset = oldRotation;
            self.SyncData.MeshRotationOffset = oldRotation;
            self.SyncData.MeshRotationTarget = newRotation;
            //更新逻辑位置    
            if (!oldPosition.Equals(newPosition))
            {
                using var _ = ScopedPreventViewMove.Create(transformCmp);
                transformCmp.SetPosition(newPosition);
            }
            
            //////////////////////////////////////////////////////////////////////////
            // Update smoothing timestamps
            if (self.SyncData.ViewSmoothTimeStamp > self.SyncData.LogicTimeStamp)
            {
                var oldClientTimeStamp = self.SyncData.ViewSmoothTimeStamp;
                self.SyncData.ViewSmoothTimeStamp = (self.SyncData.LogicTimeStamp + oldClientTimeStamp) / 2;
            }
            
            var oldLogicTimeStamp = self.SyncData.LogicTimeStamp;
            self.SyncData.LogicTimeStamp = logicTimeStamp;
            
            // Initial update has no delta.
            if (self.SyncData.LastCorrectionTime == 0)
            {
                self.SyncData.ViewSmoothTimeStamp =  self.SyncData.LogicTimeStamp;
                oldLogicTimeStamp = self.SyncData.LogicTimeStamp;
            }
            
            // Don't let the client fall too far behind or run ahead of new server time.
            var serverDeltaTime = self.SyncData.LogicTimeStamp - oldLogicTimeStamp;
            var maxOffset = self.SyncData.MaxClientSmoothingDeltaTime;
            var minOffset = math.min(self.SyncData.SmoothNetUpdateTime, maxOffset);
            
            var maxDelta = math.clamp((long)(serverDeltaTime * 1.25f), minOffset, maxOffset);
            self.SyncData.ViewSmoothTimeStamp = math.clamp(self.SyncData.ViewSmoothTimeStamp, 
                self.SyncData.LogicTimeStamp - maxDelta, 
                self.SyncData.LogicTimeStamp );
            
            // Compute actual delta between new server timestamp and client simulation.
            self.SyncData.LastCorrectionDelta = self.SyncData.LogicTimeStamp - self.SyncData.ViewSmoothTimeStamp;
            self.SyncData.LastCorrectionTime = TimeInfo.Instance.ClientNow();
        }

        private static void SmoothViewPose(this SmoothTransformComponent self, long deltaTime)
        {
            self.SmoothViewPose_Interpolate(deltaTime);
            self.SmoothViewPose_UpdateVisuals();
        }

        private static void SmoothViewPose_Interpolate(this SmoothTransformComponent self, long deltaTimeMS)
        {
            var clientData = self.SyncData;
            if (clientData == null) return;
            
            // Increment client position.
            clientData.ViewSmoothTimeStamp += deltaTimeMS;
            double lerpPercent = 0f;
            double lerpLimit = 1.15f;
            var targetDeltaTime = clientData.LastCorrectionDelta;

            if (targetDeltaTime > 0)
            {
                // Don't let the client get too far ahead (happens on spikes). But we do want a buffer for variable network conditions.
                double maxClientTimeAheadPercent = 0.15f;
                var maxTimeAhead = (long)(targetDeltaTime * maxClientTimeAheadPercent);
                clientData.ViewSmoothTimeStamp = math.min(clientData.ViewSmoothTimeStamp,
                    clientData.LogicTimeStamp + maxTimeAhead);
                
                // Compute interpolation alpha based on our client position within the server delta. We should take TargetDelta seconds to reach alpha of 1.
                var remainingTime = clientData.LogicTimeStamp - clientData.ViewSmoothTimeStamp;
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
                    clientData.ViewSmoothTimeStamp = clientData.LogicTimeStamp;
                    self.IsSmoothingComplete = true;
                }
                else
                {
                    // Allow limited forward prediction.
                    clientData.MeshTranslationOffset = math.lerp(clientData.OriginalMeshTranslationOffset, float3.zero, (float)lerpPercent);
                    self.IsSmoothingComplete = lerpPercent >= lerpLimit;
                }
                clientData.MeshRotationOffset = clientData.MeshRotationTarget;
            }
            else
            {
                clientData.MeshTranslationOffset = math.lerp(clientData.OriginalMeshTranslationOffset, float3.zero, (float)lerpPercent);
                clientData.MeshRotationOffset = math.nlerp(clientData.OriginalMeshRotationOffset, clientData.MeshRotationTarget, (float)lerpPercent);
            }
        }

        private static void SmoothViewPose_UpdateVisuals(this SmoothTransformComponent self)
        {
            var clientData = self.SyncData;
            if (clientData == null) return;
            
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            
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
    }
}