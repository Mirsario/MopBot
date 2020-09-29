using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MopBot.Collections
{
	public struct NameIdValue<T>
	{
		public string name;
		public ulong id;
		public T value;
	}

	//TODO: Stop using sub dictionaries.
	//TODO: Add more interfaces?
	//TODO: Come up with a better name.
	[Serializable]
	public class BotIdCollection<T> : ICollection<NameIdValue<T>>
	{
		[JsonProperty] private readonly Dictionary<string, ulong> NameToId = new Dictionary<string, ulong>(StringComparer.InvariantCultureIgnoreCase);
		[JsonProperty] private readonly Dictionary<ulong, T> IdToValue = new Dictionary<ulong, T>();

		public T this[string key] => IdToValue[GetIdFromName(key)];
		public T this[ulong key] => IdToValue[key];

		public int Count => IdToValue.Count;
		public bool IsSynchronized => throw new NotImplementedException();
		public object SyncRoot => throw new NotImplementedException();
		public bool IsReadOnly => false;

		public void Add(NameIdValue<T> item)
		{
			NameToId[item.name] = item.id;
			IdToValue[item.id] = item.value;
		}
		public void Add(string name, T value)
		{
			StringUtils.CheckAndLowerStringId(ref name);

			if(NameToId.ContainsKey(name)) {
				throw new ArgumentException($"'{name}' item already exists in this collection."); //Should this throw BotErrors too?
			}

			ulong id = BotUtils.GenerateUniqueId(IdToValue.ContainsKey);

			NameToId[name] = id;
			IdToValue[id] = value;
		}
		public void Rename(string name, string newName)
		{
			StringUtils.CheckAndLowerStringId(ref newName);

			ulong id = GetIdFromName(name);

			if(NameToId.ContainsKey(newName)) {
				throw new BotError($"{typeof(T).Name} '{newName}' already exists.");
			}

			NameToId.Remove(name);
			NameToId[newName] = id;
		}
		public bool Remove(NameIdValue<T> item) => Remove(item.name);
		public bool Remove(string name)
		{
			if(NameToId.TryGetValue(name, out ulong id)) {
				NameToId.Remove(name);
				IdToValue.Remove(id);

				return true;
			}

			return false;
		}
		public void Clear()
		{
			NameToId.Clear();
			IdToValue.Clear();
		}
		//Ids
		public bool TryGetIdFromName(string name, out ulong id) => NameToId.TryGetValue(name, out id);
		public ulong GetIdFromName(string name) => NameToId.TryGetValue(name, out ulong id) ? id : throw new BotError($"Unknown `{typeof(T).Name}`: {name}");
		//Values
		public bool TryGetValue(ulong id, out T result) => IdToValue.TryGetValue(id, out result);
		public bool TryGetValue(string nameId, out T result, out ulong id)
		{
			if(NameToId.TryGetValue(nameId, out id)) {
				return IdToValue.TryGetValue(id, out result);
			}

			result = default;

			return false;
		}
		//Checks
		public bool ContainsKey(ulong key) => IdToValue.ContainsKey(key);
		public bool Contains(NameIdValue<T> item) => IdToValue.ContainsKey(item.id);
		//Etc
		public void CopyTo(Array array, int index) => throw new NotImplementedException();
		public void CopyTo(NameIdValue<T>[] array, int arrayIndex) => throw new NotImplementedException();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<NameIdValue<T>> GetEnumerator()
		{
			foreach(var pair in NameToId) {
				string name = pair.Key;
				ulong id = pair.Value;
				var value = IdToValue[id];

				yield return new NameIdValue<T> {
					name = name,
					id = id,
					value = value
				};
			}
		}
	}
}
