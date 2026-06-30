using MemoryPack;
using Unity.Mathematics;

namespace Bliss
{
    public class ShooterGear : ASceneActorCmp<ShooterGear, ShooterGearConfig>, IAfterSceneInit, IDestroy
    {
        public long UpdateTimer;
    }

    [System.Serializable]
    [MemoryPackable]
    public partial class ShooterGearConfig : ASceneActorConfig
    {
        public float Damage;
        public float Speed;
        public int BulletConfigId;
    }
}