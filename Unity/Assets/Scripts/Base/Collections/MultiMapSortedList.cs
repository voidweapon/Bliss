using System;
using DataStructures.SortedCollections;

namespace Bliss.Base.Collections
{
    public class MultiMapSortedList<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, SortedList<TValue>>
    where TValue : IComparable<TValue>
    {
        private readonly SortedList<TValue> Empty = new();
        private readonly int maxPoolCount;
        private readonly System.Collections.Generic.Queue<DataStructures.SortedCollections.SortedList<TValue>> pool;

        public MultiMapSortedList(int maxPoolCount = 0)
        {
            this.maxPoolCount = maxPoolCount;
            this.pool = new System.Collections.Generic.Queue<SortedList<TValue>>(maxPoolCount);
        }

        private SortedList<TValue> FetchList()
        {
            if (this.pool.Count > 0)
            {
                return this.pool.Dequeue();
            }
            return new SortedList<TValue>();
        }

        private void Recycle(SortedList<TValue> list)
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

        public void Add(TKey key, TValue value)
        {
            SortedList<TValue> list;
            this.TryGetValue(key, out list);
            if (list == null)
            {
                list = this.FetchList();
                this.Add(key, list);
            }
            list.Add(value);
        }

        public bool Remove(TKey t, TValue k)
        {
            SortedList<TValue> list;
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
            SortedList<TValue> list;
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
            SortedList<TValue> list;

            this.TryGetValue(t, out list);
            if (list == null)
            {
                return Array.Empty<TValue>();
            }
            
            TValue[] array = new TValue[list.Count];
            list.CopyTo(array, 0);

            return array;
        }

        /// <summary>
        /// 返回内部的list
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public new SortedList<TValue> this[TKey t]
        {
            get
            {
                this.TryGetValue(t, out SortedList<TValue> list);
                return list ?? Empty;
            }
        }

        public TValue GetOne(TKey t)
        {
            SortedList<TValue> list;
            this.TryGetValue(t, out list);
            if (list != null && list.Count > 0)
            {
                return list[0];
            }
            return default;
        }

        public bool Contains(TKey t, TValue k)
        {
            SortedList<TValue> list;
            this.TryGetValue(t, out list);
            if (list == null)
            {
                return false;
            }
            return list.Contains(k);
        }
    }
}