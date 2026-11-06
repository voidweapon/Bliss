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
        private EntityTypeSystemMgr  _entityTypeSystemMgr;
        
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
        ///  获取<paramref name="entityType"/>指定类型的Entity的特定分类(<paramref name="systemCategoryType"/>)下的EntitySystems
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="systemCategoryType"></param>
        /// <returns></returns>
        public SortedList<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
        {
            return _entityTypeSystemMgr.GetEntitySystems(entityType, systemCategoryType);
        }

        public bool IsEntityUpdateInGroup(Type entityType, ESystemUpdateGroup group)
        {
            return _entityTypeSystemMgr.IsEntityUpdateInGroup(entityType, group);
        }
        
        public int GetEntityUpdateGroupPriority(Type entityType, ESystemUpdateGroup group)
        {
            return _entityTypeSystemMgr.GetEntityUpdateGroupPriority(entityType, group);
        }
        
        private class EntityTypeSystemMgr
        {
            /// <summary>
            /// Entity Type -> EntityTypeSystem
            /// </summary>
            private Dictionary<Type, EntitySystemCategoryMgr> _entitySystems = new Dictionary<Type, EntitySystemCategoryMgr>();
            
            public SortedList<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
            {
                if (_entitySystems.TryGetValue(entityType, out EntitySystemCategoryMgr entitySystem))
                {
                    return entitySystem.GetEntitySystems(systemCategoryType);
                }

                return null;
            }

            public void AddEntitySystem(ISystem systemObject)
            {
                var entityType = systemObject.GetEntityType();
                if (!_entitySystems.TryGetValue(entityType, out var entitySystem))
                {
                    entitySystem = new EntitySystemCategoryMgr();
                    _entitySystems.Add(entityType, entitySystem);
                }
                entitySystem.AddEntitySystem(systemObject);
            }
            
            public bool IsEntityUpdateInGroup(Type entityType, ESystemUpdateGroup  group)
            {
                if (!_entitySystems.TryGetValue(entityType, out var entitySystem))
                {
                    return false;
                }

                return entitySystem.IsEntityUpdateInGroup(group);
            }

            public int GetEntityUpdateGroupPriority(Type entityType, ESystemUpdateGroup group)
            {
                if (!_entitySystems.TryGetValue(entityType, out var entitySystem))
                {
                    return 0;
                }
                return entitySystem.GetEntityUpdateGroupPriority(group);
            }
        }

        private class EntitySystemCategoryMgr
        {
            /// <summary>
            /// EntitySystem CategoryType -> EntitySystem Object
            /// </summary>
            //private readonly MultiMapList<Type, ISystem> _entitySystems = new MultiMapList<Type, ISystem>();
            private readonly MultiMapSortedList<Type, ISystem> _entitySystems = new MultiMapSortedList<Type, ISystem>();

            private readonly bool[] _updateInGroup = new bool[(int)ESystemUpdateGroup.Max];
            
            private readonly int[] _updateInGroupPriority = new int[(int)ESystemUpdateGroup.Max];
            
            public SortedList<ISystem> GetEntitySystems(Type systemCategoryType)
            {
                return _entitySystems[systemCategoryType];
            }

            public void AddEntitySystem(ISystem systemObject)
            {
                var categoryType = systemObject.GetSystemCategoryType();
                _entitySystems.Add(categoryType, systemObject);
                
                ESystemUpdateGroup updateGroup = systemObject.GetUpdateGroup();
                if (ESystemUpdateGroup.None < updateGroup && updateGroup < ESystemUpdateGroup.Max)
                {
                    _updateInGroup[(int)updateGroup] = true;
                    _updateInGroupPriority[(int)updateGroup] = systemObject.GetPriority();
                }
            }

            public bool IsEntityUpdateInGroup(ESystemUpdateGroup group)
            {
                if(group == ESystemUpdateGroup.None) return false;
                
                return _updateInGroup[(int)group];
            }
            
            public int GetEntityUpdateGroupPriority(ESystemUpdateGroup group)
            {
                if(group == ESystemUpdateGroup.None) return 0;
                
                return _updateInGroupPriority[(int)group];
            }
        }
    }



}