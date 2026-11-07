using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// Schedules update systems by phase and priority while preserving their entity registration order.
    /// </summary>
    public sealed class EntityUpdateScheduler
    {
        private readonly SortedDictionary<int, SortedDictionary<int, List<SystemGroup>>> _groups =
            new SortedDictionary<int, SortedDictionary<int, List<SystemGroup>>>();

        private readonly Dictionary<Type, SystemGroup> _systemGroups = new Dictionary<Type, SystemGroup>();
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
                if (!_systemGroups.TryGetValue(systemType, out SystemGroup systemGroup))
                {
                    systemGroup = new SystemGroup(system);
                    _systemGroups.Add(systemType, systemGroup);

                    int updateGroup = (int)system.GetUpdateGroup();
                    if (!_groups.TryGetValue(updateGroup, out SortedDictionary<int, List<SystemGroup>> priorityGroups))
                    {
                        priorityGroups = new SortedDictionary<int, List<SystemGroup>>();
                        _groups.Add(updateGroup, priorityGroups);
                    }

                    int priority = system.GetPriority();
                    if (!priorityGroups.TryGetValue(priority, out List<SystemGroup> systemGroups))
                    {
                        systemGroups = new List<SystemGroup>();
                        priorityGroups.Add(priority, systemGroups);
                    }

                    systemGroups.Add(systemGroup);
                }

                systemGroup.Register(entity);
            }
        }

        /// <summary>
        /// Captures the entities eligible for every phase of the next frame.
        /// </summary>
        public void BeginFrame()
        {
            _frameGroups.Clear();
            foreach (SortedDictionary<int, List<SystemGroup>> priorityGroups in _groups.Values)
            {
                foreach (List<SystemGroup> systemGroups in priorityGroups.Values)
                {
                    foreach (SystemGroup systemGroup in systemGroups)
                    {
                        systemGroup.BeginFrame();
                        _frameGroups.Add(systemGroup);
                    }
                }
            }
        }

        /// <summary>
        /// Runs captured systems in the inclusive numeric group range.
        /// </summary>
        public void Update(ESystemUpdateGroup minimumGroup, ESystemUpdateGroup maximumGroup)
        {
            foreach (SystemGroup systemGroup in _frameGroups)
            {
                if (systemGroup.UpdateGroup < (int)minimumGroup || systemGroup.UpdateGroup > (int)maximumGroup)
                {
                    continue;
                }

                systemGroup.Update();
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

            public void Register(Entity entity)
            {
                _entities.Add(entity);
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

            public void Update()
            {
                for (int i = 0; i < _activeEntityCount; i++)
                {
                    Entity entity = (Entity)_entities[i];
                    if (entity == null || entity.IsDisposed)
                    {
                        continue;
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
