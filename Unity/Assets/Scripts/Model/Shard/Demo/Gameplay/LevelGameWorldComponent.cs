#nullable enable
using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// Associates a LevelScene with its owning GameplayWorld and stages effects until creation commits.
    /// </summary>
    public class LevelGameWorldComponent : Entity, IEarlyUpdate, IUpdate, ILateUpdate
    {
        private LevelSceneInitializationTransaction? initializationTransaction;
        private bool drivesGameplayWorld = true;
        internal bool isTransactionRoot;
        internal bool isInitialized;

        /// <summary>
        /// The GameplayWorld that owns this LevelScene.
        /// </summary>
        public GameplayWorld GameplayWorld = null!;

        /// <summary>
        /// Gets whether this LevelScene and its initialization transaction are committed.
        /// </summary>
        public bool IsCommitted { get; private set; } = true;

        /// <summary>
        /// Stages this association in the ancestor's transaction or starts a new transaction.
        /// </summary>
        /// <param name="gameplayWorld">The owning GameplayWorld.</param>
        /// <param name="parentAssociation">The parent LevelScene association, if nested.</param>
        public void BeginInitialization(GameplayWorld gameplayWorld, LevelGameWorldComponent? parentAssociation)
        {
            this.GameplayWorld = gameplayWorld;
            this.IsCommitted = false;
            this.drivesGameplayWorld = parentAssociation == null;
            this.isTransactionRoot = parentAssociation == null || parentAssociation.IsCommitted;
            this.initializationTransaction = parentAssociation != null && !parentAssociation.IsCommitted
                ? parentAssociation.initializationTransaction
                : new LevelSceneInitializationTransaction();
            if (this.initializationTransaction == null)
            {
                throw new InvalidOperationException("The ancestor LevelScene transaction is unavailable.");
            }

            this.initializationTransaction.AddScene(this, this.isTransactionRoot);
        }

        /// <summary>
        /// Defers an externally visible effect until this LevelScene's ancestor transaction commits.
        /// </summary>
        /// <param name="apply">Applies the effect during commit.</param>
        /// <param name="rollback">Reverses the effect if commit fails.</param>
        public void DeferUntilCommit(Action apply, Action rollback)
        {
            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }

            if (rollback == null)
            {
                throw new ArgumentNullException(nameof(rollback));
            }

            if (this.initializationTransaction == null || this.IsCommitted)
            {
                apply();
                return;
            }

            this.initializationTransaction.AddEffect(apply, rollback);
        }

        /// <summary>
        /// Publishes an external notification after all commit effects and scenes are activated.
        /// </summary>
        /// <param name="publish">The notification to publish after successful commit.</param>
        public void AfterCommit(Action publish)
        {
            if (publish == null)
            {
                throw new ArgumentNullException(nameof(publish));
            }

            if (this.initializationTransaction == null || this.IsCommitted)
            {
                publish();
                return;
            }

            this.initializationTransaction.AddAfterCommit(publish);
        }

        /// <summary>
        /// Defers Root update-system registration until this LevelScene commits.
        /// </summary>
        /// <param name="entity">The entity whose Root update registration is deferred.</param>
        /// <param name="register">Registers the entity with its Root update system.</param>
        /// <param name="unregister">Removes the entity from its Root update system.</param>
        /// <returns>True when the entity registration was deferred.</returns>
        /// <exception cref="ArgumentNullException">An entity or registration callback is null.</exception>
        public bool DeferEntityRegistration(
            Entity entity,
            Action<Entity> register,
            Action<Entity> unregister)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (register == null)
            {
                throw new ArgumentNullException(nameof(register));
            }

            if (unregister == null)
            {
                throw new ArgumentNullException(nameof(unregister));
            }

            if (this.IsCommitted)
            {
                return false;
            }

            this.DeferUntilCommit(
                () => register(entity),
                () => unregister(entity));
            return true;
        }

        /// <summary>
        /// Marks initialization complete and commits when the entire ancestor transaction is ready.
        /// </summary>
        public void CompleteInitialization()
        {
            if (this.initializationTransaction == null)
            {
                throw new InvalidOperationException("LevelScene initialization has not started.");
            }

            this.initializationTransaction.CompleteScene(this);
        }

        internal void ValidateForCommit()
        {
            LogicScene? levelScene = this.Scene();
            if (!this.isInitialized || this.IsDisposed || this.GameplayWorld == null ||
                this.GameplayWorld.IsDisposed || levelScene == null ||
                levelScene.SceneType != ELogicSceneType.Level ||
                !ReferenceEquals(levelScene.GetComponent<LevelGameWorldComponent>(), this) ||
                levelScene.Root != this.GameplayWorld.Root)
            {
                throw new InvalidOperationException("LevelScene commit preconditions are not satisfied.");
            }

            bool belongsToWorld = false;
            for (Entity? current = levelScene; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, this.GameplayWorld))
                {
                    belongsToWorld = true;
                    break;
                }

                if (current is LogicScene ancestor && ancestor.SceneType == ELogicSceneType.Level)
                {
                    LevelGameWorldComponent? ancestorAssociation = ancestor.GetComponent<LevelGameWorldComponent>();
                    if (ancestorAssociation == null ||
                        !ReferenceEquals(ancestorAssociation.GameplayWorld, this.GameplayWorld))
                    {
                        throw new InvalidOperationException("Nested LevelScenes must share the same GameplayWorld.");
                    }
                }
            }

            if (!belongsToWorld)
            {
                throw new InvalidOperationException("A LevelScene must remain within its GameplayWorld subtree until commit.");
            }
        }

        /// <summary>
        /// Aborts staged effects after initialization fails.
        /// </summary>
        public void AbortInitialization()
        {
            this.initializationTransaction?.Abort();
        }

        public void EarlyUpdate()
        {
            if (this.IsCommitted && this.drivesGameplayWorld)
            {
                this.GameplayWorld.EarlyUpdate();
            }
        }

        public void Update()
        {
            if (this.IsCommitted && this.drivesGameplayWorld)
            {
                this.GameplayWorld.Update();
            }
        }

        public void LateUpdate()
        {
            if (this.IsCommitted && this.drivesGameplayWorld)
            {
                this.GameplayWorld.LateUpdate();
            }
        }

        internal void Activate()
        {
            this.IsCommitted = true;
        }
    }

    internal sealed class LevelSceneInitializationTransaction
    {
        private readonly List<LevelGameWorldComponent> scenes = new List<LevelGameWorldComponent>();
        private readonly List<LevelSceneCommitEffect> effects = new List<LevelSceneCommitEffect>();
        private readonly List<LevelSceneCommitEffect> appliedEffects = new List<LevelSceneCommitEffect>();
        private readonly List<Action> afterCommitActions = new List<Action>();
        private int pendingScenes;
        private bool rootSceneInitialized;
        private bool isCompleted;
        private bool isCommitted;

        internal void AddScene(LevelGameWorldComponent scene, bool isTransactionRoot)
        {
            this.scenes.Add(scene);
            this.pendingScenes++;
            if (isTransactionRoot)
            {
                this.rootSceneInitialized = false;
            }
        }

        internal void AddEffect(Action apply, Action rollback)
        {
            if (this.isCompleted)
            {
                if (!this.isCommitted)
                {
                    throw new InvalidOperationException("The ancestor LevelScene initialization transaction was aborted.");
                }

                apply();
                return;
            }

            this.effects.Add(new LevelSceneCommitEffect(apply, rollback));
        }

        internal void AddAfterCommit(Action publish)
        {
            if (this.isCompleted)
            {
                if (!this.isCommitted)
                {
                    throw new InvalidOperationException("The ancestor LevelScene initialization transaction was aborted.");
                }

                publish();
                return;
            }

            this.afterCommitActions.Add(publish);
        }

        internal void CompleteScene(LevelGameWorldComponent scene)
        {
            if (this.isCompleted && !this.isCommitted)
            {
                throw new InvalidOperationException("The ancestor LevelScene initialization transaction was aborted.");
            }

            if (scene.isInitialized)
            {
                throw new InvalidOperationException("LevelScene initialization was already completed.");
            }

            scene.isInitialized = true;
            this.pendingScenes--;
            if (scene.isTransactionRoot)
            {
                this.rootSceneInitialized = true;
            }

            if (this.pendingScenes == 0 && this.rootSceneInitialized)
            {
                this.commit();
            }
        }

        internal void Abort()
        {
            if (this.isCompleted)
            {
                return;
            }

            try
            {
                this.rollbackAppliedEffects();
            }
            finally
            {
                this.effects.Clear();
                this.afterCommitActions.Clear();
                this.isCompleted = true;
            }
        }

        private void commit()
        {
            try
            {
                foreach (LevelGameWorldComponent scene in this.scenes)
                {
                    scene.ValidateForCommit();
                }

                foreach (LevelSceneCommitEffect effect in this.effects)
                {
                    this.appliedEffects.Add(effect);
                    effect.Apply();
                }

                foreach (LevelGameWorldComponent scene in this.scenes)
                {
                    scene.Activate();
                }

                this.isCompleted = true;
                this.isCommitted = true;
                this.effects.Clear();
                this.appliedEffects.Clear();
            }
            catch (Exception commitException)
            {
                try
                {
                    this.rollbackAppliedEffects();
                }
                catch (Exception rollbackException)
                {
                    this.effects.Clear();
                    this.afterCommitActions.Clear();
                    this.isCompleted = true;
                    throw new AggregateException("LevelScene commit and rollback failed.", commitException, rollbackException);
                }

                this.effects.Clear();
                this.afterCommitActions.Clear();
                this.isCompleted = true;
                throw;
            }

            foreach (Action publish in this.afterCommitActions)
            {
                try
                {
                    publish();
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            }

            this.afterCommitActions.Clear();
        }

        private void rollbackAppliedEffects()
        {
            List<Exception> rollbackExceptions = new List<Exception>();
            for (int effectIndex = this.appliedEffects.Count - 1; effectIndex >= 0; effectIndex--)
            {
                try
                {
                    this.appliedEffects[effectIndex].Rollback();
                }
                catch (Exception exception)
                {
                    rollbackExceptions.Add(exception);
                }
            }

            this.appliedEffects.Clear();
            if (rollbackExceptions.Count > 0)
            {
                throw new AggregateException("One or more LevelScene effects failed to roll back.", rollbackExceptions);
            }
        }
    }

    internal sealed class LevelSceneCommitEffect
    {
        internal Action Apply { get; }
        internal Action Rollback { get; }

        internal LevelSceneCommitEffect(Action apply, Action rollback)
        {
            this.Apply = apply;
            this.Rollback = rollback;
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
            var cmp = entity.GetSceneComponent<LevelGameWorldComponent>();
            return cmp?.GameplayWorld ?? null!;
        }
    }
}
