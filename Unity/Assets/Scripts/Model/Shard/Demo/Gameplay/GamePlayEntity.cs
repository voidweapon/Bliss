using System;

namespace Bliss
{
    /// <summary>An entity whose updates belong to its associated GameplayWorld.</summary>
    public class GamePlayEntity : Entity
    {
        private GameplayWorld registeredLogicWorld;

        /// <summary>
        /// Registers one primary fixed-step callback, named after this entity's full type name.
        /// The callback is automatically unregistered before disposal.
        /// </summary>
        /// <param name="priority">Execution priority within the owning World.</param>
        /// <param name="callback">Receives the live entity and fixed delta; prefer a cached static method.</param>
        /// <exception cref="InvalidOperationException">The entity has no World or already registered its primary callback.</exception>
        /// <exception cref="ArgumentNullException">The callback is null.</exception>
        /// <exception cref="ObjectDisposedException">The entity or World scheduler has been disposed.</exception>
        public void RegisterLogicUpdate(int priority, Action<Entity, float> callback)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(GamePlayEntity));
            }

            if (this.registeredLogicWorld != null)
            {
                throw new InvalidOperationException("The gameplay entity already registered its primary LogicUpdate callback.");
            }

            GameplayWorld world = this.GetLevelGameplayWorld()
                ?? throw new InvalidOperationException("The gameplay entity must belong to a GameplayWorld.");
            world.LogicUpdate.Register(this, this.GetType().FullName, priority, callback);
            this.registeredLogicWorld = world;
        }

        /// <summary>Unregisters the primary fixed-step callback before disposing the entity hierarchy.</summary>
        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            // Entity.Dispose clears InstanceId before invoking Destroy systems, so unregister first.
            if (this.registeredLogicWorld != null)
            {
                this.registeredLogicWorld.LogicUpdate.Unregister(this, this.GetType().FullName);
                this.registeredLogicWorld = null;
            }

            base.Dispose();
        }

        protected override void RegisterToSystem()
        {
            GameplayWorld gameplayWorld = this.LogicScene as GameplayWorld;
            if (gameplayWorld == null &&
                this.LogicScene is LogicScene levelScene && levelScene.SceneType == ELogicSceneType.Level)
            {
                LevelGameWorldComponent association = levelScene.GetComponent<LevelGameWorldComponent>();
                gameplayWorld = association?.GameplayWorld;
                if (gameplayWorld != null)
                {
                    association.ValidateOwnership(gameplayWorld);
                }
            }

            if (gameplayWorld == null)
            {
                string sceneType = LogicScene?.GetType().FullName ?? "<null>";
                throw new InvalidOperationException(
                    $"Gameplay entity '{GetType().FullName}' must belong to '{nameof(GameplayWorld)}', but its scene is '{sceneType}'.");
            }

            gameplayWorld.Register(this);
        }
    }
}