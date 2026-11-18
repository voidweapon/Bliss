using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>Owns gameplay updates, timers and fixed-step simulation within a Root.</summary>
    public class GameplayWorld : LogicScene
    {
        public GameplayWorldUpdater Updater { get; } = new GameplayWorldUpdater();

        /// <summary>
        /// Gets the timer service owned by this world.
        /// </summary>
        public GameplayWorldTimer Timer { get; }

        /// <summary>Gets the fixed-step simulation scheduler owned by this world.</summary>
        public GameplayWorldLogicUpdate LogicUpdate { get; }

        internal GameplayWorld(Root root, long id, long instanceId, string name)
            : base(root, id, instanceId, ELogicSceneType.GameplayWorld, name)
        {
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

        public void EarlyUpdate()
        {
            Updater.EarlyUpdate();
        }

        /// <summary>Runs ordinary gameplay updates; fixed ticks are driven independently by Root.</summary>
        public void Update()
        {
            Updater.Update();
        }

        public void LateUpdate()
        {
            Updater.LateUpdate();
        }

        /// <summary>Disposes the world and clears its timers and pending simulation ticks.</summary>
        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.Timer.dispose();
            this.LogicUpdate.dispose();
            base.Dispose();
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
        private bool hasPendingTick;
        private bool isDisposed;
        private bool isDispatching;

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
        /// <exception cref="InvalidOperationException">The owner is invalid or the entity and name are already registered.</exception>
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
        /// <exception cref="InvalidOperationException">The owner is invalid or the entity and name are already registered.</exception>
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

        internal void update()
        {
            if (this.isDisposed || !this.hasPendingTick)
            {
                return;
            }

            if (this.isDispatching)
            {
                throw new InvalidOperationException("LogicUpdate cannot dispatch recursively.");
            }

            this.hasPendingTick = false;
            this.isDispatching = true;
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
        }

        internal void dispose()
        {
            this.isDisposed = true;
            this.hasPendingTick = false;
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