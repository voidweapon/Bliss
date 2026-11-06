using System;
using System.Collections;
using System.Collections.Generic;

namespace Bliss
{
    [WeakRefConstraint]
    public struct EntityRef<T> where T : Entity
    {
        public static readonly EqualityComparer Comparer = new EqualityComparer();

        private long _instanceId;
        private T _entity;

        public EntityRef(T entity)
        {
            if (entity == null)
            {
                _instanceId = 0;
                _entity = null;
                return;
            }
            
            _instanceId = entity.InstanceId;
            _entity = entity;
        }
        
        private T UnPack
        {
            get
            {
                if (_entity == null || _instanceId == 0)
                {
                    return null;
                }
                if (_entity.InstanceId != _instanceId)
                {
                    // 这里instanceId变化了，设置为null，解除引用，好让runtime去gc
                    _instanceId = 0;
                    _entity = null;
                }
                return _entity;
            }
        }
        
        public static implicit operator EntityRef<T>(T t)
        {
            return new EntityRef<T>(t);
        }

        // 不允许隐式解引用
        public static explicit operator T(EntityRef<T> v)
        {
            return v.UnPack;
        }

        public override int GetHashCode()
        {
            return (int)_instanceId;
        }

        public bool Validate()
        {
            return _instanceId != 0 
                   &&  _entity != null 
                   && _instanceId == _entity.InstanceId ;
        }

        public class EqualityComparer : IEqualityComparer<EntityRef<T>>
        {
            public bool Equals(EntityRef<T> x, EntityRef<T> y)
            {
                return x._instanceId == y._instanceId;
            }

            public int GetHashCode(EntityRef<T> obj)
            {
                return (int)obj._instanceId;
            }
        }
    }
    
    [WeakRefConstraint]
    public struct EntityWeakRef<T> where T : Entity
    {
        public static readonly EqualityComparer Comparer = new EqualityComparer();
        private long _instanceId;
        private WeakReference<T> _entityWeakRef;

        public EntityWeakRef(T entity)
        {
            if (entity == null)
            {
                _instanceId = 0;
                _entityWeakRef = null;
                return;
            }
            _instanceId = entity.InstanceId;
            _entityWeakRef = new WeakReference<T>(entity);
        }
        
        private T UnPack
        {
            get
            {
                if (_entityWeakRef == null || _instanceId == 0)
                {
                    return null;
                }
                
                if (!_entityWeakRef.TryGetTarget(out T entity))
                {
                    _instanceId = 0;
                    return null;
                }
                
                if (entity.InstanceId != _instanceId)
                {
                    // 这里instanceId变化了，设置为null，解除引用，好让runtime去gc
                    _entityWeakRef = null;
                }
                return entity;
            }
        }
        
        public static implicit operator EntityWeakRef<T>(T t)
        {
            return new EntityWeakRef<T>(t);
        }
        
        //不允许隐式解引用
        public static explicit operator T(EntityWeakRef<T> v)
        {
            return v.UnPack;
        }
        
        public override int GetHashCode()
        {
            return (int)_instanceId;
        }
        
        public class EqualityComparer : IEqualityComparer<EntityWeakRef<T>>
        {
            public bool Equals(EntityWeakRef<T> x, EntityWeakRef<T> y)
            {
                return x._instanceId == y._instanceId;
            }

            public int GetHashCode(EntityWeakRef<T> obj)
            {
                return (int)obj._instanceId;
            }
        }
    }
    
}