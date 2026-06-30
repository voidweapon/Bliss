using Unity.Mathematics;

namespace Bliss
{
    public struct GameplayTargetInfo
    {
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