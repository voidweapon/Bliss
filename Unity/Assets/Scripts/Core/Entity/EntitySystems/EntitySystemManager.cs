using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;

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
        public List<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
        {
            return _entityTypeSystemMgr.GetEntitySystems(entityType, systemCategoryType);
        }

        public bool IsEntityUpdateInGroup(Type entityType, ESystemUpdateGroup group)
        {
            return _entityTypeSystemMgr.IsEntityUpdateInGroup(entityType, group);
        }
        
        private class EntityTypeSystemMgr
        {
            /// <summary>
            /// Entity Type -> EntityTypeSystem
            /// </summary>
            private Dictionary<Type, EntitySystemCategoryMgr> _entitySystems = new Dictionary<Type, EntitySystemCategoryMgr>();
            
            public List<ISystem> GetEntitySystems(Type entityType, Type systemCategoryType)
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
        }

        private class EntitySystemCategoryMgr
        {
            /// <summary>
            /// EntitySystem CategoryType -> EntitySystem Object
            /// </summary>
            private MultiMapList<Type, ISystem> _entitySystems = new MultiMapList<Type, ISystem>();

            private bool[] UpdateInGroup = new bool[(int)ESystemUpdateGroup.Max];
            
            public List<ISystem> GetEntitySystems(Type systemCategoryType)
            {
                return _entitySystems[systemCategoryType];
            }

            public void AddEntitySystem(ISystem systemObject)
            {
                var categoryType = systemObject.GetSystemCategoryType();
                _entitySystems.Add(categoryType, systemObject);
            }

            public bool IsEntityUpdateInGroup(ESystemUpdateGroup group)
            {
                if(group == ESystemUpdateGroup.None) return false;
                
                return UpdateInGroup[(int)group];
            }
        }
    }



}