using System;
using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Physics;
using Bliss.Timer;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(Bullet))]
    public static partial class BulletSystems
    {
        private static void Awake(this Bullet self, BulletCreateInfo createInfo)
        {
            self.BulleConfigId = createInfo.BulleConfigId;
            self.TargetInfo = createInfo.TargetInfo;
            self.Runtime = createInfo.Runtime;
            self.CreateTime = DateTime.Now.Ticks;
            
            var networkEntityManagerCmp = self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManagerCmp?.AddEntity(self.Id, self);
            
            var bulletManager = self.GetLevelGameplayWorld()?.GetComponent<BulletManager>();
            bulletManager?.Add(self);
            
            var config = self.GetConfig();
            if (config.DurationMS > 0)
            {
                self.TimeoutTimer = self.RootTimerComponent().NewOnceTimer(config.DurationMS - self.Runtime, TimerInvokeType.BulletTimeout, self);
            }
            self.LogicUpdateTimer = self.RootTimerComponent().NewRepeatedTimer(GamePlayDefines.LogicUpdateDeltaTimeMS, TimerInvokeType.BulletLogicUpdate, self);
        }
        
        private static void Destroy(this Bullet self)
        {
            self.DestroyTime = DateTime.Now.Ticks;
            
            self.BulleConfigId = 0;
            var networkEntityManagerCmp = self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManagerCmp?.RemoveEntity(self.Id);
            
            var bulletManager = self.GetLevelGameplayWorld()?.GetComponent<BulletManager>();
            bulletManager?.Remove(self.Id);

            var timerCmp = self.RootTimerComponent();
            timerCmp.CancelTimer(ref self.TimeoutTimer);
            timerCmp.CancelTimer(ref self.LogicUpdateTimer);
        }

        public static void DestroyBullet(this Bullet self, int reason)
        {
            self.Dispose();
        }

        public static BulletConfig GetConfig(this Bullet self)
        {
            return ConfigManager.Instance.Tables.TbBulletConfig.GetOrDefault(self.BulleConfigId);
        }

        public static void Launch(this Bullet self, GameplayTargetInfo  launchInfo)
        {
            var moverCmp = self.GetComponent<MoverComponent>();
            moverCmp.LaunchMover(self.GetConfig().MoverId, launchInfo);
            
            var transformCmp = self.GetComponent<TransformComponent>();
            self.PrevPosition = transformCmp.Transform.Location;

        }

        [Invoke(TimerInvokeType.BulletTimeout)]
        private class BulletTimeoutTimer : ATimer<Bullet>
        {
            protected override void Run(Bullet self)
            {
                self?.Timeout();
            }
        }

        private static void Timeout(this Bullet self)
        {
            self.DestroyBullet(0);
        }
        
        [Invoke(TimerInvokeType.BulletLogicUpdate)]
        private class BulletLogicUpdateTimer : ATimer<Bullet>
        {
            protected override void Run(Bullet self)
            {
                self?.LogicUpdate(GamePlayDefines.LogicUpdateDeltaTimeMS);
            }
        }

        private static void LogicUpdate(this Bullet self, long dtMS)
        {
            self.Runtime += dtMS;
            
            using var results = ArrayComponent<GameplayRaycastHit>.Create(64);
            var transformCmp = self.GetComponent<TransformComponent>();
            var currentPosition = transformCmp.Transform.Location;
            var direction = currentPosition - self.PrevPosition;
            var distance = math.length(direction);
            float radius = 0.5f;
            self.PrevPosition = currentPosition;
            direction = math.normalize(direction);
            
#if DOTNET

            var physicalScene = self.Scene().GetComponent<PhysicalScene>();
            int count = physicalScene.SpereCastNonAlloc(self.PrevPosition, radius, direction, distance, -1, results.ItemArray);
            if (count > 0)
            {
                M2C_DestroyEntities m2c_destroyEntities = M2C_DestroyEntities.Create();
                m2c_destroyEntities.EntityIds.Add(self.Id);
                MapMessageHelper.Broadcast(self.Scene(), currentPosition, m2c_destroyEntities); 
                self.Dispose();
            }
#endif
        }
    }
}