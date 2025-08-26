using System;
using System.Collections.Generic;
using System.Linq;

namespace Bliss.Base.Collections
{
    /// <summary>
    /// key -> HashSet Sorted Collection
    /// <para><c>not thread-safe</c></para>
    /// </summary>
    public class SortedMultiMapSet<TKey, TValue>: SortedDictionary<TKey, HashSet<TValue>>
    {
        private readonly HashSet<TValue> Empty = new HashSet<TValue>();
		
        public void Add(TKey t, TValue k)
        {
            HashSet<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                list = new HashSet<TValue>();
                this.Add(t, list);
            }
            list.Add(k);
        }

        public bool Remove(TKey t, TValue k)
        {
            HashSet<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            if (!list.Remove(k))
            {
                return false;
            }
            if (list.Count == 0)
            {
                this.Remove(t);
            }
            return true;
        }

        /// <summary>
        /// 不返回内部的list,copy一份出来
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public TValue[] GetAll(TKey t)
        {
            HashSet<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return Array.Empty<TValue>();
            }
            return list.ToArray();
        }

        /// <summary>
        /// 返回内部的list
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public new HashSet<TValue> this[TKey t]
        {
            get
            {
                this.TryGetValue(t, out var list);
                return list ?? Empty;
            }
        }

        public TValue GetOne(TKey t)
        {
            HashSet<TValue> list;
            this.TryGetValue(t, out list);
            if (list != null && list.Count > 0)
            {
                return list.FirstOrDefault();
            }
            return default;
        }

        public bool Contains(TKey t, TValue k)
        {
            HashSet<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            return list.Contains(k);
        }
    }
}