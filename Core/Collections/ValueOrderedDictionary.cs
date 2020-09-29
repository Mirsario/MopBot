using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

#pragma warning disable IDE0044

namespace MopBot.Collections
{
	//TODO: Turn into ValueOrderedDictionary<TKey,TValue>
	//TODO: Rewrite to get rid of internal lists & dictionaries.

	public class ValueOrderedDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
		where TValue : IComparable<TValue>
	{
		public class ValueIndexPair
		{
			public TValue value;
			public int index;
		}

		private List<TKey> orderedKeys;
		private ConcurrentDictionary<TKey, TValue> dictionary;

		public object SyncRoot { get; }
		public int Count => orderedKeys.Count;

		public bool IsFixedSize => false;
		public bool IsReadOnly => false;
		public bool IsSynchronized => false;
		public ICollection<TKey> Keys => throw new NotImplementedException();
		public ICollection<TValue> Values => throw new NotImplementedException();

		public TValue this[TKey key] {
			get => dictionary[key];
			set {
				if(dictionary.TryAdd(key, value)) {
					//Insert new entry

					for(int i = 0; i < Count; i++) {
						if(dictionary[orderedKeys[i]].CompareTo(value) < 0) {
							orderedKeys.Insert(i, key);
							return;
						}
					}

					orderedKeys.Add(key);
					return;
				}

				//Modify existing entry

				if(Equals(value, dictionary[key])) {
					return;
				}

				dictionary[key] = value;

				lock(SyncRoot) {
					int currentIndex = -1;
					int newIndex = -1;

					for(int i = 0; i < Count; i++) {
						var thisKey = orderedKeys[i];

						if(currentIndex == -1 && Equals(thisKey, key)) {
							currentIndex = i;

							if(newIndex != -1) {
								break;
							}
						} else if(newIndex == -1 && dictionary[thisKey].CompareTo(value) < 0) {
							newIndex = i;

							if(currentIndex != -1) {
								break;
							}
						}
					}

					if(currentIndex == -1) {
						throw new Exception("Couldn't find existing key's index.");
					}

					orderedKeys.RemoveAt(currentIndex);

					if(newIndex == -1) {
						orderedKeys.Add(key);
					} else {
						orderedKeys.Insert(newIndex > currentIndex ? newIndex - 1 : newIndex, key);
					}
				}
			}
		}

		public ValueOrderedDictionary()
		{
			orderedKeys = new List<TKey>();
			dictionary = new ConcurrentDictionary<TKey, TValue>();
			SyncRoot = new object();
		}

		public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
		public void Add(TKey key, TValue value)
		{
			lock(SyncRoot) {
				if(!dictionary.TryAdd(key, value)) {
					throw new ArgumentException($"Key '{key}' already exists.");
				}

				for(int i = 0; i < Count; i++) {
					if(dictionary[orderedKeys[i]].CompareTo(value) < 0) {
						orderedKeys.Insert(i, key);
						return;
					}
				}

				orderedKeys.Add(key);
			}
		}
		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		{
			lock(SyncRoot) {
				foreach(var pair in pairs) {
					var key = pair.Key;

					dictionary.TryAdd(key, pair.Value);
					orderedKeys.Add(key);
				}

				orderedKeys = orderedKeys.OrderByDescending(k => dictionary[k]).ToList();
			}
		}
		public void Clear()
		{
			lock(SyncRoot) {
				dictionary.Clear();
				orderedKeys.Clear();
			}
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			lock(SyncRoot) {
				for(int i = 0; i < Count; i++) {
					var key = orderedKeys[i];
					yield return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
				}
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) => dictionary.Contains(item);
		public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

		public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
		public bool Remove(TKey key)
		{
			if(dictionary.TryRemove(key, out _)) {
				orderedKeys.Remove(key);

				return true;
			}

			return false;
		}
		public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

		public void Sort()
		{

		}
	}
}
