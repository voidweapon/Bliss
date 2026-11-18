using Unity.Mathematics;

namespace Bliss
{
    /// <summary>Tracks a cargo entity's configured movement stages.</summary>
    public class Mover : GamePlayEntity, IAwake<int>
    {
        public int MoverConfigId;
        public MoverConfig MoverConfig;
        
        public MoverStageRuntime CurrentStage;
        public int CurrentStageIndex;
        public bool IsCompleted = false;
        
        /// <summary>
        /// 被移动的实体
        /// </summary>
        public EntityRef<Entity> CargoEntity;
        
        /// <summary>
        /// 运行了多少时间
        /// </summary>
        public long MoverTimer;
        
        /// <summary>
        /// 目标点
        /// </summary>
        public float3 TargetPos;
        
        /// <summary>
        /// 目标方向
        /// </summary>
        public float3 TargetDir;
        
        /// <summary>
        /// 目标实体
        /// </summary>
        public EntityRef<Entity> TargetEntity;
    }
}