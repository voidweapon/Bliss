using Bliss.Mathematics;

namespace Bliss
{
    [MoverStageHandler(typeof(StraightLineMoverStageCfg))]
    public class StraightLineMoverStageHandler : AMoverStageHandler
    {
        public override DeltaMovement Calculate(MoverStageRuntime stageRuntime, long deltaMS, float weight)
        {
            Mover mover = stageRuntime.Parent as Mover;
            StraightLineMoverStageCfg cfg = (StraightLineMoverStageCfg)stageRuntime.StageCfg;

            float delta = deltaMS / 1000f;
            
            return new DeltaMovement(cfg.Speed * weight * delta * mover.TargetDir);
        }

        public override bool IsComplete(MoverStageRuntime stageRuntime)
        {
            return false;
        }
    }
}