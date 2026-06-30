namespace Bliss
{
    public class MoverStageRuntime : Entity, IAwake<MoverStageCfg>, IDestroy
    {
        public MoverStageCfg StageCfg;
        
        /// <summary>
        /// 运行了多少时间
        /// </summary>
        public long StageTimer;
    }
}