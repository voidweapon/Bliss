using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using DataStructures.SortedCollections;

namespace Bliss
{
    [SingletonManger]
    public class EntitySystemManager : Singleton<EntitySystemManager>, ISingletonInit
    {
        private EntityTypeSystemMgr _entityTypeSystemMgr;

        public void Init()
        {
            _entityTypeSystemMgr = new EntityTypeSystemMgr();

            var systemTypes = TypeManager.Instance.GetTypes(typeof(EntitySystemAttribute));
            foreach (var systemType in systemTypes)
            {
                SystemObject systemObject = (SystemObject)Activator.CreateInstance(systemType);
                if (systemObject is ISystem system)
                {
                    _entityTypeSystemMgr.AddEntitySystem(system);
                }
            }
        }

        /// <summary>
        /// Gets the entity systems for the specified entity type and category.
        /// </summary>
        public SortedList<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
        {
            return _entityTypeSystemMgr.GetEntitySystems(entityType, systemCategoryType);
        }

        /// <summary>
        /// Gets update systems registered for the specified entity type.
        /// </summary>
        public IReadOnlyList<IUpdateGroupSystem> GetEntityUpdateSystems(Type entityType)
        {
            return _entityTypeSystemMgr.GetEntityUpdateSystems(entityType);
        }

        public bool IsEntityUpdateInGroup(Type entityType, ESystemUpdateGroup group)
        {
            return _entityTypeSystemMgr.IsEntityUpdateInGroup(entityType, group);
        }

        public int GetEntityUpdateGroupPriority(Type entityType, ESystemUpdateGroup group)
        {
            return _entityTypeSystemMgr.GetEntityUpdateGroupPriority(entityType, group);
        }

        private static void ValidateUpdateSystem(IUpdateGroupSystem updateSystem)
        {
            Type systemType = updateSystem.GetType();
            Type entityType = updateSystem.GetEntityType();
            Type categoryType = updateSystem.GetSystemCategoryType();
            Type entityConstraintType;
            int minimumGroup;
            int maximumGroup;

            if (categoryType == typeof(IEarlyUpdateSystem))
            {
                entityConstraintType = typeof(IEarlyUpdate);
                minimumGroup = SystemUpdateGroupRange.FIRST_EARLY_UPDATE;
                maximumGroup = (int)ESystemUpdateGroup.EarlyUpdate;
            }
            else if (categoryType == typeof(IUpdateSystem))
            {
                entityConstraintType = typeof(IUpdate);
                minimumGroup = (int)ESystemUpdateGroup.EarlyUpdate + 1;
                maximumGroup = (int)ESystemUpdateGroup.Update;
            }
            else if (categoryType == typeof(ILateUpdateSystem))
            {
                entityConstraintType = typeof(ILateUpdate);
                minimumGroup = (int)ESystemUpdateGroup.Update + 1;
                maximumGroup = (int)ESystemUpdateGroup.LateUpdate;
            }
            else
            {
                throw new ArgumentException(
                    $"Update system '{systemType.FullName}' declares unsupported category '{categoryType?.FullName ?? "<null>"}'.",
                    nameof(updateSystem));
            }

            if (!categoryType.IsInstanceOfType(updateSystem))
            {
                throw new ArgumentException(
                    $"Update system '{systemType.FullName}' does not implement its declared category '{categoryType.FullName}'.",
                    nameof(updateSystem));
            }

            if (entityType == null || !typeof(Entity).IsAssignableFrom(entityType))
            {
                throw new ArgumentException(
                    $"Update system '{systemType.FullName}' declares invalid entity type '{entityType?.FullName ?? "<null>"}'.",
                    nameof(updateSystem));
            }

            if (!entityConstraintType.IsAssignableFrom(entityType))
            {
                throw new ArgumentException(
                    $"Update system '{systemType.FullName}' requires entity type '{entityType.FullName}' to implement '{entityConstraintType.FullName}'.",
                    nameof(updateSystem));
            }

            ESystemUpdateGroup updateGroup = updateSystem.GetUpdateGroup();
            int updateGroupValue = (int)updateGroup;
            if (updateGroupValue < minimumGroup || updateGroupValue > maximumGroup)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(updateGroup),
                    updateGroupValue,
                    $"Update system '{systemType.FullName}' has invalid update group value {updateGroupValue} for category '{categoryType.FullName}'; expected a value from {minimumGroup} through {maximumGroup}.");
            }
        }

        private class EntityTypeSystemMgr
        {
            private readonly Dictionary<Type, EntitySystemCategoryMgr> _entitySystems =
                new Dictionary<Type, EntitySystemCategoryMgr>();

            public SortedList<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
            {
                if (_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    return entitySystem.GetEntitySystems(systemCategoryType);
                }

                return null;
            }

            public IReadOnlyList<IUpdateGroupSystem> GetEntityUpdateSystems(Type entityType)
            {
                if (_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    return entitySystem.UpdateSystems;
                }

                return Array.Empty<IUpdateGroupSystem>();
            }

            public void AddEntitySystem(ISystem systemObject)
            {
                if (systemObject is IUpdateGroupSystem updateSystem)
                {
                    ValidateUpdateSystem(updateSystem);
                }

                Type entityType = systemObject.GetEntityType();
                if (!_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    entitySystem = new EntitySystemCategoryMgr();
                    _entitySystems.Add(entityType, entitySystem);
                }

                entitySystem.AddEntitySystem(systemObject);
            }

            public bool IsEntityUpdateInGroup(Type entityType, ESystemUpdateGroup group)
            {
                if (!_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    return false;
                }

                foreach (IUpdateGroupSystem system in entitySystem.UpdateSystems)
                {
                    if (system.GetUpdateGroup() == group)
                    {
                        return true;
                    }
                }

                return false;
            }

            public int GetEntityUpdateGroupPriority(Type entityType, ESystemUpdateGroup group)
            {
                if (!_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    return 0;
                }

                int priority = default;
                bool found = false;
                foreach (IUpdateGroupSystem system in entitySystem.UpdateSystems)
                {
                    if (system.GetUpdateGroup() == group && (!found || system.GetPriority() < priority))
                    {
                        priority = system.GetPriority();
                        found = true;
                    }
                }

                return priority;
            }
        }

        private class EntitySystemCategoryMgr
        {
            private readonly MultiMapSortedList<Type, ISystem> _entitySystems =
                new MultiMapSortedList<Type, ISystem>();

            private readonly List<IUpdateGroupSystem> _updateSystems = new List<IUpdateGroupSystem>();

            public IReadOnlyList<IUpdateGroupSystem> UpdateSystems => _updateSystems;

            public SortedList<ISystem> GetEntitySystems(Type systemCategoryType)
            {
                return _entitySystems[systemCategoryType];
            }

            public void AddEntitySystem(ISystem systemObject)
            {
                Type categoryType = systemObject.GetSystemCategoryType();
                _entitySystems.Add(categoryType, systemObject);

                if (systemObject is IUpdateGroupSystem updateSystem)
                {
                    _updateSystems.Add(updateSystem);
                }
            }
        }
    }
}
