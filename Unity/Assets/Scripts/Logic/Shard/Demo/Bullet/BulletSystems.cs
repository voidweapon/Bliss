using System;
using Bliss.Core;
using Bliss.Physics;
using Bliss.Timer;
using Unity.Mathematics;

namespace Bliss
{
    [EntitySystemOf(typeof(Bullet))]
    public static partial class BulletSystems
    {
        private const long NO_CONFIG_ID = 0;
        private const long NO_TIMER_ID = 0;
        private const long INFINITE_LIFETIME_MILLISECONDS = 0;
        private const float COLLISION_RADIUS = 0.5f;
        private const float NO_TRAVEL_DISTANCE = 0f;
        private const int ALL_PHYSICS_LAYERS = -1;
        private const int NO_DESTROY_REASON = 0;
        private const string PATH_CHECK_ENTRY_NAME = "BulletPathCheck";
#if DOTNET
        private static readonly Action<Entity, float> spawnCheckCallback = checkSpawnOverlap;
        private static readonly Action<Entity, float> pathCheckCallback = checkPathCollision;
#endif

        private static void Awake(this Bullet self, BulletCreateInfo createInfo)
        {
            self.BulletConfigId = createInfo.BulletConfigId;
            self.TargetInfo = createInfo.TargetInfo;
            self.ShooterHost = createInfo.ShooterHost;
            self.PhysicalScene = createInfo.PhysicalScene;
            self.LevelScene = createInfo.LevelScene;

            GameplayWorld world = self.GetLevelGameplayWorld()
                ?? throw new InvalidOperationException("Bullet must belong to a GameplayWorld.");
            Entity shooterHost = (Entity)createInfo.ShooterHost;
            ShooterGear shooterGear = shooterHost?.GetComponent<ShooterGear>();
            PhysicalScene physicalScene = (PhysicalScene)createInfo.PhysicalScene;
            LogicScene levelScene = (LogicScene)createInfo.LevelScene;
            BulletConfig config = self.GetConfig();

            if (shooterHost == null || shooterHost.IsDisposed || shooterGear == null || shooterGear.IsDisposed ||
                !ReferenceEquals(shooterGear.Parent, shooterHost) ||
                !ReferenceEquals(shooterHost.GetLevelGameplayWorld(), world))
            {
                throw new InvalidOperationException("Bullet requires a live ShooterGear and its host in the same GameplayWorld.");
            }

            if (physicalScene == null || physicalScene.IsDisposed ||
                !ReferenceEquals(physicalScene.Scene(), levelScene) ||
                levelScene.IsDisposed ||
                !ReferenceEquals(levelScene.GetComponent<LevelGameWorldComponent>()?.GameplayWorld, world))
            {
                throw new InvalidOperationException("Bullet requires the live PhysicalScene associated with its GameplayWorld.");
            }

            long remainingLifetimeMS = config.DurationMS > INFINITE_LIFETIME_MILLISECONDS
                ? createInfo.RemainingLifetimeMS
                : INFINITE_LIFETIME_MILLISECONDS;
            if (config.DurationMS > INFINITE_LIFETIME_MILLISECONDS &&
                remainingLifetimeMS <= INFINITE_LIFETIME_MILLISECONDS)
            {
                throw new InvalidOperationException("A finite-lived Bullet requires positive remaining lifetime.");
            }

            NetworkEntityManagerComponent networkEntityManager =
                self.RootScene().GetComponent<NetworkEntityManagerComponent>()
                ?? throw new InvalidOperationException("Bullet requires a NetworkEntityManagerComponent.");
            BulletManager bulletManager = world.GetComponent<BulletManager>()
                ?? throw new InvalidOperationException("Bullet requires a BulletManager in its GameplayWorld.");

            networkEntityManager.AddEntity(self.Id, self);
            bulletManager.Add(self);

            if (config.DurationMS > INFINITE_LIFETIME_MILLISECONDS)
            {
                self.ExpirationTimeMS = TimeInfo.Instance.ServerFrameTime() + remainingLifetimeMS;
#if DOTNET
                self.TimeoutTimerId = world.Timer.ScheduleOnce(remainingLifetimeMS, self.timeout, self);
#endif
            }

#if DOTNET
            self.RegisterLogicUpdate(GameplayWorldLogicPriority.BULLET_SPAWN_CHECK, spawnCheckCallback);
            world.LogicUpdate.Register(self, PATH_CHECK_ENTRY_NAME, GameplayWorldLogicPriority.BULLET_PATH_CHECK,
                pathCheckCallback);
#endif
        }

        private static void Destroy(this Bullet self)
        {
            GameplayWorld world = self.GetLevelGameplayWorld();
            world?.Timer.Cancel(ref self.TimeoutTimerId);

            NetworkEntityManagerComponent networkEntityManager =
                self.RootScene().GetComponent<NetworkEntityManagerComponent>();
            networkEntityManager?.RemoveEntity(self.Id);

            BulletManager bulletManager = world?.GetComponent<BulletManager>();
            bulletManager?.Remove(self.Id);

            self.BulletConfigId = (int)NO_CONFIG_ID;
        }

        /// <summary>Broadcasts the authoritative destruction of this Bullet and removes it from its World.</summary>
        /// <param name="self">The Bullet to destroy.</param>
        /// <param name="reason">The gameplay reason for destruction.</param>
        public static void DestroyBullet(this Bullet self, int reason)
        {
            if (self.IsDisposed)
            {
                return;
            }

            GameplayWorld world = self.GetLevelGameplayWorld();
            world?.LogicUpdate.Unregister(self, PATH_CHECK_ENTRY_NAME);
            self.UnregisterLogicUpdate();

#if DOTNET
            TransformComponent transform = self.GetComponent<TransformComponent>();
            M2C_DestroyEntities message = M2C_DestroyEntities.Create();
            message.EntityIds.Add(self.Id);
            MapMessageHelper.BroadcastAll((LogicScene)self.LevelScene, transform.Transform.Location, message);
#endif
            self.Dispose();
        }

        /// <summary>Returns the Bullet configuration or fails when its identifier is invalid.</summary>
        /// <param name="self">The Bullet whose configuration is required.</param>
        /// <returns>The matching configuration.</returns>
        /// <exception cref="InvalidOperationException">The configuration table has no entry for this Bullet.</exception>
        public static BulletConfig GetConfig(this Bullet self)
        {
            return ConfigManager.Instance.Tables.TbBulletConfig.GetOrDefault(self.BulletConfigId)
                ?? throw new InvalidOperationException($"Bullet configuration {self.BulletConfigId} was not found.");
        }

        /// <summary>Starts the Bullet's configured Mover and records its initial position.</summary>
        /// <param name="self">The Bullet to launch.</param>
        /// <param name="launchInfo">The target information used by its Mover.</param>
        public static void Launch(this Bullet self, GameplayTargetInfo launchInfo)
        {
            MoverComponent mover = self.GetComponent<MoverComponent>();
            mover.LaunchMover(self.GetConfig().MoverId, launchInfo);

            TransformComponent transform = self.GetComponent<TransformComponent>();
            self.PreviousPosition = transform.Transform.Location;
        }

#if DOTNET
        private static void checkSpawnOverlap(Entity entity, float deltaTime)
        {
            Bullet bullet = (Bullet)entity;
            PhysicalScene physicalScene = getPhysicalScene(bullet);
            Entity shooterHost = (Entity)bullet.ShooterHost;
            if (shooterHost == null || shooterHost.IsDisposed)
            {
                shooterHost = bullet;
            }

            TransformComponent transform = bullet.GetComponent<TransformComponent>();
            bool hasOverlap = physicalScene.OverlapSpere(transform.Transform.Location, COLLISION_RADIUS,
                ALL_PHYSICS_LAYERS, bullet, shooterHost, out _);
            bullet.UnregisterLogicUpdate();
            if (hasOverlap)
            {
                bullet.DestroyBullet(NO_DESTROY_REASON);
            }
        }

        private static void checkPathCollision(Entity entity, float deltaTime)
        {
            Bullet bullet = (Bullet)entity;
            PhysicalScene physicalScene = getPhysicalScene(bullet);
            TransformComponent transform = bullet.GetComponent<TransformComponent>();
            float3 currentPosition = transform.Transform.Location;
            float3 direction = currentPosition - bullet.PreviousPosition;
            float distance = math.length(direction);
            float3 previousPosition = bullet.PreviousPosition;
            bullet.PreviousPosition = currentPosition;

            if (distance <= NO_TRAVEL_DISTANCE)
            {
                return;
            }

            Entity shooterHost = (Entity)bullet.ShooterHost;
            if (shooterHost == null || shooterHost.IsDisposed)
            {
                shooterHost = bullet;
            }

            bool hasHit = physicalScene.SpereCast(previousPosition, COLLISION_RADIUS, direction / distance, distance,
                ALL_PHYSICS_LAYERS, bullet, shooterHost, out _);
            if (hasHit)
            {
                bullet.DestroyBullet(NO_DESTROY_REASON);
            }
        }

        private static PhysicalScene getPhysicalScene(Bullet bullet)
        {
            PhysicalScene physicalScene = (PhysicalScene)bullet.PhysicalScene;
            if (physicalScene == null || physicalScene.IsDisposed)
            {
                throw new InvalidOperationException("Bullet's PhysicalScene is unavailable.");
            }

            return physicalScene;
        }

        private static void timeout(this Bullet self)
        {
            self.DestroyBullet(NO_DESTROY_REASON);
        }
#endif
    }
}
