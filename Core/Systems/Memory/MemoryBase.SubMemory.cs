using System;
using System.Linq;
using System.Collections.Generic;


namespace MopBotTwo.Core.Systems.Memory
{
	public partial class MemoryBase<TPerSystemDataType>
	{
		public void RegisterSubMemory<T>() where T : MemoryBase
		{
			subMemory.Add(typeof(T),new Dictionary<ulong,MemoryBase>());
		}

		public T GetSubMemory<T>(ulong id) where T : MemoryBase
		{
			var type = typeof(T);
			var dict = subMemory[type];
			if(!dict.TryGetValue(id,out MemoryBase tempMemory) || !(tempMemory is T resultMemory)) {
				dict[id] = resultMemory = (T)Activator.CreateInstance(typeof(T));
				resultMemory.id = id;
			}

			return resultMemory;
		}
		public void SetSubMemory<T>(ulong id,T value) where T : MemoryBase
		{
			var type = typeof(T);
			var dict = subMemory[type];
			if(value==null) {
				dict.Remove(id);
			} else {
				dict[id] = value;
			}
		}

		public Dictionary<ulong,T> GetSubMemories<T>() where T : MemoryBase => subMemory[typeof(T)].Keys.Select(key => (key,GetSubMemory<T>(key))).ToDictionary(t => t.key,t => t.Item2);
	}
}