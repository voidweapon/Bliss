namespace Bliss
{
    /// <summary>Stores the runtime state of a single Mover stage.</summary>
    public class MoverStageRuntime : GamePlayEntity, IAwake<MoverStageCfg>, IDestroy
    {
        public MoverStageCfg StageCfg;
        
        /// <summary>
        /// 运行了多少时间
        /// </summary>
        public long StageTimer;
    }
}