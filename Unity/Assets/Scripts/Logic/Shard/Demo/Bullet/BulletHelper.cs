using System;
using Bliss.Physics;
using Bliss.Timer;
using Unity.Mathematics;

namespace Bliss
{
    /// <summary>Creates GameplayWorld-owned Bullets and their replicated protocol data.</summary>
    public static class BulletHelper
    {
        private const float BULLET_AOI_RADIUS = 10f;
        private const long INFINITE_LIFETIME_MILLISECONDS = 0;
        private const long EXPIRED_LIFETIME_MILLISECONDS = 0;

        /// <summary>Creates a Bullet after validating its ShooterGear, LevelScene and World dependencies.</summary>
        /// <param name="levelScene">The LevelScene that owns the ShooterGear and PhysicalScene.</param>
        /// <param name="bulletConfigId">The Bullet configuration identifier.</param>
        /// <param name="entityId">The network identifier assigned to the Bullet.</param>
        /// <param name="localRole">The Bullet's local network role.</param>
        /// <param name="remoteRole">The Bullet's remote network role.</param>
        /// <param name="position">The Bullet's starting position.</param>
        /// <param name="rotation">The Bullet's starting rotation.</param>
        /// <param name="targetInfo">The target information used by its Mover.</param>
        /// <param name="shooter">The live ShooterGear required by the Bullet.</param>
        /// <param name="remainingLifetimeMS">Remaining lifetime for finite Bullets; zero starts a new server lifetime.</param>
        /// <param name="isRaiseEvent">Whether to raise the scene actor creation event.</param>
        /// <returns>The created Bullet.</returns>
        /// <exception cref="InvalidOperationException">A required scene, component, configuration or Shooter is missing.</exception>
        public static Bullet CreateBullet(LogicScene levelScene,
            int bulletConfigId, long entityId,
            ENetRole localRole, ENetRole remoteRole,
            float3 position, quaternion rotation,
            GameplayTargetInfo targetInfo,
            ShooterGear shooter,
            long remainingLifetimeMS = INFINITE_LIFETIME_MILLISECONDS,
            bool isRaiseEvent = true)
        {
            if (levelScene == null || levelScene.IsDisposed)
            {
                throw new InvalidOperationException("Bullet creation requires a live LevelScene.");
            }

            LevelGameWorldComponent levelWorld = levelScene.GetComponent<LevelGameWorldComponent>();
            GameplayWorld world = levelWorld?.GameplayWorld
                ?? throw new InvalidOperationException("Bullet creation requires a GameplayWorld for its LevelScene.");
            PhysicalScene physicalScene = levelScene.GetComponent<Bliss.Physics.PhysicalScene>()
                ?? throw new InvalidOperationException("Bullet creation requires a PhysicalScene.");
            AoiManagerComponent aoiManager = world.GetComponent<AoiManagerComponent>()
                ?? throw new InvalidOperationException("Bullet creation requires an AoiManagerComponent.");
            BulletManager bulletManager = world.GetComponent<BulletManager>()
                ?? throw new InvalidOperationException("Bullet creation requires a BulletManager.");
            NetworkEntityManagerComponent networkEntityManager =
                levelScene.RootScene().GetComponent<NetworkEntityManagerComponent>()
                ?? throw new InvalidOperationException("Bullet creation requires a NetworkEntityManagerComponent.");
            BulletConfig config = ConfigManager.Instance.Tables.TbBulletConfig.GetOrDefault(bulletConfigId)
                ?? throw new InvalidOperationException($"Bullet configuration {bulletConfigId} was not found.");

            Entity shooterHost = shooter?.Parent;
            if (shooter == null || shooter.IsDisposed || shooterHost == null || shooterHost.IsDisposed ||
                !ReferenceEquals(shooter.Scene(), levelScene) ||
                !ReferenceEquals(shooterHost.GetLevelGameplayWorld(), world))
            {
                throw new InvalidOperationException("Bullet creation requires a live ShooterGear in the same GameplayWorld.");
            }

            if (physicalScene.IsDisposed ||
                !ReferenceEquals(physicalScene.Scene(), levelScene) ||
                !ReferenceEquals(levelWorld.GameplayWorld, world))
            {
                throw new InvalidOperationException("Bullet creation requires the live PhysicalScene associated with its LevelScene.");
            }

            if (!ReferenceEquals((Entity)networkEntityManager.GetEntity(shooterHost.Id), shooterHost))
            {
                throw new InvalidOperationException("The Shooter host is not registered with the NetworkEntityManagerComponent.");
            }

            if (bulletManager.IsDisposed || aoiManager.IsDisposed)
            {
                throw new InvalidOperationException("Bullet creation requires live GameplayWorld managers.");
            }

            long lifetimeMS = config.DurationMS > INFINITE_LIFETIME_MILLISECONDS
                ? (remainingLifetimeMS > EXPIRED_LIFETIME_MILLISECONDS
                    ? remainingLifetimeMS
                    : config.DurationMS)
                : INFINITE_LIFETIME_MILLISECONDS;

            Bullet bullet = world.AddChildWithId<Bullet, BulletCreateInfo>(entityId, new BulletCreateInfo
            {
                BulletConfigId = bulletConfigId,
                LocalRole = localRole,
                RemoteRole = remoteRole,
                TargetInfo = targetInfo,
                RemainingLifetimeMS = lifetimeMS,
                ShooterHost = shooterHost,
                PhysicalScene = physicalScene,
                LevelScene = levelScene,
            });

            TransformComponent transform = bullet.AddComponent<TransformComponent>();
            transform.SetPositionAndRotation(position, rotation);

            bullet.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(
                new NetRoleComponentInitParam { LocalNetRole = localRole, RemoteNetRole = remoteRole });
#if Client
            bullet.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(
                new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
#endif
            bullet.AddComponent<MoverComponent>();

            long visibilityId = aoiManager.Register(AoiRegisterInfo.CreateSphere(
                bullet, AOIType.Visibility, transform.Transform.Location, BULLET_AOI_RADIUS));
            AOIEntityCmp aoi = bullet.AddComponent<AOIEntityCmp>();
            aoi.Add(AOIType.Visibility, visibilityId);

#if DOTNET
            long networkVisibilityId = aoiManager.Register(AoiRegisterInfo.CreateSphere(
                bullet, AOIType.NetworkVisibility, transform.Transform.Location, BULLET_AOI_RADIUS));
            aoi.Add(AOIType.NetworkVisibility, networkVisibilityId);
#endif

            if (isRaiseEvent)
            {
                EventSystem.Instance.Push(levelScene, new Event_AfterCreateSceneActor
                {
                    Actor = bullet,
                    ViewAssetPath = config.ModelPath,
                });
            }

            bullet.Launch(targetInfo);
            return bullet;
        }

        /// <summary>Creates a client Bullet from a server DTO, skipping an expired finite lifetime.</summary>
        /// <param name="bulletInfo">The server Bullet state to apply.</param>
        /// <param name="levelScene">The live LevelScene receiving the Bullet.</param>
        /// <param name="bullet">The created Bullet, or null when the finite lifetime has expired.</param>
        /// <returns>Whether a Bullet was created.</returns>
        /// <exception cref="InvalidOperationException">A required Shooter, scene, component or configuration is missing.</exception>
        public static bool TryCreateBullet(this DTOBulletInfo bulletInfo, LogicScene levelScene, out Bullet bullet)
        {
            bullet = null;
            BulletConfig config = ConfigManager.Instance.Tables.TbBulletConfig.GetOrDefault(bulletInfo.ConfigId)
                ?? throw new InvalidOperationException($"Bullet configuration {bulletInfo.ConfigId} was not found.");
            if (config.DurationMS > INFINITE_LIFETIME_MILLISECONDS &&
                bulletInfo.RemainingLifetimeMS <= EXPIRED_LIFETIME_MILLISECONDS)
            {
                return false;
            }

            NetworkEntityManagerComponent networkEntityManager =
                levelScene.RootScene().GetComponent<NetworkEntityManagerComponent>()
                ?? throw new InvalidOperationException("Bullet replication requires a NetworkEntityManagerComponent.");
            Entity shooterHost = (Entity)networkEntityManager.GetEntity(bulletInfo.ShooterEntityId);
            ShooterGear shooter = shooterHost?.GetComponent<ShooterGear>();
            if (shooterHost == null || shooter == null || shooterHost.IsDisposed || shooter.IsDisposed)
            {
                throw new InvalidOperationException(
                    $"Bullet replication references missing Shooter host {bulletInfo.ShooterEntityId}.");
            }

            GameplayTargetInfo targetInfo = bulletInfo.TargetInfo.Create(levelScene);
            bullet = CreateBullet(levelScene, bulletInfo.ConfigId, bulletInfo.BulletId,
                bulletInfo.LocalRole, bulletInfo.RemoteRole, bulletInfo.Position, bulletInfo.Rotation,
                targetInfo, shooter, bulletInfo.RemainingLifetimeMS);
            return true;
        }

        /// <summary>Creates a network DTO from an authoritative Bullet's current state.</summary>
        /// <param name="bullet">The Bullet to replicate.</param>
        /// <param name="isFromPool">Whether the DTO should come from the message object pool.</param>
        /// <returns>A DTO carrying the Bullet and Shooter identifiers and its remaining lifetime.</returns>
        /// <exception cref="InvalidOperationException">The Shooter host is no longer available.</exception>
        public static DTOBulletInfo CreateDTOBulletInfo(this Bullet bullet, bool isFromPool = false)
        {
            TransformComponent transform = bullet.GetComponent<TransformComponent>();
            NetRoleComponent netRole = bullet.GetComponent<NetRoleComponent>();
            Entity shooterHost = (Entity)bullet.ShooterHost;
            if (shooterHost == null || shooterHost.IsDisposed)
            {
                throw new InvalidOperationException("Cannot replicate a Bullet whose Shooter host is unavailable.");
            }

            BulletConfig config = bullet.GetConfig();
            long remainingLifetimeMS = config.DurationMS > INFINITE_LIFETIME_MILLISECONDS
                ? Math.Max(EXPIRED_LIFETIME_MILLISECONDS,
                    bullet.ExpirationTimeMS - TimeInfo.Instance.ServerFrameTime())
                : INFINITE_LIFETIME_MILLISECONDS;

            DTOBulletInfo dtoBulletInfo = DTOBulletInfo.Create(isFromPool);
            dtoBulletInfo.BulletId = bullet.Id;
            dtoBulletInfo.ConfigId = bullet.BulletConfigId;
            dtoBulletInfo.Position = transform.Transform.Location;
            dtoBulletInfo.Rotation = transform.Transform.Rotation;
            dtoBulletInfo.LocalRole = netRole.LocalNetRole;
            dtoBulletInfo.RemoteRole = netRole.RemoteNetRole;
            dtoBulletInfo.TargetInfo = bullet.TargetInfo.Create();
            dtoBulletInfo.RemainingLifetimeMS = remainingLifetimeMS;
            dtoBulletInfo.ShooterEntityId = shooterHost.Id;
            return dtoBulletInfo;
        }
    }
}
