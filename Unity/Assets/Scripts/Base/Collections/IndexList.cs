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

        public void Remove(TKey key)
        {
            if (_key2IndexMap.TryGetValue(key, out var index))
            {
                if (index == Count - 1)
                {
                    _key2IndexMap.Remove(key);
                    _index2KeyMap.Remove(index);

                    RemoveAt(index);
                }
                else
                {
                    _index2KeyMap[index] = _index2KeyMap[Count - 1];
                    _key2IndexMap[_index2KeyMap[Count - 1]] = index;
                    _key2IndexMap.Remove(key);
                    _index2KeyMap.Remove(Count - 1);
                    
                    this[index] = this[Count - 1];
                }
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