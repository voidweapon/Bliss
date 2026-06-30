using Unity.Mathematics;

namespace Bliss
{
    public struct GameplayEvent_ShootBullet
    {
        public EntityRef<Entity> Caster;
        public EntityRef<Entity> Shooter;
        public float3 Position;
        public quaternion Rotation;
        public int BulletConfigId;
        public GameplayTargetInfo TargetInfo;
    }
}