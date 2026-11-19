using System;
using System.Collections.Generic;
using Bliss.Physics;
using ET;

namespace Bliss
{
    /// <summary>Moves gameplay subtrees between LevelScenes owned by the same GameplayWorld.</summary>
    public static class GameplaySubtreeMigration
    {
        /// <summary>
        /// Moves a mounted child subtree to another prepared LevelScene in the same live World.
        /// Requests made during LogicUpdate complete after the tick; other valid requests apply immediately.
        /// On the client, Unity views and physical resources must be hosted under the persistent Global entities node.
        /// </summary>
        /// <param name="subtree">The live subtree root.</param>
        /// <param name="destination">The destination LevelScene.</param>
        /// <returns>A task completed when the migration has finished.</returns>
        /// <exception cref="InvalidOperationException">The scenes, hierarchy, or resources cannot be migrated.</exception>
        /// <exception cref="OperationCanceledException">The queued source or owning World was destroyed.</exception>
        public static ETTask MigrateAsync(Entity subtree, LogicScene destination)
        {
            ETTask task = ETTask.Create();
            MigrationRequest request;
            bool deferred;
            try
            {
                GameplayWorld world = validate(subtree, destination);
                request = new MigrationRequest(subtree, destination, world, task);
                deferred = world.LogicUpdate.deferUntilAfterTick(request);
            }
            catch (Exception exception)
            {
                task.SetException(exception);
                return task;
            }

            if (!deferred)
            {
                request.Execute();
            }

            return task;
        }

        private static GameplayWorld validate(Entity subtree, LogicScene destination)
        {
            if (subtree == null || subtree.IsDisposed || destination == null || destination.IsDisposed ||
                subtree.Parent == null || subtree is ILogicScene)
            {
                throw new InvalidOperationException("A live mounted subtree and destination LevelScene are required.");
            }

            LogicScene source = subtree.Scene();
            GameplayWorld world = subtree.GetLevelGameplayWorld();
            LevelGameWorldComponent sourceOwner = source?.GetComponent<LevelGameWorldComponent>();
            LevelGameWorldComponent destinationOwner = destination.GetComponent<LevelGameWorldComponent>();
            if (source == null || source.SceneType != ELogicSceneType.Level ||
                destination.SceneType != ELogicSceneType.Level || ReferenceEquals(source, destination) ||
                world == null || world.IsDisposed || world.IsFaulted ||
                sourceOwner == null || destinationOwner == null ||
                !sourceOwner.IsPrepared || sourceOwner.IsEnabling ||
                !destinationOwner.IsPrepared || destinationOwner.IsEnabling ||
                !ReferenceEquals(world, destinationOwner.GameplayWorld) ||
                source.GetComponent<PhysicalScene>() == null || destination.GetComponent<PhysicalScene>() == null)
            {
                throw new InvalidOperationException("Migration requires two prepared LevelScenes in the same live World.");
            }

            sourceOwner.ValidateOwnership(world);
            destinationOwner.ValidateOwnership(world);
            if (!ReferenceEquals(subtree.Parent.GetChild<Entity>(subtree.Id), subtree))
            {
                throw new InvalidOperationException("The subtree root must be a child rather than a component.");
            }

            for (Entity current = destination; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, subtree))
                {
                    throw new InvalidOperationException("The destination cannot be inside the migrating subtree.");
                }
            }

            return world;
        }

        private static void apply(Entity entity, LogicScene source, LogicScene destination)
        {
            if (destination.GetChild<Entity>(entity.Id) != null)
            {
                throw new InvalidOperationException("The destination already contains the subtree identifier.");
            }

            PhysicalScene sourcePhysics = source.GetComponent<PhysicalScene>();
            PhysicalScene destinationPhysics = destination.GetComponent<PhysicalScene>();
            List<Bullet> bullets = new List<Bullet>();
            collectBullets(entity, source, sourcePhysics, bullets);
#if DOTNET
            GameplaySubtreePhysicsMigration physics = GameplaySubtreePhysicsMigration.prepare(entity,
                sourcePhysics, destinationPhysics);
            physics.move();
            try
            {
                entity.migrateLevelSceneParent(destination);
            }
            catch (Exception exception)
            {
                try
                {
                    physics.rollback();
                }
                catch (Exception rollbackException)
                {
                    throw new GameplayMigrationRollbackException("Hierarchy migration and PhysX rollback failed.",
                        exception, rollbackException);
                }

                throw;
            }
#else
            GameplaySubtreePhysicsMigrationClient.validate(entity, source);
            entity.migrateLevelSceneParent(destination);
#endif
            foreach (Bullet bullet in bullets)
            {
                bullet.LevelScene = destination;
                bullet.PhysicalScene = destinationPhysics;
            }
        }

        private static void collectBullets(Entity entity, LogicScene source, PhysicalScene sourcePhysics, List<Bullet> bullets)
        {
            if (entity is ILogicScene)
            {
                return;
            }

            if (entity is Bullet bullet)
            {
                if (!ReferenceEquals((LogicScene)bullet.LevelScene, source) ||
                    !ReferenceEquals((PhysicalScene)bullet.PhysicalScene, sourcePhysics))
                {
                    throw new InvalidOperationException("A Bullet references a different LevelScene or physical scene.");
                }

                bullets.Add(bullet);
            }

            if (entity.ComponentCount > 0)
            {
                foreach (Entity component in entity.Components.Values)
                {
                    collectBullets(component, source, sourcePhysics, bullets);
                }
            }

            if (entity.ChildCount > 0)
            {
                foreach (Entity child in entity.Children.Values)
                {
                    collectBullets(child, source, sourcePhysics, bullets);
                }
            }
        }

        private sealed class MigrationRequest : IGameplayWorldDeferredWork
        {
            private readonly EntityRef<Entity> subtree;
            private readonly EntityRef<LogicScene> source;
            private readonly EntityRef<LogicScene> destination;
            private readonly EntityRef<GameplayWorld> world;
            private readonly ETTask task;

            /// <summary>Captures the original owner and completion task for execution-time validation.</summary>
            public MigrationRequest(Entity subtree, LogicScene destination, GameplayWorld world, ETTask task)
            {
                this.subtree = subtree;
                this.source = subtree.Scene();
                this.destination = destination;
                this.world = world;
                this.task = task;
            }

            /// <inheritdoc />
            public Entity Owner => (Entity)this.subtree;

            /// <inheritdoc />
            public void Execute()
            {
                try
                {
                    Entity entity = (Entity)this.subtree;
                    LogicScene oldScene = (LogicScene)this.source;
                    GameplayWorld originalWorld = (GameplayWorld)this.world;
                    if (entity == null || oldScene == null || originalWorld == null)
                    {
                        throw new OperationCanceledException("The migration source was destroyed.");
                    }

                    if (!ReferenceEquals(entity.Scene(), oldScene))
                    {
                        throw new InvalidOperationException("The subtree changed LevelScenes before migration.");
                    }

                    LogicScene target = (LogicScene)this.destination;
                    if (!ReferenceEquals(validate(entity, target), originalWorld))
                    {
                        throw new InvalidOperationException("The subtree changed World before migration.");
                    }

                    apply(entity, oldScene, target);
                }
                catch (Exception exception)
                {
                    if (exception is GameplayMigrationRollbackException)
                    {
                        ((GameplayWorld)this.world)?.StopWithFailure(exception);
                    }

                    this.task.SetException(exception);
                    return;
                }

                this.task.SetResult();
            }

            /// <inheritdoc />
            public void Cancel()
            {
                this.task.SetException(new OperationCanceledException("The owning World stopped before migration."));
            }
        }

        internal sealed class GameplayMigrationRollbackException : AggregateException
        {
            internal GameplayMigrationRollbackException(string message, Exception failure, Exception rollbackFailure)
                : base(message, failure, rollbackFailure)
            {
            }
        }
    }
}
