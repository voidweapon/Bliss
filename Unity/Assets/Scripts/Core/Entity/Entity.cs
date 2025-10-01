using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Id;
using Bliss.Pool;

namespace Bliss
{

    public enum EEntityStatus
    {
        //是否是池化对象
    }
    
    public class Entity : DisposableObject, IPoolableObject
    {
        public bool IsFromPool { get; set; }
        public bool IsInPool { get; set; }
        public bool IsComponent { get; private set; }

        /// <summary>
        /// 对象是否是序列化出来的
        /// </summary>
        public bool IsSerialized { get; private set; }

        public bool IsDisposed => InstanceId == 0;

        public long Id { get; set; }
        public long InstanceId { get; protected set; }
        
            
        protected  Entity()
        {
           
        }

        private static Entity Create(Type type, bool isFromPool)
        {
            Entity entity;
            if (isFromPool)
            {
                entity = (Entity)ObjectPool.Instance.Fetch(type);
            }
            else
            {
                entity = (Entity) Activator.CreateInstance(type);
            }

            entity.Id = 0;
            entity.InstanceId = 0;
            entity.IsSerialized = false;
            
            return entity;
        }

        #region Scene

        /// <summary>
        /// 挂载在哪个场景上
        /// </summary>
        protected ILogicScene _logicScene;

        public ILogicScene LogicScene
        {
            get
            {
                return _logicScene;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"logicScene cant set null: {GetType().FullName}");
                }
                
                if (_logicScene == value)
                {
                    return;
                }
                
                var prevLogicScene = _logicScene;
                _logicScene = value;
                
                if (prevLogicScene == null)
                {
                    if (InstanceId == 0)
                    {
                        InstanceId = IdGenerator.Instance.GenerateInstanceId();
                    }

                    RegisterToSystem();
                }

                if (_children != null)
                {
                    foreach (var child in _children.Values)
                    {
                        child.LogicScene = _logicScene;
                    }
                }

                if (_components != null)
                {
                    foreach (var component in _components.Values)
                    {
                        component.LogicScene = _logicScene;
                    }
                }
                
            }
        }


        #endregion

        #region Entity Tree
        
        protected SortedDictionary<long, Entity> _children;
        /// <summary>
        /// 孩子节点
        /// </summary>
        public SortedDictionary<long, Entity> Children
        {
            get
            {
                if (_children == null)
                    _children = ObjectPool.Instance.Fetch<SortedDictionary<long, Entity>>();
                return _children;
            }
        }

        /// <summary>
        /// 孩子节点数量
        /// </summary>
        public int ChildCount
        {
            get
            {
                if(_children == null)
                    return 0;
                return _children.Count;
            }
        }

        private void AddToChildren(Entity entity)
        {
            Children.Add(entity.Id, entity);
        }

        private void RemoveFromChildren(Entity entity)
        {
            if (_children == null)
            {
                return;
            }
            
            _children.Remove(entity.Id);
            if (_children.Count == 0)
            {
                ObjectPool.Instance.Recycle(_children);
                _children = null;
            }
        }

        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = Bliss.Id.IdGenerator.Instance.GenerateId();
            entity.Parent = this;
            GetEntitySystem().Awake(entity);
            return entity;
        }
        
        public T AddChild<T, TArg>(TArg arg, bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = Bliss.Id.IdGenerator.Instance.GenerateId();
            entity.Parent = this;
            GetEntitySystem().Awake(entity, arg);
            return entity;
        }
        
        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = id;
            entity.Parent = this;
            GetEntitySystem().Awake(entity);
            return entity;
        }
        
        public T AddChildWithId<T, TArg>(long id, TArg arg, bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = id;
            entity.Parent = this;
            GetEntitySystem().Awake(entity);
            return entity;
        }

        public T GetChild<T>(long id) where T : Entity
        {
            if (_children == null)
            {
                return null;
            }
            _children.TryGetValue(id, out Entity child);
            return child as T;
        }

        public void RemoveChild(long id)
        {
            if (_children == null)
            {
                return;
            }


            if (!_children.TryGetValue(id, out Entity child))
            {
                return;
            }

            _children.Remove(id);
            child.Dispose();
        }
        
        /// <summary>
        /// 父亲节点
        /// </summary>
        protected Entity _parent;

        public Entity Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                CheckParentValue(value);

                if (_parent != null)
                {
                    // parent相同，不设置
                    if (this._parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this._parent.GetType().FullName}");
                        return;
                    }
                    
                    this._parent.RemoveFromChildren(this);
                }
                
                this._parent = value;
                this.IsComponent = false;
                this._parent.AddToChildren(this);
                
                if (this is ILogicScene logicScene)
                {
                    LogicScene = logicScene;
                }
                else
                {
                    LogicScene = _parent.LogicScene;
                }
            }
        }
        
        

        #endregion

        #region Entity Component
        protected SortedDictionary<long, Entity> _components;
        /// <summary>
        /// Entity的组件
        /// </summary>
        public SortedDictionary<long, Entity> Components
        {
            get
            {
                if (_components == null)
                    _components = ObjectPool.Instance.Fetch<SortedDictionary<long, Entity>>();
                return _components;
            }
        }

        /// <summary>
        /// 组件数量
        /// </summary>
        public int ComponentCount
        {
            get
            {
                if (_components == null)
                    return 0;
                return _components.Count;
            }
        }
        

        private void AddToComponents(Entity component)
        {
            Components.Add(GetLongHashCode(component.GetType()), component);
        }

        private void RemoveFromComponents(Entity component)
        {
            if (_components == null)
            {
                return;
            }
            
            _components.Remove(GetLongHashCode(component.GetType()));
            if (_components.Count == 0)
            {
                ObjectPool.Instance.Recycle(_components);
                _components = null;
            }
        }

        private void SetComponentParent(Entity value)
        {
            CheckParentValue(value);
            
            if (_parent != null)
            {
                // parent相同，不设置
                if (_parent == value)
                {
                    Log.Error($"重复设置了Parent: {GetType().FullName} parent: {_parent.GetType().FullName}");
                    return;
                }
                _parent.RemoveFromComponents(this);
            }
            
            _parent = value;
            IsComponent = true;
            _parent.AddToComponents(this);

            if (this is ILogicScene logicScene)
            {
                LogicScene = logicScene;
            }
            else
            {
                LogicScene = _parent.LogicScene;
            }
        }
        
        public Entity AddComponent(Entity component)
        {
            component.SetComponentParent(this);
            return component;
        }

        public TComponent AddComponent<TComponent>(bool isFromPool = false) where TComponent : Entity
        {
            Entity component = Create(typeof(TComponent), isFromPool);
            component.Id = Id;
            component.SetComponentParent(this);
            GetEntitySystem().Awake(component);
            return (TComponent)component;
        }
        
        public TComponent AddComponent<TComponent, TParam>(TParam param, bool isFromPool = false) where TComponent : Entity
        {
            Entity component = Create(typeof(TComponent), isFromPool);
            component.Id = Id;
            component.SetComponentParent(this);
            GetEntitySystem().Awake(component, param);
            return (TComponent)component;
        }

        public TComponent AddComponentWithId<TComponent>(int id, bool isFromPool = false) where TComponent : Entity
        {
            Entity component = Create(typeof(TComponent), isFromPool);
            component.Id = id;
            component.SetComponentParent(this);
            GetEntitySystem().Awake(component);
            return (TComponent)component;
        }
        
        public TComponent GetComponent<TComponent>() where TComponent : Entity
        {
            return (TComponent)GetComponent(typeof(TComponent));
        }

        public Entity GetComponent(Type componentType)
        {
            if (_components == null)
            {
                return null;
            } 
            
            if (!_components.TryGetValue(GetLongHashCode(componentType), out Entity component))
            {
                return null;
            }
            
            return component;
        }

        public void RemoveComponent<TComponent>() where TComponent : Entity
        {
            if (IsDisposed)
            {
                return;
            }

            if (_components == null)
            {
                return;
            }

            Type type = typeof (TComponent);
            
            if (!_components.TryGetValue(this.GetLongHashCode(type), out var component))
            {
                return;
            }

            RemoveFromComponents(component);
            component.Dispose();
        }
        
        #endregion

        public override void BeforeSerialize()
        {
            base.BeforeSerialize();
            GetEntitySystem().Serialize(this);

            if (_components != null && _components.Count > 0)
            {
                foreach (Entity component in _components.Values)
                {
                    if (component is not ISerializableEntity)
                    {
                        continue;
                    }
                    
                    component.BeforeSerialize();
                }
            }
            
            if (_children != null && _children.Count > 0)
            {
                foreach (Entity child in _children.Values)
                {
                    if (child is not ISerializableEntity)
                    {
                        continue;
                    }
                    
                    child.BeforeSerialize();
                }
            }
        }

        // public override void AfterDeserialize()
        // {
        //     base.AfterDeserialize();
        //     
        //     if (_components != null && _components.Count > 0)
        //     {
        //         foreach (Entity component in _components.Values)
        //         {
        //             component.AfterDeserialize();
        //         }
        //     }
        //     
        //     if (_children != null && _children.Count > 0)
        //     {
        //         foreach (Entity child in _children.Values)
        //         {
        //             child.AfterDeserialize();
        //         }
        //     }
        //     
        //     GetEntitySystem().Deserialize(this);
        // }
        
        protected virtual void RegisterToSystem()
        {
            this.LogicScene.Root.EntitySystem.RegisterSystem(this);
        }

        public override void Dispose()
        {
            if(IsDisposed)
            {
              return;  
            }

            InstanceId = 0;

            if (_children != null)
            {
                foreach (var child in _children.Values)
                {
                    child.Dispose();
                }
                _children.Clear();
                ObjectPool.Instance.Recycle(_children);
                _children = null;
            }

            if (_components != null)
            {
                foreach (var component in _components.Values)
                {
                    component.Dispose();
                }
                _components.Clear();
                ObjectPool.Instance.Recycle(_components);
                _components = null;
            }

            // 触发Destroy事件
            GetEntitySystem().Destroy(this);
            
            _logicScene = null;

            if (_parent != null && !_parent.IsDisposed)
            {
                if (IsComponent)
                {
                    _parent.RemoveFromComponents(this);
                }
                else
                {
                    _parent.RemoveFromChildren(this);
                }
            }

            _parent = null;
            base.Dispose();

            //重置运行时状态
            IsSerialized = false;
            
            ObjectPool.Instance.Recycle(this);
        }

        protected virtual long GetLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }

        private void CheckParentValue(Entity value)
        {
            if (value == null)
            {
                throw new Exception($"cant set parent null: {GetType().FullName}");
            }

            if (value == this)
            {
                throw new Exception($"cant set parent self: {GetType().FullName}");
            }

            // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
            if (value.LogicScene == null)
            {
                throw new Exception($"cant set parent because parent domain is null: {GetType().FullName} {value.GetType().FullName}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EntitySystem GetEntitySystem()
        {
            return LogicScene.Root.EntitySystem;
        }
    }
}