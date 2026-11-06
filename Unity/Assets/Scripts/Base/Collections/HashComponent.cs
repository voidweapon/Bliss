using System;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss.Base.Collections
{
    public class HashComponent<T> : HashSet<T>, IDisposable
    {
        public HashComponent()
        {
            
        }

        public static HashComponent<T> Create()
        {
            return ObjectPool.Instance.Fetch<HashComponent<T>>();
        }
        public void Dispose()
        {
            Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }
}