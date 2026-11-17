#nullable enable
namespace Bliss
{
    /// <summary>
    /// Associates a LevelScene with its owning GameplayWorld and tracks its lifecycle.
    /// </summary>
    public class LevelGameWorldComponent : Entity, IEarlyUpdate, IUpdate, ILateUpdate
    {
        private bool drivesGameplayWorld;
        private bool isEnabling;

        /// <summary>
        /// The GameplayWorld that owns this LevelScene.
        /// </summary>
        public GameplayWorld GameplayWorld = null!;

        /// <summary>
        /// Gets the configuration identifier used to prepare this LevelScene.
        /// </summary>
        public int LevelConfigId { get; private set; }

        /// <summary>
        /// Gets whether preparation of this LevelScene completed successfully.
        /// </summary>
        public bool IsPrepared { get; private set; }

        /// <summary>
        /// Gets whether this LevelScene has completed enabling.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Gets whether the owning LevelScene tree is currently being enabled.
        /// </summary>
        public bool IsEnabling => this.isEnabling;

        /// <summary>
        /// Initializes ownership and configuration before LevelScene preparation begins.
        /// </summary>
        /// <param name="gameplayWorld">The world that owns the LevelScene.</param>
        /// <param name="levelConfigId">The configuration identifier used to prepare the LevelScene.</param>
        /// <param name="drivesGameplayWorld">Whether this association forwards update phases to the world.</param>
        public void Initialize(GameplayWorld gameplayWorld, int levelConfigId, bool drivesGameplayWorld)
        {
            this.GameplayWorld = gameplayWorld;
            this.LevelConfigId = levelConfigId;
            this.drivesGameplayWorld = drivesGameplayWorld;
        }

        /// <summary>
        /// Marks preparation complete after all required resources are ready.
        /// </summary>
        public void MarkPrepared()
        {
            if (this.IsPrepared || this.IsEnabled || this.isEnabling)
            {
                throw new System.InvalidOperationException("LevelScene preparation has already completed.");
            }

            this.IsPrepared = true;
        }

        /// <summary>
        /// Verifies that this LevelScene remains within its owning GameplayWorld subtree.
        /// </summary>
        /// <param name="gameplayWorld">The expected owning world.</param>
        /// <exception cref="System.InvalidOperationException">The association or ownership is invalid.</exception>
        public void ValidateOwnership(GameplayWorld gameplayWorld)
        {
            LogicScene? levelScene = this.Scene();
            if (this.IsDisposed || this.GameplayWorld == null ||
                !ReferenceEquals(this.GameplayWorld, gameplayWorld) || gameplayWorld.IsDisposed ||
                levelScene == null || levelScene.SceneType != ELogicSceneType.Level ||
                !ReferenceEquals(levelScene.GetComponent<LevelGameWorldComponent>(), this) ||
                levelScene.Root != gameplayWorld.Root)
            {
                throw new System.InvalidOperationException("LevelScene ownership preconditions are not satisfied.");
            }

            bool belongsToWorld = false;
            for (Entity? current = levelScene; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, gameplayWorld))
                {
                    belongsToWorld = true;
                    break;
                }

                if (current is LogicScene ancestor && ancestor.SceneType == ELogicSceneType.Level)
                {
                    LevelGameWorldComponent? ancestorAssociation = ancestor.GetComponent<LevelGameWorldComponent>();
                    if (ancestorAssociation == null ||
                        !ReferenceEquals(ancestorAssociation.GameplayWorld, gameplayWorld))
                    {
                        throw new System.InvalidOperationException("Nested LevelScenes must share the same GameplayWorld.");
                    }
                }
            }

            if (!belongsToWorld)
            {
                throw new System.InvalidOperationException("A LevelScene must remain within its GameplayWorld subtree.");
            }
        }

        /// <summary>
        /// Verifies that this prepared LevelScene can participate in enabling.
        /// </summary>
        /// <param name="gameplayWorld">The expected owning world.</param>
        /// <exception cref="System.InvalidOperationException">The scene is not ready to enable.</exception>
        public void ValidateForEnable(GameplayWorld gameplayWorld)
        {
            if (!this.IsPrepared || this.IsEnabled || this.isEnabling)
            {
                throw new System.InvalidOperationException("LevelScene enable preconditions are not satisfied.");
            }

            this.ValidateOwnership(gameplayWorld);
        }

        /// <summary>
        /// Marks this LevelScene as participating in an in-progress tree enable.
        /// </summary>
        public void BeginEnable()
        {
            this.isEnabling = true;
        }

        /// <summary>
        /// Marks this LevelScene enabled after every scene in its tree succeeds.
        /// </summary>
        public void MarkEnabled()
        {
            if (!this.isEnabling || this.IsEnabled)
            {
                throw new System.InvalidOperationException("LevelScene enabling has not started or already completed.");
            }

            this.isEnabling = false;
            this.IsEnabled = true;
        }

        /// <summary>
        /// Advances the owning GameplayWorld during its early update phase after enabling.
        /// </summary>
        public void EarlyUpdate()
        {
            if (this.IsEnabled && this.drivesGameplayWorld)
            {
                this.GameplayWorld.EarlyUpdate();
            }
        }

        /// <summary>
        /// Advances the owning GameplayWorld during its update phase after enabling.
        /// </summary>
        public void Update()
        {
            if (this.IsEnabled && this.drivesGameplayWorld)
            {
                this.GameplayWorld.Update();
            }
        }

        /// <summary>
        /// Advances the owning GameplayWorld during its late update phase after enabling.
        /// </summary>
        public void LateUpdate()
        {
            if (this.IsEnabled && this.drivesGameplayWorld)
            {
                this.GameplayWorld.LateUpdate();
            }
        }
    }

    /// <summary>
    /// Resolves the GameplayWorld associated with an entity's scene.
    /// </summary>
    public static class LevelGameWorldComponentHelper
    {
        /// <summary>
        /// Returns the GameplayWorld associated with the entity's scene, if one exists.
        /// </summary>
        /// <param name="entity">The entity whose scene is inspected.</param>
        /// <returns>The associated GameplayWorld, or null when no association exists.</returns>
        public static GameplayWorld GetLevelGameplayWorld(this Entity entity)
        {
            if (entity.Scene() is GameplayWorld gameplayWorld) return gameplayWorld;
            var component = entity.GetSceneComponent<LevelGameWorldComponent>();
            return component?.GameplayWorld ?? null!;
        }
    }
}
