using System;
using System.Collections.Generic;

namespace Bliss.Base.Collections
{
    /// <summary>
    /// key -> List  Collection
    /// <para><c>not thread-safe</c></para>
    /// </summary>
    public class MultiMapList<TKey, TValue>: Dictionary<TKey, List<TValue>>
    {
        public void Add(TKey key, TValue value)
        {
            if (!this.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                this.Add(key, list);
            }
            list.Add(value);
        }

        public void Remove(TKey key, TValue value)
        {
            if (this.TryGetValue(key, out var list))
            {
                list.Remove(value);
            }
        }
        
        public bool Contains(TKey key, TValue value)
        {
            if (this.TryGetValue(key, out var list))
            {
                return list.Contains(value);
            }
            return false;
        }

        public TValue GetOne(TKey key)
        {
            if (this.TryGetValue(key, out var list))
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            
            return default(TValue);
        }

        public TValue[] GetAllCopy(TKey key)
        {
            if (this.TryGetValue(key, out var list))
            {
                return list.ToArray();
            }
            return Array.Empty<TValue>();
        }

        public new List<TValue> this[TKey key]
        {
            get
            {
                this.TryGetValue(key, out var value);
                return value;
            }
        }
    }
}