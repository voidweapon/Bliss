using System;
using DataStructures.SortedCollections;

namespace Bliss
{
    public class EntitySystem
    {
        private readonly EntityUpdateScheduler _updateScheduler = new EntityUpdateScheduler();
        private readonly Root _root;

        /// <summary>Creates an EntitySystem without a Root-specific initialization gate.</summary>
        public EntitySystem()
        {
        }

        internal EntitySystem(Root root)
        {
            _root = root;
        }

        public virtual void RegisterSystem(Entity entity)
        {
            _updateScheduler.Register(
                entity,
                EntitySystemManager.Instance.GetEntityUpdateSystems(entity.GetType()));
        }

        /// <summary>
        /// Removes all update-system registrations for an entity.
        /// </summary>
        /// <param name="entity">The entity to unregister.</param>
        public virtual void UnregisterSystem(Entity entity)
        {
            _updateScheduler.Unregister(
                entity,
                EntitySystemManager.Instance.GetEntityUpdateSystems(entity.GetType()));
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
            if (_root?.InitializationState == ERootInitializationState.Failed)
            {
                return;
            }

            RootInitializationGateComponent gate = _root?.GetInitializationGate();
            _updateScheduler.BeginFrame();
            _updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_EARLY_UPDATE,
                ESystemUpdateGroup.EarlyUpdate,
                _root, gate);
        }

        public void Update()
        {
            if (_root?.InitializationState == ERootInitializationState.Failed)
            {
                return;
            }

            _updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_UPDATE,
                ESystemUpdateGroup.Update,
                _root, _root?.GetInitializationGate());
        }

        public void LateUpdate()
        {
            if (_root?.InitializationState == ERootInitializationState.Failed)
            {
                return;
            }

            _updateScheduler.Update(
                (ESystemUpdateGroup)SystemUpdateGroupRange.FIRST_LATE_UPDATE,
                ESystemUpdateGroup.LateUpdate,
                _root, _root?.GetInitializationGate());
        }
        #endregion
    }
}