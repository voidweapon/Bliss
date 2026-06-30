using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Timer;

namespace Bliss
{
    [EntitySystemOf(typeof(MoverComponent))]
    public static partial class MoverComponentSystem
    {
        private static void Awake(this MoverComponent self)
        {
            self.UpdateTimer = self.RootTimerComponent().NewRepeatedTimer(GamePlayDefines.LogicUpdateDeltaTimeMS, TimerInvokeType.MoverUpdate, self);
            self.PrevUpdateTime = TimeInfo.Instance.ClientNow();
        }
        private static void Destroy(this MoverComponent self)
        {
            self.RootTimerComponent().CancelTimer(ref self.UpdateTimer);
        }
    
        [Invoke(TimerInvokeType.MoverUpdate)]
        private class MoverTimer : ATimer<MoverComponent>
        {
            protected override void Run(MoverComponent self)
            {
                self.LogicUpdate();
            }
        }
    
        private static void LogicUpdate(this MoverComponent self)
        {
            long now = TimeInfo.Instance.ClientNow();
            long dtMS = now - self.PrevUpdateTime;
            self.PrevUpdateTime = now;
            
            using var removeList = ListComponent<Entity>.Create();
            
            foreach (var kv in self.Children)
            {
                var mover = kv.Value as Mover;
                if (mover == null)
                {
                    continue;
                }
                
                mover.LogicUpdate(dtMS);
                
                if (mover.IsCompleted)
                {
                    removeList.Add(mover);
                }
            }

            foreach (var mover in removeList)
            {
                mover.Dispose();
            }
        }
    
        public static long LaunchMover(this MoverComponent self, int moverConfigId, GameplayTargetInfo  launchInfo)
        {
            var moverConfig = ConfigManager.Instance.Tables.TbMoverConfig.GetOrDefault(moverConfigId);
            if (moverConfig == null)
            {
                Log.Error($"not found MoverConfig  {moverConfigId}");
                return -1;
            }

            var mover = self.AddChild<Mover, int>(moverConfigId);
            mover.Launch(self.Parent, launchInfo);
            
            return mover.Id;
        }
    }
}