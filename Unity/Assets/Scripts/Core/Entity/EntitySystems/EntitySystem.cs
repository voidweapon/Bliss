using System;
using System.Collections.Generic;
using DataStructures.SortedCollections;

namespace Bliss
{
    public class EntitySystem 
    {
        private Queue<EntityRef<Entity>>[] _updateQueue = new Queue<EntityRef<Entity>>[(int)ESystemUpdateGroup.Max];

        public EntitySystem()
        {
            for (int i = 0; i < (int)ESystemUpdateGroup.Max; i++)
            {
                _updateQueue[i] = new Queue<EntityRef<Entity>>();
            }
        }
        
        public virtual void RegisterSystem(Entity entity)
        {
            var entityType = entity.GetType();
            for (int i = 0; i < (int)ESystemUpdateGroup.Max; i++)
            {
                if (EntitySystemManager.Instance.IsEntityUpdateInGroup(entityType, (ESystemUpdateGroup)i))
                {
                    _updateQueue[i].Enqueue(entity);
                }
            }
        }
        
        #region Awake Call
        public void Awake(Entity entity)
        {
            if (entity is not IAwake)
            {
                return;
            }

            SortedList<ISystem> awakeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IAwakeSystem));

            if (awakeSystems == null)
            {
                return;
            }

            foreach (var system in awakeSystems)
            {
                var awakeSystem = (IAwakeSystem)system;
                if (awakeSystem == null)
                {
                    continue;
                }
                
                awakeSystem.Run(entity);
            }
        }
        
        public void Awake<TParam>(Entity entity, TParam param)
        {
            if (entity is not IAwake<TParam>)
            {
                return;
            }
            
            SortedList<ISystem> awakeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IAwakeSystem<TParam>));

            if (awakeSystems == null)
            {
                return;
            }

            foreach (var system in awakeSystems)
            {
                var awakeSystem = (IAwakeSystem<TParam>)system;
                if (awakeSystem == null)
                {
                    continue;
                }
                
                awakeSystem.Run(entity,  param);
            }
        }
        #endregion

        #region Destroy Call

        public void Destroy(Entity entity)
        {
            CallEventSystem<IDestroy, IDestroySystem>(entity);
        }

        #endregion

        #region Serialize Call
        public void OnBeforeSerialize(Entity entity)
        {
            CallEventSystem<ISerialize, ISerializeSystem>(entity);
        }

        public void OnAfterDeserialize(Entity entity)
        {
            CallEventSystem<IDeserialize, IDeserializeSystem>(entity);
        }
        #endregion

        public void OnMountToScene(Entity entity)
        {
            CallEventSystem<IMountToScene, IMountToSceneSystem>(entity);
        }
        
        public void CallEventSystem<TEntityConstraint, TSystem>(Entity entity)
            where TEntityConstraint : IEntitySystemConstraint
            where TSystem : IEventSystem
        {
            if (entity is not TEntityConstraint)
            {
                return;
            }
            
            SortedList<ISystem> serializeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(TSystem));
            if (serializeSystems == null)
            {
                return;
            }
            
            foreach (TSystem mountToSceneSystem in serializeSystems)
            {
                if (mountToSceneSystem == null)
                {
                    continue;
                }

                try
                {
                    mountToSceneSystem.Run(entity);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        #region Update Call

        public void EarlyUpdate()
        {
            UpdateSystemUpdateGroup<IEarlyUpdate, IEarlyUpdateSystem>(ESystemUpdateGroup.EarlyUpdate);
        }

        public void Update()
        {
            UpdateSystemUpdateGroup<IUpdate, IUpdateSystem>(ESystemUpdateGroup.Update);
        }

        public void LateUpdate()
        {
            UpdateSystemUpdateGroup<ILateUpdate, ILateUpdateSystem>(ESystemUpdateGroup.LateUpdate);
        }

        private void UpdateSystemUpdateGroup<TEntity, TSystem>(ESystemUpdateGroup updateGroup) 
            where TEntity : IEntitySystemConstraint 
            where TSystem : IUpdateGroupSystem
        {
            var updateQueue = _updateQueue[(int)updateGroup];
            int thisFrameCount = updateQueue.Count;
            while (thisFrameCount > 0)
            {
                var entity = (Entity)updateQueue.Dequeue();
                thisFrameCount--;
                
                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                if (entity is not TEntity)
                {
                    continue;
                }

                try
                {
                    var lateUpdateSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(TSystem));
                    if (lateUpdateSystems == null)
                    {
                        continue;
                    }
                    updateQueue.Enqueue(entity);

                    foreach (var system in lateUpdateSystems)
                    {
                        var updateSystem = (TSystem)system;
                        if (updateSystem == null)
                        {
                            continue;
                        }
                        updateSystem.Run(entity);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"entity system ${updateGroup} fail: {entity.GetType().FullName}", e);
                }
            }
        }
        #endregion
    }
}