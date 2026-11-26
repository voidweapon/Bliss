using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// Schedules update systems by phase and priority while preserving their entity registration order.
    /// </summary>
    public sealed class EntityUpdateScheduler
    {
        // Keys are numeric update groups; values collect system groups assigned to each update group.
        private readonly SortedDictionary<int, UpdateGroupCollection> _groupsByUpdateGroup =
            new SortedDictionary<int, UpdateGroupCollection>();

        // Keys are concrete update-system types; values hold their scheduled system groups.
        private readonly Dictionary<Type, SystemGroup> _systemGroupsByType = new Dictionary<Type, SystemGroup>();
        private readonly List<SystemGroup> _frameGroups = new List<SystemGroup>();

        /// <summary>
        /// Registers an entity with each update system that applies to its type.
        /// </summary>
        public void Register(Entity entity, IReadOnlyList<IUpdateGroupSystem> updateSystems)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (updateSystems == null)
            {
                throw new ArgumentNullException(nameof(updateSystems));
            }

            foreach (IUpdateGroupSystem system in updateSystems)
            {
                Type systemType = system.GetType();
                if (!_systemGroupsByType.TryGetValue(systemType, out SystemGroup systemGroup))
                {
                    systemGroup = new SystemGroup(system);
                    _systemGroupsByType.Add(systemType, systemGroup);

                    int updateGroup = (int)system.GetUpdateGroup();
                    if (!_groupsByUpdateGroup.TryGetValue(updateGroup, out UpdateGroupCollection updateGroupCollection))
                    {
                        updateGroupCollection = new UpdateGroupCollection();
                        _groupsByUpdateGroup.Add(updateGroup, updateGroupCollection);
                    }

                    updateGroupCollection.Register(system.GetPriority(), systemGroup);
                }

                systemGroup.Register(entity);
            }
        }

        /// <summary>
        /// Removes an entity from every registered update-system group.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void Unregister(Entity entity, IReadOnlyList<IUpdateGroupSystem> updateSystems)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (updateSystems == null)
            {
                throw new ArgumentNullException(nameof(updateSystems));
            }

            EntityRef<Entity> entityRef = entity;
            foreach (IUpdateGroupSystem system in updateSystems)
            {
                if (_systemGroupsByType.TryGetValue(system.GetType(), out SystemGroup systemGroup))
                {
                    systemGroup.Unregister(entityRef);
                }
            }
        }

        /// <summary>
        /// Captures the entities eligible for every phase of the next frame.
        /// </summary>
        public void BeginFrame()
        {
            _frameGroups.Clear();
            foreach (UpdateGroupCollection updateGroupCollection in _groupsByUpdateGroup.Values)
            {
                foreach (SystemGroup systemGroup in updateGroupCollection.SystemGroups)
                {
                    systemGroup.BeginFrame();
                    _frameGroups.Add(systemGroup);
                }
            }
        }

        /// <summary>
        /// Runs captured systems in the inclusive numeric group range.
        /// </summary>
        public void Update(ESystemUpdateGroup minimumGroup, ESystemUpdateGroup maximumGroup)
        {
            Update(minimumGroup, maximumGroup, null, null);
        }

        internal void Update(ESystemUpdateGroup minimumGroup, ESystemUpdateGroup maximumGroup,
            Root root, RootInitializationGateComponent gate)
        {
            foreach (SystemGroup systemGroup in _frameGroups)
            {
                if (root?.InitializationState == ERootInitializationState.Failed)
                {
                    return;
                }

                if (root != null && (gate == null || gate.IsDisposed))
                {
                    gate = root.GetInitializationGate();
                }

                if (systemGroup.UpdateGroup < (int)minimumGroup || systemGroup.UpdateGroup > (int)maximumGroup)
                {
                    continue;
                }

                if (gate != null && !gate.AllowsSystem(systemGroup.SystemType))
                {
                    continue;
                }

                systemGroup.Update(root, gate);
            }
        }

        private sealed class UpdateGroupCollection
        {
            // Keys are system priorities; values contain systems that share the same priority.
            private readonly SortedDictionary<int, PriorityGroupCollection> _groupsByPriority =
                new SortedDictionary<int, PriorityGroupCollection>();

            public IEnumerable<SystemGroup> SystemGroups
            {
                get
                {
                    foreach (PriorityGroupCollection priorityGroup in _groupsByPriority.Values)
                    {
                        foreach (SystemGroup systemGroup in priorityGroup.SystemGroups)
                        {
                            yield return systemGroup;
                        }
                    }
                }
            }

            public void Register(int priority, SystemGroup systemGroup)
            {
                if (!_groupsByPriority.TryGetValue(priority, out PriorityGroupCollection priorityGroup))
                {
                    priorityGroup = new PriorityGroupCollection();
                    _groupsByPriority.Add(priority, priorityGroup);
                }

                priorityGroup.Add(systemGroup);
            }
        }

        private sealed class PriorityGroupCollection
        {
            private readonly List<SystemGroup> _systemGroups = new List<SystemGroup>();

            public IReadOnlyList<SystemGroup> SystemGroups => _systemGroups;

            public void Add(SystemGroup systemGroup)
            {
                _systemGroups.Add(systemGroup);
            }
        }

        private sealed class SystemGroup
        {
            private readonly IUpdateGroupSystem _system;
            private readonly List<EntityRef<Entity>> _entities = new List<EntityRef<Entity>>();
            private int _activeEntityCount;

            public SystemGroup(IUpdateGroupSystem system)
            {
                _system = system;
                UpdateGroup = (int)system.GetUpdateGroup();
            }

            public int UpdateGroup { get; }
            public Type SystemType => _system.GetType();

            public void Register(Entity entity)
            {
                _entities.Add(entity);
            }

            public void Unregister(EntityRef<Entity> entityRef)
            {
                for (int entityIndex = this._entities.Count - 1; entityIndex >= 0; entityIndex--)
                {
                    if (EntityRef<Entity>.Comparer.Equals(this._entities[entityIndex], entityRef))
                    {
                        this._entities.RemoveAt(entityIndex);
                    }
                }

                this._activeEntityCount = Math.Min(this._activeEntityCount, this._entities.Count);
            }

            public void BeginFrame()
            {
                int validEntityCount = 0;
                for (int i = 0; i < _entities.Count; i++)
                {
                    EntityRef<Entity> entityRef = _entities[i];
                    if (!entityRef.Validate())
                    {
                        continue;
                    }

                    _entities[validEntityCount] = entityRef;
                    validEntityCount++;
                }

                if (validEntityCount < _entities.Count)
                {
                    _entities.RemoveRange(validEntityCount, _entities.Count - validEntityCount);
                }

                _activeEntityCount = _entities.Count;
            }

            public void Update(Root root, RootInitializationGateComponent gate)
            {
                for (int i = 0; i < _activeEntityCount; i++)
                {
                    Entity entity = (Entity)_entities[i];
                    if (entity == null || entity.IsDisposed)
                    {
                        continue;
                    }

                    Root entityRoot = root ?? entity.LogicScene?.Root;
                    if (entityRoot?.InitializationState == ERootInitializationState.Failed)
                    {
                        if (root != null)
                        {
                            return;
                        }

                        continue;
                    }

                    if (entityRoot != null)
                    {
                        gate = entityRoot.GetInitializationGate();
                        if (gate != null && !gate.AllowsSystem(SystemType))
                        {
                            if (root != null)
                            {
                                return;
                            }

                            continue;
                        }
                    }

                    try
                    {
                        _system.Run(entity);
                    }
                    catch (Exception exception)
                    {
                        throw new Exception(
                            $"entity system {_system.GetUpdateGroup()} fail: {entity.GetType().FullName}",
                            exception);
                    }
                }
            }
        }
    }
}
