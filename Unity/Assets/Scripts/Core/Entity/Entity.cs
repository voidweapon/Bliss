using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Base;
using Bliss.Id;
using Bliss.Pool;
using MemoryPack;

namespace Bliss
{
    using SerializeIgnore = MemoryPackIgnoreAttribute;
    using SerializeInclude = MemoryPackIncludeAttribute;
    
    /*
     * 序列化消息顺序: BeforeSerialize -> AfterSerialize -- Invoke--> SerializeSystem
     * 反序列化消息函数顺: BeginInit (possible not call) -> EndInit -> （first mount to scene） Deserialize System -- Then --> MountToSceneSystem
     */
    [MemoryPackable(GenerateType.NoGenerate)]
    public partial class Entity : DisposableObject, IPoolableObject
    {
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        public bool IsFromPool { get; set; }
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        public bool IsInPool { get; set; }
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        protected bool IsComponent { get; private set; }

        /// <summary>
        /// 对象是否是反序列化出来的
        /// </summary>
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        protected bool IsSerialized { get; private set; }
        
        /// <summary>
        /// 对象是否挂载在场景上
        /// </summary>
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        protected bool IsMountedOnScene { get; private set; }

#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
        public bool IsDisposed => InstanceId == 0;
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfDefault]
        [MongoDB.Bson.Serialization.Attributes.BsonDefaultValue(0L)]
        [MongoDB.Bson.Serialization.Attributes.BsonElement]
        [MongoDB.Bson.Serialization.Attributes.BsonId]
#endif
        public long Id { get; set; }
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        protected ILogicScene _logicScene;

#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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

                bool isMountedOnScene = IsMountedOnScene;
                IsMountedOnScene = true;

                if (IsSerialized && !isMountedOnScene)
                {
                    GetEntitySystem().OnAfterDeserialize(this);
                }
                
                GetEntitySystem().OnMountToScene(this);
            }
        }


        #endregion

        #region Entity Tree
        /// <summary>
        /// key: Entity Id
        /// </summary>
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        protected SortedDictionary<long, Entity> _children;
        /// <summary>
        /// 孩子节点
        /// key: Entity Id
        /// </summary>
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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

        
        public Entity AddChild(Type type, bool isFromPool = false) 
        {
            Entity entity = Entity.Create(type, isFromPool);
            entity.Id = Bliss.Id.IdGenerator.Instance.GenerateId();
            entity.Parent = this;
            GetEntitySystem().Awake(entity);
            return entity;
        }
        
        public Entity AddChildWithId(Type type, long id, bool isFromPool = false) 
        {
            Entity entity = Entity.Create(type, isFromPool);
            entity.Id = id;
            entity.Parent = this;
            GetEntitySystem().Awake(entity);
            return entity;
        }
        
        public T AddChild<T>(bool isFromPool = false) where T : Entity
        {
            return (T)AddChild(typeof(T), isFromPool);
        }
        
        public T AddChild<T, TArg>(TArg arg, bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = Bliss.Id.IdGenerator.Instance.GenerateId();
            entity.Parent = this;
            GetEntitySystem().Awake<TArg>(entity, arg);
            return entity;
        }
        
        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity
        {
            return (T)AddChildWithId(typeof(T), id, isFromPool);
        }
        
        public T AddChildWithId<T, TArg>(long id, TArg arg, bool isFromPool = false) where T : Entity
        {
            T entity = (T)Entity.Create(typeof(T), isFromPool);
            entity.Id = id;
            entity.Parent = this;
            GetEntitySystem().Awake<TArg>(entity, arg);
            return entity;
        }

        /// <summary>
        /// 获取孩子节点
        /// </summary>
        /// <param name="id">Entity Id</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        protected Entity _parent;

#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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
                    if (_parent == value)
                    {
                        Log.Error($"重复设置了Parent: {GetType().FullName} parent: {_parent.GetType().FullName}");
                        return;
                    }
                    
                    _parent.RemoveFromChildren(this);
                }
                
                _parent = value;
                IsComponent = false;
                _parent.AddToChildren(this);
                
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
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        protected SortedDictionary<long, Entity> _components;
        /// <summary>
        /// Entity的组件
        /// </summary>
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [SerializeIgnore]
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
            EarlyCheckDuplicateComponents(component.GetType());
            
            component.SetComponentParent(this);
            return component;
        }
        
        public Entity AddComponent(Type componentType, bool isFromPool = false)
        {
            EarlyCheckDuplicateComponents(componentType);
            
            Entity component = Create(componentType, isFromPool);
            component.Id = Id;
            component.SetComponentParent(this);
            GetEntitySystem().Awake(component);
            return component;
        }

        public TComponent AddComponent<TComponent>(bool isFromPool = false) where TComponent : Entity
        {
            Type componentType = typeof(TComponent);
            return (TComponent)AddComponent(componentType, isFromPool);
        }
        
        public TComponent AddComponent<TComponent, TParam>(TParam param, bool isFromPool = false) where TComponent : Entity
        {
            Type componentType = typeof(TComponent);
            EarlyCheckDuplicateComponents(componentType);
            
            Entity component = Create(componentType, isFromPool);
            component.Id = Id;
            component.SetComponentParent(this);
            GetEntitySystem().Awake<TParam>(component, param);
            return (TComponent)component;
        }

        public TComponent AddComponentWithId<TComponent>(int id, bool isFromPool = false) where TComponent : Entity
        {
            Type componentType = typeof(TComponent);
            EarlyCheckDuplicateComponents(componentType);
            
            Entity component = Create(componentType, isFromPool);
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
            
            if (!_components.TryGetValue(GetLongHashCode(type), out var component))
            {
                return;
            }

            RemoveFromComponents(component);
            component.Dispose();
        }

        private void EarlyCheckDuplicateComponents(Type componentType)
        {
            if (_components != null && _components.ContainsKey(GetLongHashCode(componentType)))
            {
                throw new Exception($"entity already has component: {componentType.FullName}");
            }
        }
        
        
        #endregion

        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonElement("Children")]
        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfNull]
#endif
        [SerializeInclude]
        private List<Entity> _childrenSerializer = null;
        
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonElement("Cmps")]
        [MongoDB.Bson.Serialization.Attributes.BsonIgnoreIfNull]
#endif
        [SerializeInclude]
        private List<Entity> _componentSerializer = null;
        
        public override void BeforeSerialize()
        {
            base.BeforeSerialize();
            GetEntitySystem().OnBeforeSerialize(this);
            
            _componentSerializer?.Clear();
            if (_components != null && _components.Count > 0)
            {
                if (_componentSerializer == null)
                {
                    _componentSerializer = ObjectPool.Instance.Fetch<List<Entity>>();
                }
                foreach (Entity component in _components.Values)
                {
                    if (component is not ISerializableEntity)
                    {
                        continue;
                    }
                    component.BeforeSerialize();
                    _componentSerializer.Add(component);
                }
            }
            
            _childrenSerializer?.Clear();
            if (_children != null && _children.Count > 0)
            {
                if (_childrenSerializer == null)
                {
                    _childrenSerializer = ObjectPool.Instance.Fetch<List<Entity>>();
                }
                
                foreach (Entity child in _children.Values)
                {
                    if (child is not ISerializableEntity)
                    {
                        continue;
                    }
                    child.BeforeSerialize();
                    
                    _childrenSerializer.Add(child);
                }
            }
        }

        /// <summary>
        /// 清理序列化状态
        /// </summary>
        public override void AfterSerialize()
        {
            base.AfterSerialize();
            
            _componentSerializer?.Clear();
            if (_componentSerializer != null)
            {
                ObjectPool.Instance.Recycle(_componentSerializer);
            }
            _componentSerializer = null;
            if (_components != null && _components.Count > 0)
            {
                foreach (Entity component in _components.Values)
                {
                    component.AfterSerialize();   
                }
            }
            
            _childrenSerializer?.Clear();
            if (_childrenSerializer != null)
            {
                ObjectPool.Instance.Recycle(_childrenSerializer);
            }
            _childrenSerializer = null;
            if (_children != null && _children.Count > 0)
            {
                foreach (Entity child in _children.Values)
                {
                    child.AfterSerialize();   
                }
            }
            
        }
        
        /// <summary>
        /// AfterDeserialize
        /// 从Serializer集合中还原component和child集合，以及他们的父子关系
        /// 完成后清除Serializer集合
        /// </summary>
        public override void EndInit()
        {
            base.EndInit();
            IsSerialized = true;
            
            if (_componentSerializer != null && _componentSerializer.Count > 0)
            {
                foreach (Entity component in _componentSerializer)
                {
                    component.IsComponent = true;
                    component.IsSerialized = true;
                    AddToComponents(component);
                    component._parent = this;
                    component.EndInit();
                }
            }
            
            if (_childrenSerializer != null && _childrenSerializer.Count > 0)
            {
                foreach (Entity child in _childrenSerializer)
                {
                    child.IsComponent = false;
                    child.IsSerialized = true;
                    AddToChildren(child);
                    child._parent =  this;
                    child.EndInit();
                }
            }
            
            _componentSerializer?.Clear();
            _componentSerializer = null;
            
            _childrenSerializer?.Clear();
            _childrenSerializer = null;
        }
        
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
            IsMountedOnScene = false;
            
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
        protected EntitySystem GetEntitySystem()
        {
            return LogicScene.Root.EntitySystem;
        }
    }
}