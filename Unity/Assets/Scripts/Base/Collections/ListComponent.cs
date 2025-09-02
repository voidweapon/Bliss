using System;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss.Base.Collections
{
    public class ListComponent<T>: List<T>, IDisposable
    {
        private ListComponent()
        {
            
        }

        public static ListComponent<T> Create()
        {
            return ObjectPool.Instance.Fetch<ListComponent<T>>();
        }
        
        public void Dispose()
        {
            Clear();
            ObjectPool.Instance.Recycle(this);
        }
    }
}