using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

namespace Bliss.ObjectPool
{
    public class ObjectPool
    {
        private ConcurrentDictionary<Type, Pool> _pools = new ConcurrentDictionary<Type, Pool>();
        private class Pool
        {
            private readonly Type _objectType;
            private readonly ConcurrentQueue<object> _items = new ConcurrentQueue<object>();
            private readonly int _capacity;

            private Pool(Type objectType, int capacity)
            {
                _objectType = objectType;
                _capacity = capacity;
            }
        }
    }
}