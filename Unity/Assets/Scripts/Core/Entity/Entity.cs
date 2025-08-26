using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Pool;

namespace Bliss
{

    public enum EEntityStatus
    {
        
    }
    
    public class Entity : DisposableObject, IPoolableObject
    {
        public bool IsFromPool { get; set; }
        public bool IsInPool { get; set; }
        
        public bool IsComponent { get; private set; }

        public bool IsDisposed => InstanceId == 0;

        public long Id { get; set; }
        public long InstanceId { get; protected set; }
        
            
        protected Entity()
        {
           
        }

        protected static Entity Create(Type type, bool isFromPool)
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
            
            return entity;
        }

        #region Scene

        /// <summary>
        /// 挂载在哪个场景上
        /// </summary>
        protected ILogicScene LogicScene;


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
            return entity;
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

        public T AddComponent<T>(bool isFromPool = false) where T : Entity
        {
            Entity component = Create(typeof(T), isFromPool);
            component.Id = Id;
            component.SetComponentParent(this);
            
            return component as T;
        }

        private Entity _componentParent;
        #endregion
        
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
    }
}