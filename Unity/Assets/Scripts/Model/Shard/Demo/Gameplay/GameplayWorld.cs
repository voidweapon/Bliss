using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Bliss
{
    internal interface IGameplayWorldDeferredWork
    {
        /// <summary>Gets the subtree used to reject overlapping requests.</summary>
        Entity Owner { get; }
        /// <summary>Executes the request after a successful tick.</summary>
        void Execute();
        /// <summary>Marks a request whose subtree overlaps another pending request.</summary>
        void MarkOverlapping();
        /// <summary>Cancels a request when its World cannot execute it.</summary>
        void Cancel();
    }

    /// <summary>Owns gameplay updates, timers and fixed-step simulation within a Root.</summary>
    public class GameplayWorld : LogicScene
    {
        /// <summary>Gets the updater associated with this World and its fault state.</summary>
        public GameplayWorldUpdater Updater { get; }

        /// <summary>
        /// Gets the timer service owned by this world.
        /// </summary>
        public GameplayWorldTimer Timer { get; }

        /// <summary>Gets the fixed-step simulation scheduler owned by this world.</summary>
        public GameplayWorldLogicUpdate LogicUpdate { get; }

        /// <summary>Gets the failure that stopped this World's updates, or null if no failure was recorded.</summary>
        public Exception Failure { get; private set; }

        /// <summary>Gets whether this World has been stopped after a failure.</summary>
        public bool IsFaulted => this.Failure != null;

        internal GameplayWorld(Root root, long id, long instanceId, string name)
            : base(root, id, instanceId, ELogicSceneType.GameplayWorld, name)
        {
            this.Updater = new GameplayWorldUpdater(this);
            this.Timer = new GameplayWorldTimer(this);
            this.LogicUpdate = new GameplayWorldLogicUpdate(this, this.Timer);
        }
        
        public void Register(Entity entity)
        {
            Updater.Add(entity);
        }

        public void Unregister(Entity entity)
        {
        }

        /// <summary>Runs the World's early updates unless the World is faulted.</summary>
        public void EarlyUpdate()
        {
            Updater.EarlyUpdate();
        }

        /// <summary>Runs ordinary updates unless faulted; fixed ticks are driven independently by Root.</summary>
        public void Update()
        {
            Updater.Update();
        }

        /// <summary>Runs the World's late updates unless the World is faulted.</summary>
        public void LateUpdate()
        {
            Updater.LateUpdate();
        }

        /// <summary>
        /// Stops World-owned updates and retains the first failure for diagnosis. If called during
        /// an update, the caller must propagate the failure to abort the current dispatch.
        /// Root timers and external messages are not stopped by this method.
        /// </summary>
        /// <param name="failure">The cause of the failure; it is not automatically retried.</param>
        /// <exception cref="ArgumentNullException">The failure is null.</exception>
        public void StopWithFailure(Exception failure)
        {
            if (failure == null)
            {
                throw new ArgumentNullException(nameof(failure));
            }

            this.Failure ??= failure;
        }

        /// <summary>Disposes the world and clears its timers and pending simulation ticks.</summary>
        /// <exception cref="AggregateException">Multiple disposal callbacks or cleanup operations failed.</exception>
        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            List<Exception> failures = null;
            try
            {
                this.Timer.dispose();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }

            try
            {
                this.LogicUpdate.dispose();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }

            try
            {
                base.Dispose();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }

            if (failures?.Count == 1)
            {
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            if (failures != null)
            {
                throw new AggregateException("GameplayWorld disposal failed.", failures);
            }
        }
    }

    internal sealed class GameplayWorldDriver : Entity, IEarlyUpdate, IUpdate, ILateUpdate
    {
    }

    [EntitySystem]
    internal sealed class GameplayWorldEarlyDriverSystem : EarlyUpdateSystem<GameplayWorldDriver>
    {
        protected override void EarlyUpdate(GameplayWorldDriver entity)
        {
            ((GameplayWorld)entity.Parent).EarlyUpdate();
        }
    }

    internal sealed class GameplayWorldSimulationDriver : Entity, IUpdate
    {
    }

    [EntitySystem]
    internal sealed class GameplayWorldTimerDriverSystem : UpdateSystem<GameplayWorldSimulationDriver>
    {
        private const int TIMER_PRIORITY = int.MinValue;

        /// <inheritdoc />
        public override int GetPriority() => TIMER_PRIORITY;

        /// <inheritdoc />
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE;

        protected override void Update(GameplayWorldSimulationDriver entity)
        {
            ((GameplayWorld)entity.Parent).Timer.update();
        }
    }

    [EntitySystem]
    internal sealed class GameplayWorldLogicDriverSystem : UpdateSystem<GameplayWorldSimulationDriver>
    {
        private const int LOGIC_PRIORITY = int.MinValue + 1;

        /// <inheritdoc />
        public override int GetPriority() => LOGIC_PRIORITY;

        /// <inheritdoc />
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE;

        protected override void Update(GameplayWorldSimulationDriver entity)
        {
            ((GameplayWorld)entity.Parent).LogicUpdate.update();
        }
    }

    [EntitySystem]
    internal sealed class GameplayWorldUpdateDriverSystem : UpdateSystem<GameplayWorldDriver>
    {
        private const int UPDATE_PRIORITY = int.MinValue + 2;

        /// <inheritdoc />
        public override int GetPriority() => UPDATE_PRIORITY;

        /// <inheritdoc />
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE;

        protected override void Update(GameplayWorldDriver entity)
        {
            ((GameplayWorld)entity.Parent).Update();
        }
    }

    [EntitySystem]
    internal sealed class GameplayWorldLateDriverSystem : LateUpdateSystem<GameplayWorldDriver>
    {
        protected override void LateUpdate(GameplayWorldDriver entity)
        {
            ((GameplayWorld)entity.Parent).LateUpdate();
        }
    }

    /// <summary>Dispatches fixed-step simulation callbacks for one world.</summary>
    public sealed class GameplayWorldLogicUpdate
    {
        /// <summary>Fixed simulation interval in milliseconds.</summary>
        public const long STEP_MILLISECONDS = 50;
        /// <summary>Fixed simulation delta passed to callbacks, in seconds.</summary>
        public const float STEP_SECONDS = 0.05f;
        private readonly GameplayWorld world;
        // Keys identify an entity instance and entry name; values are active registrations.
        private readonly Dictionary<LogicUpdateKey, LogicUpdateEntry> entriesByKey =
            new Dictionary<LogicUpdateKey, LogicUpdateEntry>();
        // Keys are priorities; values preserve registration order within each priority.
        private readonly SortedDictionary<int, LogicUpdateBucket> bucketsByPriority =
            new SortedDictionary<int, LogicUpdateBucket>();
        private readonly List<LogicUpdateEntry> dispatchSnapshot = new List<LogicUpdateEntry>();
        private readonly Stack<LogicUpdateEntry> availableEntries = new Stack<LogicUpdateEntry>();
        private readonly Stack<LogicUpdateEntry> retiredEntries = new Stack<LogicUpdateEntry>();
        private readonly Queue<IGameplayWorldDeferredWork> deferredWork = new Queue<IGameplayWorldDeferredWork>();
        private bool hasPendingTick;
        private bool isDisposed;
        private bool isDispatching;

        internal bool deferUntilAfterTick(IGameplayWorldDeferredWork work)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameplayWorldLogicUpdate));
            }

            foreach (IGameplayWorldDeferredWork pending in this.deferredWork)
            {
                if (isAncestor(pending.Owner, work.Owner) || isAncestor(work.Owner, pending.Owner))
                {
                    throw new InvalidOperationException("A migration is already pending for an overlapping subtree.");
                }
            }

            if (!this.isDispatching)
            {
                return false;
            }

            this.deferredWork.Enqueue(work);
            return true;
        }

        private static bool isAncestor(Entity ancestor, Entity descendant)
        {
            for (Entity current = descendant; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        internal GameplayWorldLogicUpdate(GameplayWorld world, GameplayWorldTimer timer)
        {
            this.world = world;
            timer.ScheduleRepeated(STEP_MILLISECONDS, this.markTick);
        }

        /// <summary>
        /// Registers a named callback for a live participant in this world.
        /// A registration made during dispatch starts on the next tick.
        /// </summary>
        /// <param name="entity">The owning entity, which must be in this world's subtree.</param>
        /// <param name="entryName">A nonempty name unique to this participant.</param>
        /// <param name="priority">Execution priority; use <see cref="GameplayWorldLogicPriority"/> for shared ordering.</param>
        /// <param name="callback">The callback receiving the fixed delta in seconds.</param>
        /// <exception cref="ArgumentNullException">The entity or callback is null.</exception>
        /// <exception cref="ArgumentException">The entry name is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">The World is faulted, the owner is invalid, or the entry already exists.</exception>
        /// <exception cref="ObjectDisposedException">The world scheduler has been disposed.</exception>
        public void Register(Entity entity, string entryName, int priority, Action<float> callback)
        {
            this.register(entity, entryName, priority, callback, null);
        }

        /// <summary>
        /// Registers a callback receiving its live owner explicitly, allowing shared static callbacks without closures.
        /// Registrations made during dispatch start on the next tick.
        /// </summary>
        /// <param name="entity">The owning entity, which must be in this World's subtree.</param>
        /// <param name="entryName">A nonempty name unique to this participant.</param>
        /// <param name="priority">Execution priority; use <see cref="GameplayWorldLogicPriority"/> for shared ordering.</param>
        /// <param name="callback">Receives the live owner and fixed delta in seconds.</param>
        /// <exception cref="ArgumentNullException">The entity or callback is null.</exception>
        /// <exception cref="ArgumentException">The entry name is empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException">The World is faulted, the owner is invalid, or the entry already exists.</exception>
        /// <exception cref="ObjectDisposedException">The World scheduler has been disposed.</exception>
        public void Register(Entity entity, string entryName, int priority, Action<Entity, float> callback)
        {
            this.register(entity, entryName, priority, null, callback);
        }

        private void register(Entity entity, string entryName, int priority,
            Action<float> callback, Action<Entity, float> entityCallback)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameplayWorldLogicUpdate));
            }

            if (this.world.IsFaulted)
            {
                throw new InvalidOperationException("The GameplayWorld has stopped after a failure.", this.world.Failure);
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrWhiteSpace(entryName))
            {
                throw new ArgumentException("An entry name is required.", nameof(entryName));
            }

            if (callback == null && entityCallback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!this.belongsToWorld(entity))
            {
                throw new InvalidOperationException("The participant must be alive and belong to this world.");
            }

            var key = new LogicUpdateKey(entity, entryName);
            if (this.entriesByKey.ContainsKey(key))
            {
                throw new InvalidOperationException("The entity and entry name are already registered.");
            }

            if (!this.bucketsByPriority.TryGetValue(priority, out LogicUpdateBucket bucket))
            {
                bucket = new LogicUpdateBucket();
                this.bucketsByPriority.Add(priority, bucket);
            }

            LogicUpdateEntry entry = this.availableEntries.Count > 0
                ? this.availableEntries.Pop()
                : new LogicUpdateEntry();
            entry.initialize(entity, key, priority, callback, entityCallback);
            this.entriesByKey.Add(key, entry);
            bucket.Entries.AddLast(entry.Node);
        }

        /// <summary>Removes a participant's named callback; a pending callback is skipped during dispatch.</summary>
        /// <param name="entity">The owning entity.</param>
        /// <param name="entryName">The registered entry name.</param>
        /// <returns>Whether a matching entry was removed.</returns>
        /// <exception cref="ArgumentNullException">The entity is null.</exception>
        /// <exception cref="ArgumentException">The entry name is empty or whitespace.</exception>
        public bool Unregister(Entity entity, string entryName)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrWhiteSpace(entryName))
            {
                throw new ArgumentException("An entry name is required.", nameof(entryName));
            }

            var key = new LogicUpdateKey(entity, entryName);
            if (this.entriesByKey.TryGetValue(key, out LogicUpdateEntry entry))
            {
                this.removeEntry(entry);
                return true;
            }

            return false;
        }

        internal bool hasRegistration(Entity entity, string entryName, Action<Entity, float> callback)
        {
            var key = new LogicUpdateKey(entity, entryName);
            return this.entriesByKey.TryGetValue(key, out LogicUpdateEntry entry) &&
                entry.EntityCallback == callback;
        }

        internal void update()
        {
            if (this.isDisposed || this.world.IsFaulted || !this.hasPendingTick)
            {
                return;
            }

            if (this.isDispatching)
            {
                throw new InvalidOperationException("LogicUpdate cannot dispatch recursively.");
            }

            this.hasPendingTick = false;
            this.isDispatching = true;
            bool completed = false;
            Exception dispatchFailure = null;
            // Reuse the snapshot buffer; registration changes cannot affect the current tick.
            try
            {
                foreach (LogicUpdateBucket bucket in this.bucketsByPriority.Values)
                {
                    this.dispatchSnapshot.AddRange(bucket.Entries);
                }

                foreach (LogicUpdateEntry entry in this.dispatchSnapshot)
                {
                    if (entry.Active)
                    {
                        if (!entry.Entity.Validate() || !this.belongsToWorld((Entity)entry.Entity))
                        {
                            this.removeEntry(entry);
                            continue;
                        }

                        try
                        {
                            if (entry.EntityCallback != null)
                            {
                                entry.EntityCallback((Entity)entry.Entity, STEP_SECONDS);
                            }
                            else
                            {
                                entry.Callback(STEP_SECONDS);
                            }
                        }
                        catch (Exception exception)
                        {
                            throw new GameplayWorldLogicUpdateException(this.world, entry.Key.Name, exception);
                        }
                    }
                }
                completed = true;
            }
            catch (Exception exception)
            {
                dispatchFailure = exception;
            }
            finally
            {
                this.dispatchSnapshot.Clear();
                this.isDispatching = false;
                while (this.retiredEntries.Count > 0)
                {
                    this.recycleEntry(this.retiredEntries.Pop());
                }
            }

            Exception deferredFailure = this.drainDeferredWork(completed);
            if (dispatchFailure != null && deferredFailure != null)
            {
                throw new AggregateException("LogicUpdate and deferred work completion failed.",
                    dispatchFailure, deferredFailure);
            }

            Exception failure = dispatchFailure ?? deferredFailure;
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        internal void dispose()
        {
            this.isDisposed = true;
            this.hasPendingTick = false;
            Exception deferredFailure = this.drainDeferredWork(false);
            foreach (LogicUpdateEntry entry in this.entriesByKey.Values)
            {
                entry.Active = false;
                entry.Node.List.Remove(entry.Node);
                if (this.isDispatching)
                {
                    this.retiredEntries.Push(entry);
                }
                else
                {
                    entry.reset();
                }
            }

            this.entriesByKey.Clear();
            this.bucketsByPriority.Clear();
            this.availableEntries.Clear();
            if (deferredFailure != null)
            {
                ExceptionDispatchInfo.Capture(deferredFailure).Throw();
            }
        }

        private Exception drainDeferredWork(bool execute)
        {
            List<Exception> failures = null;
            while (this.deferredWork.Count > 0)
            {
                IGameplayWorldDeferredWork work = this.deferredWork.Dequeue();
                try
                {
                    if (execute && !this.isDisposed && !this.world.IsFaulted)
                    {
                        foreach (IGameplayWorldDeferredWork pending in this.deferredWork)
                        {
                            if (isAncestor(pending.Owner, work.Owner) || isAncestor(work.Owner, pending.Owner))
                            {
                                work.MarkOverlapping();
                                pending.MarkOverlapping();
                            }
                        }

                        work.Execute();
                    }
                    else
                    {
                        work.Cancel();
                    }
                }
                catch (Exception exception)
                {
                    (failures ??= new List<Exception>()).Add(exception);
                    execute = false;
                }
            }

            if (failures == null)
            {
                return null;
            }

            return failures.Count == 1
                ? failures[0]
                : new AggregateException("Deferred work completion failed.", failures);
        }

        private void removeEntry(LogicUpdateEntry entry)
        {
            entry.Active = false;
            this.entriesByKey.Remove(entry.Key);
            LogicUpdateBucket bucket = this.bucketsByPriority[entry.Priority];
            bucket.Entries.Remove(entry.Node);
            if (bucket.Entries.Count == 0)
            {
                this.bucketsByPriority.Remove(entry.Priority);
            }

            if (this.isDispatching)
            {
                // Snapshot entries must not be reused until every callback in this dispatch has finished.
                this.retiredEntries.Push(entry);
            }
            else
            {
                this.recycleEntry(entry);
            }
        }

        private void recycleEntry(LogicUpdateEntry entry)
        {
            entry.reset();
            if (!this.isDisposed)
            {
                this.availableEntries.Push(entry);
            }
        }

        private void markTick()
        {
            this.hasPendingTick = true;
        }

        private bool belongsToWorld(Entity entity)
        {
            if (entity == null || entity.IsDisposed || this.world.IsDisposed)
            {
                return false;
            }

            for (Entity current = entity; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, this.world))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct LogicUpdateKey : IEquatable<LogicUpdateKey>
        {
            private readonly Entity entity;
            private readonly long instanceId;

            internal LogicUpdateKey(Entity entity, string name)
            {
                this.entity = entity;
                this.instanceId = entity.InstanceId;
                this.Name = name;
            }

            internal string Name { get; }

            /// <inheritdoc />
            public bool Equals(LogicUpdateKey other)
            {
                return ReferenceEquals(this.entity, other.entity) &&
                    this.instanceId == other.instanceId &&
                    StringComparer.Ordinal.Equals(this.Name, other.Name);
            }

            /// <inheritdoc />
            public override bool Equals(object obj) => obj is LogicUpdateKey other && this.Equals(other);

            /// <inheritdoc />
            public override int GetHashCode() =>
                HashCode.Combine(this.instanceId, this.Name);
        }

        private sealed class LogicUpdateBucket
        {
            internal LinkedList<LogicUpdateEntry> Entries { get; } = new LinkedList<LogicUpdateEntry>();
        }

        private sealed class LogicUpdateEntry
        {
            internal LogicUpdateEntry()
            {
                this.Node = new LinkedListNode<LogicUpdateEntry>(this);
            }

            internal void initialize(Entity entity, LogicUpdateKey key, int priority,
                Action<float> callback, Action<Entity, float> entityCallback)
            {
                this.Entity = entity;
                this.Key = key;
                this.Priority = priority;
                this.Callback = callback;
                this.EntityCallback = entityCallback;
                this.Active = true;
            }

            internal void reset()
            {
                this.Entity = default;
                this.Key = default;
                this.Priority = default;
                this.Callback = null;
                this.EntityCallback = null;
                this.Active = false;
            }

            internal EntityRef<Entity> Entity { get; private set; }
            internal LogicUpdateKey Key { get; private set; }
            internal int Priority { get; private set; }
            internal Action<float> Callback { get; private set; }
            internal Action<Entity, float> EntityCallback { get; private set; }
            internal LinkedListNode<LogicUpdateEntry> Node { get; }
            internal bool Active { get; set; }
        }
    }
}