using System;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss.Base.Collections
{
    
    [DisableNew]
    public class ArrayComponent<T> : IDisposable
    {
        public T[] ItemArray => _array;
        private T[] _array;
        
        public ArrayComponent()
        {
            
        }

        public static ArrayComponent<T> Create(int  size)
        {
            var cmp =  ObjectPool.Instance.Fetch<ArrayComponent<T>>();
            if (cmp._array == null)
            {
                cmp._array = new T[size];
            }
            
            if (cmp._array.Length < size)
            {
                Array.Resize(ref cmp._array, size);
            }
            return cmp;
        }
        
        public void Dispose()
        {
            Array.Clear(_array, 0, _array.Length);
            ObjectPool.Instance.Recycle(this);
        }
    }
}