using System;
using System.Collections.Generic;

namespace Bliss.Base.Collections
{
	/// <summary>
	/// key -> value -> key Collection
	/// <para><c>not thread-safe</c></para>
	/// </summary>
	public class DoublyDictionary<TKey, TValue>
	{
		private readonly Dictionary<TKey, TValue> kv = new();
		private readonly Dictionary<TValue, TKey> vk = new();

		public DoublyDictionary()
		{
		}

		public DoublyDictionary(int capacity)
		{
			kv = new Dictionary<TKey, TValue>(capacity);
			vk = new Dictionary<TValue, TKey>(capacity);
		}

		public void ForEach(Action<TKey, TValue> action)
		{
			if (action == null)
			{
				return;
			}
			Dictionary<TKey, TValue>.KeyCollection keys = kv.Keys;
			foreach (TKey key in keys)
			{
				action(key, kv[key]);
			}
		}

		public List<TKey> Keys
		{
			get
			{
				return new List<TKey>(kv.Keys);
			}
		}

		public List<TValue> Values
		{
			get
			{
				return new List<TValue>(vk.Keys);
			}
		}

		public void Add(TKey key, TValue value)
		{
			if (key == null || value == null || kv.ContainsKey(key) || vk.ContainsKey(value))
			{
				return;
			}
			kv.Add(key, value);
			vk.Add(value, key);
		}

		public TValue GetValueByKey(TKey key)
		{
			if (key != null && kv.ContainsKey(key))
			{
				return kv[key];
			}
			return default(TValue);
		}

		public TKey GetKeyByValue(TValue value)
		{
			if (value != null && vk.ContainsKey(value))
			{
				return vk[value];
			}
			return default(TKey);
		}
		
		public bool TryGetValueByKey(TKey key, out TValue value)
		{
			if (key != null && kv.ContainsKey(key))
			{
				value = kv[key];
				return true;
			}

			value = default(TValue);
			return false;
		}

		public bool TryGetKeyByValue(TValue value, out TKey key)
		{
			if (value != null && vk.ContainsKey(value))
			{
				key = vk[value];
				return true;
			}
			key = default(TKey);
			return false;
		}

		public void RemoveByKey(TKey key)
		{
			if (key == null)
			{
				return;
			}
			TValue value;
			if (!kv.TryGetValue(key, out value))
			{
				return;
			}

			kv.Remove(key);
			vk.Remove(value);
		}

		public void RemoveByValue(TValue value)
		{
			if (value == null)
			{
				return;
			}

			TKey key;
			if (!vk.TryGetValue(value, out key))
			{
				return;
			}

			kv.Remove(key);
			vk.Remove(value);
		}

		public void Clear()
		{
			kv.Clear();
			vk.Clear();
		}

		public bool ContainsKey(TKey key)
		{
			if (key == null)
			{
				return false;
			}
			return kv.ContainsKey(key);
		}

		public bool ContainsValue(TValue value)
		{
			if (value == null)
			{
				return false;
			}
			return vk.ContainsKey(value);
		}

		public bool Contains(TKey key, TValue value)
		{
			if (key == null || value == null)
			{
				return false;
			}
			return kv.ContainsKey(key) && vk.ContainsKey(value);
		}
	}
}