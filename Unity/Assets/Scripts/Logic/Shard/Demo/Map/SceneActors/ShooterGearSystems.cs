using Bliss.Core;
using System;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(ShooterGear))]
    public static partial class ShooterGearSystems
    {
        private const long SHOOT_INTERVAL_MILLISECONDS = 10000;

        private static void AfterSceneInit(this ShooterGear self)
        {
            GameplayWorld world = self.GetLevelGameplayWorld()
                ?? throw new InvalidOperationException("ShooterGear must belong to a GameplayWorld.");
            self.UpdateTimer = world.Timer.ScheduleRepeated(SHOOT_INTERVAL_MILLISECONDS, self.shootIfEnabled, self);
        }

        private static void Destroy(this ShooterGear self)
        {
            GameplayWorld world = self.GetLevelGameplayWorld();
            if (world == null)
            {
                self.UpdateTimer = default;
                return;
            }

            world.Timer.Cancel(ref self.UpdateTimer);
        }

        private static void shootIfEnabled(this ShooterGear self)
        {
            LogicScene scene = self.Scene();
            if (scene.SceneType == ELogicSceneType.Level &&
                !scene.GetComponent<LevelGameWorldComponent>().IsEnabled)
            {
                return;
            }

            self.Shoot();
        }

        /// <summary>Raises a shot event from the configured scene actor's transform.</summary>
        /// <param name="self">The configured, live ShooterGear.</param>
        public static void Shoot(this ShooterGear self)
        {
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            var fwd = math.mul(transformCmp.Transform.Rotation, new float3(0, 0, 1));
            
            EventSystem.Instance.Push(self.Scene(), new GameplayEvent_ShootBullet()
            {
                BulletConfigId = self.SceneActorCmpConfig.BulletConfigId,
                Caster = self.Parent,
                Shooter = self.Parent,
                Position = transformCmp.Transform.Location,
                Rotation = transformCmp.Transform.Rotation,
                TargetInfo = new GameplayTargetInfo()
                {
                    TargetDir = fwd,
                    TargetPos = transformCmp.Transform.Location + fwd,
                    TargetEntity = default
                }
            });
        }
    }
}