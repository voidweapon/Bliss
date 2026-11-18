using System;
using Bliss.Base.Collections;
using Bliss.Core;

namespace Bliss
{
    [EntitySystemOf(typeof(MoverComponent))]
    public static partial class MoverComponentSystem
    {
        private static readonly Action<Entity, float> logicUpdateCallback = dispatchLogicUpdate;

        private static void Awake(this MoverComponent self)
        {
            self.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, logicUpdateCallback);
        }

        private static void dispatchLogicUpdate(Entity entity, float deltaTime)
        {
            ((MoverComponent)entity).logicUpdate();
        }
    
        private static void logicUpdate(this MoverComponent self)
        {
            LogicScene scene = self.Scene();
            if (scene.SceneType == ELogicSceneType.Level &&
                !scene.GetComponent<LevelGameWorldComponent>().IsEnabled)
            {
                return;
            }

            using var removeList = ListComponent<Entity>.Create();
            
            foreach (var kv in self.Children)
            {
                var mover = kv.Value as Mover;
                if (mover == null)
                {
                    continue;
                }
                
                mover.LogicUpdate(GameplayWorldLogicUpdate.STEP_MILLISECONDS);
                
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