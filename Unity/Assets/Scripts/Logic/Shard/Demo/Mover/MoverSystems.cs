using Bliss.Core;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(Mover))]
    public static partial class MoverSystems
    {
        private static void Awake(this Mover self, int configId)
        {
            self.MoverConfigId = configId;
            self.MoverConfig = ConfigManager.Instance.Tables.TbMoverConfig.GetOrDefault(configId);
            self.IsCompleted =  false;
        }
    
        public static void Launch(this Mover self, EntityRef<Entity> cargoEntity, GameplayTargetInfo launchInfo)
        {
            self.CargoEntity = cargoEntity;
            
            self.TargetPos = launchInfo.TargetPos;
            self.TargetDir = math.normalize(launchInfo.TargetDir);
            self.TargetEntity = (Entity)launchInfo.TargetEntity;
            
            self.CurrentStageIndex = 0;
            self.CurrentStage = self.AddChild<MoverStageRuntime, MoverStageCfg>(self.MoverConfig.Stages[0]);
            self.IsCompleted =  false;
        }

        public static void LogicUpdate(this Mover self, long dtMs)
        {
            if (self.MoverConfig == null || self.CurrentStage == null)
            {
                return;
            }

            var cargoEntity = (Entity)self.CargoEntity;
            if (cargoEntity == null)
            {
                return;
            }

            var transformCmp = cargoEntity.GetComponent<TransformComponent>();
            if (transformCmp == null)
            {
                return;
            }
            
            float dt = dtMs / 1000f;
            self.MoverTimer += dtMs;

            var handler = MoverHandlerManager.Instance.GetHandler(self.CurrentStage.StageCfg.GetType());
            if (handler.IsComplete(self.CurrentStage))
            {
                self.CurrentStage?.Dispose();
                if (self.CurrentStageIndex >= self.MoverConfig.Stages.Count - 1)
                {
                    //All Stage Completed
                    self.IsCompleted = true;
                    return;
                }

                self.CurrentStageIndex++;
                self.CurrentStage = self.AddChild<MoverStageRuntime, MoverStageCfg>(self.MoverConfig.Stages[self.CurrentStageIndex]);
            }


            var deltaMovement = handler.Calculate(self.CurrentStage, dtMs, 1.0f);
            self.CurrentStage.StageTimer += dtMs;

            var oldPosition = transformCmp.Transform.Location;
            var oldRotation = transformCmp.Transform.Rotation;
            var newPosition = transformCmp.Transform.Location + deltaMovement.Position;
            var newRotation = math.mul(transformCmp.Transform.Rotation, deltaMovement.Rotation);
            var smoothTransformCmp = cargoEntity.GetComponent<SmoothTransformComponent>();

            if (smoothTransformCmp == null)
            {
                transformCmp.SetPositionAndRotation(transformCmp.Transform.Location + deltaMovement.Position, math.mul(transformCmp.Transform.Rotation, deltaMovement.Rotation));
            }
            else
            {
                float3 currentVelocity = deltaMovement.Position / dt;
                smoothTransformCmp.SetLogicTransform(self.MoverTimer, 
                    currentVelocity, 
                    oldPosition, oldRotation,
                    newPosition, newRotation);
            }
        }
    }

    [EntitySystemOf(typeof(MoverStageRuntime))]
    public static partial class MoverStageRuntimeSystems
    {
        private static void Awake(this MoverStageRuntime self, MoverStageCfg param1)
        {
            self.StageCfg = param1;
            self.StageTimer = 0;

        }
        private static void Destroy(this MoverStageRuntime self)
        {
            self.StageCfg = null;
            self.StageTimer = 0;
        }
    }
}

