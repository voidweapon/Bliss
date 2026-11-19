using System;
using System.Collections.Generic;

namespace Bliss.Base.Collections
{
    public class IndexList<TKey, TValue> : List<TValue>
    {
        private Dictionary<TKey, int> _key2IndexMap = new Dictionary<TKey, int>();
        private Dictionary<int, TKey> _index2KeyMap = new Dictionary<int, TKey>();
        
        [Obsolete("Use Add(TKey key, TValue value) instead.", true)]
        public new void Add(TValue value)
        {
           throw new NotSupportedException("Use Add(TKey key, TValue value) instead.");
        }

        [Obsolete("Use Add(TKey key, TValue value) instead.", true)]
        public new void AddRange(IEnumerable<TValue> collection)
        {
           throw new NotSupportedException("Use Add(TKey key, TValue value) instead.");
        }
        
        public void Add(TKey key, TValue value)
        {
            if (_key2IndexMap.ContainsKey(key))
            {
                return;
            }
            _key2IndexMap.Add(key, Count);
            _index2KeyMap.Add(Count, key);
            base.Add(value);
        }

        /// <summary>
        /// Removes a keyed value by moving the last value into its slot; order is not preserved.
        /// A key that is not present is ignored.
        /// </summary>
        /// <param name="key">The key of the value to remove.</param>
        public void Remove(TKey key)
        {
            if (_key2IndexMap.TryGetValue(key, out var index))
            {
                int lastIndex = Count - 1;
                if (index != lastIndex)
                {
                    TKey lastKey = _index2KeyMap[lastIndex];
                    this[index] = this[lastIndex];
                    _index2KeyMap[index] = lastKey;
                    _key2IndexMap[lastKey] = index;
                }

                _key2IndexMap.Remove(key);
                _index2KeyMap.Remove(lastIndex);
                base.RemoveAt(lastIndex);
            }
        }

        public new void Remove(TValue value)
        {
            int index = IndexOf(value);
            RemoveAt(index);
        }

        public new void RemoveAt(int index)
        {
            if (index == -1)
            {
                return;
            }
            if(_index2KeyMap.TryGetValue(index, out var key))
            {
                Remove(key);
            }
        }

        public new void Clear()
        {
            base.Clear();
            _key2IndexMap.Clear();
            _index2KeyMap.Clear();
        }
    }
}