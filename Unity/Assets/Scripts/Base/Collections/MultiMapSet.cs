using System.Collections.Generic;

namespace Bliss.Base.Collections
{
    /// <summary>
    /// key -> HashSet  Collection
    /// <para><c>not thread-safe</c></para>
    /// </summary>
    public class MultiMapSet<TKey, TValue> : Dictionary<TKey, HashSet<TValue>>
    {
        public void Add(TKey key, TValue value)
        {
            if (!this.TryGetValue(key, out var hashSet))
            {
                hashSet = new HashSet<TValue>();
                this.Add(key, hashSet);
            }
            hashSet.Add(value);
        }

        public void Remove(TKey key, TValue value)
        {
            if (this.TryGetValue(key, out var hashSet))
            {
                hashSet.Remove(value);
            }
        }
        
        public bool Contains(TKey key, TValue value)
        {
            if (this.TryGetValue(key, out var hashSet))
            {
                return hashSet.Contains(value);
            }
            return false;
        }

        public new HashSet<TValue> this[TKey key]
        {
            get
            {
                this.TryGetValue(key, out var value);
                return value;
            }
        }

        
        /// <summary>
        /// Sum item count of hashset in dictionary
        /// </summary>
        public int TotalCount
        {
            get
            {
                int sum = 0;
                foreach (var pair in this)
                {
                    sum += pair.Value.Count;
                }
                return sum;
            }
        }
    }
}