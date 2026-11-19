using Bliss.Physics;
using Unity.Mathematics;

namespace Bliss
{
    /// <summary>Stores the configuration and World-owned state of one replicated Bullet.</summary>
    public class Bullet : GamePlayEntity, IAwake<BulletCreateInfo>, IDestroy
    {
        /// <summary>The configuration identifier used to create this Bullet.</summary>
        public int BulletConfigId;

        /// <summary>The target data used by this Bullet's Mover.</summary>
        public GameplayTargetInfo TargetInfo;

        /// <summary>The authoritative World timer scheduled for this Bullet's finite lifetime.</summary>
        public long TimeoutTimerId;

        /// <summary>The position recorded before the most recent movement step.</summary>
        public float3 PreviousPosition;

        /// <summary>The authoritative server timestamp at which this finite-lived Bullet expires.</summary>
        public long ExpirationTimeMS;

        /// <summary>The host entity of the ShooterGear that created this Bullet.</summary>
        public EntityRef<Entity> ShooterHost;

        /// <summary>The LevelScene PhysX component used for Bullet collision queries.</summary>
        public EntityRef<PhysicalScene> PhysicalScene;

        /// <summary>The LevelScene associated with this Bullet's GameplayWorld.</summary>
        public EntityRef<LogicScene> LevelScene;
    }

    /// <summary>Supplies validated ownership, scene and lifetime data when creating a Bullet.</summary>
    public struct BulletCreateInfo
    {
        /// <summary>The Bullet configuration identifier.</summary>
        public int BulletConfigId;

        /// <summary>The target data used by the Bullet's Mover.</summary>
        public GameplayTargetInfo TargetInfo;

        /// <summary>The local network role for the new Bullet.</summary>
        public ENetRole LocalRole;

        /// <summary>The remote network role for the new Bullet.</summary>
        public ENetRole RemoteRole;

        /// <summary>The remaining authoritative lifetime in milliseconds; finite Bullets require a positive value.</summary>
        public long RemainingLifetimeMS;

        /// <summary>The host entity of the required ShooterGear component.</summary>
        public EntityRef<Entity> ShooterHost;

        /// <summary>The required LevelScene PhysX component.</summary>
        public EntityRef<PhysicalScene> PhysicalScene;

        /// <summary>The LevelScene associated with the owning GameplayWorld.</summary>
        public EntityRef<LogicScene> LevelScene;
    }
}
