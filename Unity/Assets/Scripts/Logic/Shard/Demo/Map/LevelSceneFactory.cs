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
        /// Creates a LevelScene and prepares its required resources without enabling it.
        /// </summary>
        /// <param name="parent">A parent within the owning World.</param>
        /// <param name="gameplayWorld">The World that owns the scene.</param>
        /// <param name="configId">The Level configuration identifier.</param>
        /// <param name="id">The scene identifier within its parent.</param>
        /// <param name="name">The scene name.</param>
        /// <returns>The prepared scene.</returns>
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

                    if (owner.IsEnabling || owner.IsEnabled)
                    {
                        throw new InvalidOperationException(
                            "LevelScenes cannot be created under an enabling or enabled parent tree.");
                    }

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
                levelGameWorldComponent.Initialize(gameplayWorld, configId);
                parent.AddChild(levelScene);

                await EventSystem.Instance.PushRequiredAsync(levelScene, new Event_AfterCreateLevelScene()
                {
                    LevelConfigId = configId,
                });

                levelGameWorldComponent.MarkPrepared();
                levelGameWorldComponent.ValidateOwnership(gameplayWorld);
                return levelScene;
            }
            catch (Exception exception)
            {
                List<Exception> cleanupExceptions = new List<Exception>();
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
                    throw new AggregateException("LevelScene preparation and cleanup failed.", cleanupExceptions);
                }

                throw;
            }
        }

        /// <summary>
        /// Enables the prepared LevelScene subtree from nested scenes toward its root.
        /// </summary>
        /// <param name="levelScene">The root of the prepared subtree to enable.</param>
        /// <param name="localUnit">Optional minimally prepared local client unit to complete during root enabling.</param>
        /// <exception cref="ArgumentNullException">The scene is null.</exception>
        /// <exception cref="InvalidOperationException">The scene tree is not ready to enable.</exception>
        /// <exception cref="AggregateException">Enabling and cleanup both fail.</exception>
        public static async ETTask Enable(LogicScene levelScene, Entity localUnit = null)
        {
            if (levelScene == null)
            {
                throw new ArgumentNullException(nameof(levelScene));
            }

            LevelGameWorldComponent rootAssociation = levelScene.GetComponent<LevelGameWorldComponent>();
            if (levelScene.SceneType != ELogicSceneType.Level || rootAssociation == null)
            {
                throw new InvalidOperationException("Only a LevelScene can be enabled.");
            }

            if (localUnit != null && !isDescendantOf(localUnit, levelScene))
            {
                throw new InvalidOperationException("The local client Unit must be owned by the LevelScene being enabled.");
            }

            for (Entity current = levelScene.Parent; current != null; current = current.Parent)
            {
                if (current is LogicScene ancestor && ancestor.SceneType == ELogicSceneType.Level)
                {
                    throw new InvalidOperationException(
                        "A nested LevelScene must be enabled through its root ancestor tree.");
                }
            }

            List<LogicScene> levelScenes = new List<LogicScene>();
            collectLevelScenes(levelScene, levelScenes);

            foreach (LogicScene scene in levelScenes)
            {
                LevelGameWorldComponent association = scene.GetComponent<LevelGameWorldComponent>();
                if (association == null)
                {
                    throw new InvalidOperationException("Every nested LevelScene must have a GameplayWorld association.");
                }

                association.ValidateForEnable(rootAssociation.GameplayWorld);
            }

            foreach (LogicScene scene in levelScenes)
            {
                scene.GetComponent<LevelGameWorldComponent>().BeginEnable();
            }

            try
            {
                foreach (LogicScene scene in levelScenes)
                {
                    Event_EnableLevelScene enableEvent = new Event_EnableLevelScene();
                    if (ReferenceEquals(scene, levelScene))
                    {
                        enableEvent.LocalUnit = localUnit;
                    }

                    await EventSystem.Instance.PushRequiredAsync(scene, enableEvent);
                }

                foreach (LogicScene scene in levelScenes)
                {
                    scene.GetComponent<LevelGameWorldComponent>().MarkEnabled();
                }
            }
            catch (Exception exception)
            {
                List<Exception> cleanupExceptions = new List<Exception>();
                for (int sceneIndex = levelScenes.Count - 1; sceneIndex >= 0; sceneIndex--)
                {
                    try
                    {
                        await EventSystem.Instance.PushRequiredAsync(
                            levelScenes[sceneIndex],
                            new Event_StopEnableLevelScene());
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
                    throw new AggregateException("LevelScene enabling and cleanup failed.", cleanupExceptions);
                }

                throw;
            }
        }

        private static void collectLevelScenes(Entity entity, List<LogicScene> levelScenes)
        {
            if (entity.ChildCount > 0)
            {
                foreach (Entity child in entity.Children.Values)
                {
                    collectLevelScenes(child, levelScenes);
                }
            }

            if (entity is LogicScene scene && scene.SceneType == ELogicSceneType.Level)
            {
                levelScenes.Add(scene);
            }
        }

        private static bool isDescendantOf(Entity entity, Entity ancestor)
        {
            for (Entity current = entity; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static LogicScene createLevelLogicScene(Entity parent, long id, string name)
        {
            return new LevelLogicScene(
                parent.RootScene().Root,
                id,
                IdGenerator.Instance.GenerateInstanceId(),
                name);
        }

        private sealed class LevelLogicScene : LogicScene
        {
            internal LevelLogicScene(Root root, long id, long instanceId, string name)
                : base(root, id, instanceId, ELogicSceneType.Level, name)
            {
            }
        }
    }
}