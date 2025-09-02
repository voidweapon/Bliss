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
        public void Serialize(Entity entity)
        {
           
        }

        public void Deserialize(Entity entity)
        {
            
        }
        #endregion

        #region Update Call

        public void Update()
        {
            Queue<EntityWeakRef<Entity>> updateQueue = _updateQueue[(int)ESystemUpdateGroup.Update];
            int thisFrameCount = updateQueue.Count;
            while (thisFrameCount > 0)
            {
                var entity = (Entity)updateQueue.Dequeue();
                thisFrameCount--;
                
                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                if (entity is not IUpdate)
                {
                    continue;
                }

                try
                {
                    var updateSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(IUpdateSystem));
                    if (updateSystems == null)
                    {
                        continue;
                    }
                    updateQueue.Enqueue(entity);

                    foreach (var system in updateSystems)
                    {
                        var updateSystem = (IUpdateSystem)system;
                        if (updateSystem == null)
                        {
                            continue;
                        }
                        updateSystem.Run(entity);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"entity system update fail: {entity.GetType().FullName}", e);
                }
            }
        }

        public void LateUpdate()
        {
            Queue<EntityWeakRef<Entity>> lateUpdateQueue = _updateQueue[(int)ESystemUpdateGroup.LateUpdate];
            int thisFrameCount = lateUpdateQueue.Count;
            while (thisFrameCount > 0)
            {
                var entity = (Entity)lateUpdateQueue.Dequeue();
                thisFrameCount--;
                
                if (entity == null || entity.IsDisposed)
                {
                    continue;
                }

                if (entity is not ILateUpdate)
                {
                    continue;
                }

                try
                {
                    var lateUpdateSystems = EntitySystemManager.Instance.GetEntitySystems(entity.GetType(), typeof(ILateUpdateSystem));
                    if (lateUpdateSystems == null)
                    {
                        continue;
                    }
                    lateUpdateQueue.Enqueue(entity);

                    foreach (var system in lateUpdateSystems)
                    {
                        var updateSystem = (ILateUpdateSystem)system;
                        if (updateSystem == null)
                        {
                            continue;
                        }
                        updateSystem.Run(entity);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"entity system lateUpdate fail: {entity.GetType().FullName}", e);
                }
            }
        }

        #endregion
    }
}