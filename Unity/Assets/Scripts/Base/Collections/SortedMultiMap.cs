using System;
using System.Collections.Generic;

namespace Bliss.Base.Collections
{
    /// <summary>
    /// key -> List Sorted Collection
    /// <para><c>not thread-safe</c></para>
    /// </summary>
    public class SortedMultiMap<TKey, TValue>: SortedDictionary<TKey, List<TValue>>
    {
        private readonly List<TValue> Empty = new();
        private readonly int maxPoolCount;
        private readonly Queue<List<TValue>> pool;

        public SortedMultiMap(int maxPoolCount = 0)
        {
            this.maxPoolCount = maxPoolCount;
            this.pool = new Queue<List<TValue>>(maxPoolCount);
        }

        private List<TValue> FetchList()
        {
            if (this.pool.Count > 0)
            {
                return this.pool.Dequeue();
            }
            return new List<TValue>(10);
        }

        private void Recycle(List<TValue> list)
        {
            if (list == null)
            {
                return;
            }
            if (this.pool.Count == this.maxPoolCount)
            {
                return;
            }
            list.Clear();
            this.pool.Enqueue(list);
        }

        public void Add(TKey t, TValue k)
        {
            List<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                list = this.FetchList();
                this.Add(t, list);
            }
            list.Add(k);
        }

        public bool Remove(TKey t, TValue k)
        {
            List<TValue> list;
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

        public new bool Remove(TKey t)
        {
            List<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            this.Recycle(list);
            return base.Remove(t);
        }

        /// <summary>
        /// 不返回内部的list,copy一份出来
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public TValue[] GetAll(TKey t)
        {
            List<TValue> list;
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
        public new List<TValue> this[TKey t]
        {
            get
            {
                this.TryGetValue(t, out List<TValue> list);
                return list ?? Empty;
            }
        }

        public TValue GetOne(TKey t)
        {
            List<TValue> list;
            this.TryGetValue(t, out list);
            if (list != null && list.Count > 0)
            {
                return list[0];
            }
            return default;
        }

        public bool Contains(TKey t, TValue k)
        {
            List<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            return list.Contains(k);
        }
    }
}