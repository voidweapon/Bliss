using System;
using System.Collections.Generic;

namespace Bliss
{
    public class EntitySystem 
    {
        private Queue<EntityWeakRef<Entity>>[] _updateQueue = new Queue<EntityWeakRef<Entity>>[(int)ESystemUpdateGroup.Max];

        public EntitySystem()
        {
            for (int i = 0; i < (int)ESystemUpdateGroup.Max; i++)
            {
                _updateQueue[i] = new Queue<EntityWeakRef<Entity>>();
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

            List<ISystem> awakeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IAwakeSystem));

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
            
            List<ISystem> awakeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IAwakeSystem<TParam>));

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
            if (entity is not IDestroy)
            {
                return;
            }
            List<ISystem> destroySystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IDestroySystem));
            if (destroySystems == null)
            {
                return;
            }

            foreach (var system in destroySystems)
            {
                var destroySystem = (IDestroySystem)system;
                if (destroySystem == null)
                {
                    continue;
                }
                
                destroySystem.Run(entity);
            }
            
        }

        #endregion

        #region Serialize Call
        public void OnBeforeSerialize(Entity entity)
        {
            if (entity is not ISerialize)
            {
                return;
            }
            
            List<ISystem> serializeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(ISerializeSystem));
            if (serializeSystems == null)
            {
                return;
            }
            
            foreach (ISerializeSystem serializeSystem in serializeSystems)
            {
                if (serializeSystem == null)
                {
                    continue;
                }

                try
                {
                    serializeSystem.Run(entity);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public void OnAfterDeserialize(Entity entity)
        {
            if (entity is not IDeserialize)
            {
                return;
            }
            
            List<ISystem> deserializeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IDeserializeSystem));
            if (deserializeSystems == null)
            {
                return;
            }
            
            foreach (IDeserializeSystem deserializeSystem in deserializeSystems)
            {
                if (deserializeSystem == null)
                {
                    continue;
                }

                try
                {
                    deserializeSystem.Run(entity);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        #endregion

        public void OnMountToScene(Entity entity)
        {
            if (entity is not IMountToScene)
            {
                return;
            }
            
            List<ISystem> serializeSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IMountToSceneSystem));
            if (serializeSystems == null)
            {
                return;
            }
            
            foreach (IMountToSceneSystem mountToSceneSystem in serializeSystems)
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
            Queue<EntityWeakRef<Entity>> updateQueue = _updateQueue[(int)updateGroup];
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