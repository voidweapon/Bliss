using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Timer;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(ShooterGear))]
    public static partial class ShooterGearSystems
    {
        private static void AfterSceneInit(this ShooterGear self)
        {
            self.UpdateTimer = self.RootTimerComponent().NewRepeatedTimer(10000, TimerInvokeType.ShoterGearUpdate, self);
        }
        private static void Destroy(this ShooterGear self)
        {
            self.RootTimerComponent().CancelTimer(ref self.UpdateTimer);
        }
        
        [Invoke(TimerInvokeType.ShoterGearUpdate)]
        private class UpdateTimer : ATimer<ShooterGear>
        {
            protected override void Run(ShooterGear self)
            {
                self.Shoot();
            }
        }

        public static void Shoot(this ShooterGear self)
        {
            var transformCmp = self.Parent.GetComponent<TransformComponent>();
            var fwd = math.mul(transformCmp.Transform.Rotation, new float3(0, 0, 1));
            
            EventSystem.Instance.Push(self.Scene(), new GameplayEvent_ShootBullet()
            {
                BulletConfigId = self.SceneActorCmpConfig.BulletConfigId,
                Caster = self,
                Shooter = self,
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