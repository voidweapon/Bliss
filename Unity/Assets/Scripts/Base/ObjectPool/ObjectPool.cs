using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bliss.Pool
{
    public class ObjectPool : IDisposable
    {
        private static ObjectPool instance;

        public static ObjectPool Instance
        {
            get
            {
                if (instance == null) 
                    instance = new ObjectPool(); 
                return instance;
            }
        }

        private ConcurrentDictionary<Type, Pool> _pools = new ConcurrentDictionary<Type, Pool>();
        private Func<Type, Pool> funcAddPool = type => new Pool(type, 1024);

        
        public void Dispose()
        {
            instance = null;
        }
        
        public T Fetch<T>(bool isFromPool = true)
        {
            return (T)Fetch(typeof(T), isFromPool);
        }
        public object Fetch(Type type, bool isFromPool = true)
        {
            if (!isFromPool)
            {
                return Activator.CreateInstance(type);
            }
            
            var pool = GetPool(type);
            
            var obj = pool.Fetch();
            if (obj is IPoolableObject poolableObject)
            {
                poolableObject.IsFromPool = true;
                poolableObject.IsInPool = false;
            }
            
            return obj;
        }

        public void Recycle(object obj)
        {
            if(obj == null) return;
            
            if (obj is IPoolableObject poolableObject)
            {
                if (!poolableObject.IsFromPool)
                {
                    return;
                }
                
                if (poolableObject.IsInPool)
                {
                    throw new InvalidOperationException($"{obj} The object has been recycled");
                }
                
                poolableObject.IsFromPool = true;
                poolableObject.IsInPool = true;
            }
            
            Pool pool = GetPool(obj.GetType());
            pool.Recycle(obj);
        }
        
        public void Clear(Type type)
        {
            if(_pools.TryGetValue(type, out var pool))
            {
                pool.Clear();
            }
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pool GetPool(Type objectType)
        { 
            return _pools.GetOrAdd(objectType, funcAddPool);
        }
        
        private class Pool
        {
            private readonly Type _objectType;
            private readonly ConcurrentQueue<object> _items = new ConcurrentQueue<object>();
            private readonly int _capacity;
            private int _numItems;
            private object _fastItem;
            
            public Pool(Type objectType, int capacity)
            {
                _objectType = objectType;
                _capacity = capacity;
            }
            
            public object Fetch()
            {
                object item = _fastItem;
                if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) == item)
                {
                    if (_items.TryDequeue(out item))
                    {
                        Interlocked.Decrement(ref _numItems);
                        return item;
                    }
                    
                    return Activator.CreateInstance(_objectType);
                }
                
                return item;
            }

            public void Recycle(object obj)
            {
                if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
                {
                    if (Interlocked.Increment(ref _numItems) <= _capacity)
                    {
                        _items.Enqueue(obj);
                        return;
                    }
                    Interlocked.Decrement(ref _numItems);
                }
            }
            
            public void Clear()
            {
                // 清空 FastItem
                Interlocked.Exchange(ref _fastItem, null);

                // 清空队列
                while (_items.TryDequeue(out _)) { }
                // 重置计数
                Interlocked.Exchange(ref _numItems, 0);
            }
        }
    }
}