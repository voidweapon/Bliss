using System;
using System.Collections.Generic;
using Bliss.Id;
using ET;

namespace Bliss
{
    /// <summary>
    /// 游戏场景工厂
    /// </summary>
    public static class LevelSceneFactory
    {
        /// <summary>
        /// Creates and initializes a LevelScene under its owning GameplayWorld.
        /// </summary>
        /// <param name="parent">A parent within the owning World.</param>
        /// <param name="gameplayWorld">The World that owns the scene.</param>
        /// <param name="configId">The Level configuration identifier.</param>
        /// <param name="id">The scene identifier within its parent.</param>
        /// <param name="name">The scene name.</param>
        /// <returns>The initialized scene.</returns>
        public static async ETTask<LogicScene> Create(Entity parent, GameplayWorld gameplayWorld, int configId, long id, string name)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (gameplayWorld == null)
            {
                throw new ArgumentNullException(nameof(gameplayWorld));
            }

            if (parent.IsDisposed || gameplayWorld.IsDisposed || parent.LogicScene == null ||
                parent.LogicScene.Root != gameplayWorld.Root)
            {
                throw new InvalidOperationException("The LevelScene parent and GameplayWorld must be alive and share a Root.");
            }

            bool belongsToWorld = false;
            LevelGameWorldComponent parentAssociation = null;
            for (Entity current = parent; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, gameplayWorld))
                {
                    belongsToWorld = true;
                    break;
                }

                if (current is LogicScene scene && scene.SceneType == ELogicSceneType.Level)
                {
                    LevelGameWorldComponent owner = scene.GetComponent<LevelGameWorldComponent>();
                    if (owner == null || !ReferenceEquals(owner.GameplayWorld, gameplayWorld))
                    {
                        throw new InvalidOperationException("Nested LevelScenes must share the same GameplayWorld.");
                    }

                    parentAssociation ??= owner;
                }
            }

            if (!belongsToWorld)
            {
                throw new InvalidOperationException("A LevelScene must be within its GameplayWorld subtree.");
            }

            if (parent.GetChild<Entity>(id) != null)
            {
                throw new InvalidOperationException($"A child with identifier {id} already exists under the LevelScene parent.");
            }

            LogicScene levelScene = createLevelLogicScene(parent, id, name);
            LevelGameWorldComponent levelGameWorldComponent = null!;
            try
            {
                levelGameWorldComponent = levelScene.AddComponent<LevelGameWorldComponent>();
                levelGameWorldComponent.BeginInitialization(gameplayWorld, parentAssociation);
                parent.AddChild(levelScene);

                await EventSystem.Instance.PushRequiredAsync(levelScene, new Event_AfterCreateLevelScene()
                {
                    LevelConfigId = configId,
                });

                levelGameWorldComponent.CompleteInitialization();
                return levelScene;
            }
            catch (Exception exception)
            {
                List<Exception> cleanupExceptions = new List<Exception>();
                if (levelGameWorldComponent != null)
                {
                    try
                    {
                        levelGameWorldComponent.AbortInitialization();
                    }
                    catch (Exception cleanupException)
                    {
                        cleanupExceptions.Add(cleanupException);
                    }
                }

                try
                {
                    levelScene.Dispose();
                }
                catch (Exception cleanupException)
                {
                    cleanupExceptions.Add(cleanupException);
                }

                if (cleanupExceptions.Count > 0)
                {
                    cleanupExceptions.Insert(0, exception);
                    throw new AggregateException("LevelScene initialization and cleanup failed.", cleanupExceptions);
                }

                throw;
            }
        }

        private static LogicScene createLevelLogicScene(Entity parent, long id, string name)
        {
            return new LevelLogicScene(
                parent.RootScene().Root,
                id,
                IdGenerator.Instance.GenerateInstanceId(),
                name);
        }

        private sealed class LevelLogicScene : LogicScene, IEntityRegistrationDeferral
        {
            internal LevelLogicScene(Root root, long id, long instanceId, string name)
                : base(root, id, instanceId, ELogicSceneType.Level, name)
            {
            }

            public bool DeferRegistration(
                Entity entity,
                Action<Entity> register,
                Action<Entity> unregister)
            {
                LevelGameWorldComponent association = this.GetComponent<LevelGameWorldComponent>();
                return association != null &&
                       association.DeferEntityRegistration(entity, register, unregister);
            }
        }
    }
}