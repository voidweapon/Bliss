using Unity.Mathematics;

namespace Bliss
{
    public class Bullet : Entity, IAwake<BulletCreateInfo>, IDestroy
    {
        public int BulleConfigId;
        public GameplayTargetInfo TargetInfo;

        public long TimeoutTimer;
        public long LogicUpdateTimer;
        
        public float3 PrevPosition;
        public long Runtime;
        public long CreateTime;
        public long DestroyTime;
    }

    public struct BulletCreateInfo
    {
        public int BulleConfigId;
        public GameplayTargetInfo TargetInfo;
        public ENetRole LocalRole;
        public ENetRole RemoteRole;
        public long Runtime;
    }
}