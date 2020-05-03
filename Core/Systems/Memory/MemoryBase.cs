using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MopBot.Core.Systems.Memory
{
	public abstract class MemoryBase
	{
		[JsonIgnore] public ulong id;

		protected virtual string Name => null; //Doesn't do anything

		public virtual void Initialize() {}
		public virtual void ReadFromJson(JObject jObj) {}
		public virtual void WriteToJson(ref JObject jObj)
		{
			string name = Name;

			if(!string.IsNullOrEmpty(name)) {
				jObj["name"] = name;
			}
		}

		public string ToString(Formatting jsonFormatting)
		{
			var jObj = new JObject();

			WriteToJson(ref jObj);

			return jObj.ToString(jsonFormatting);
		}

		public static async Task<T> Load<T>(string filePath,ulong setId = 0,bool logExceptions = true) where T : MemoryBase
		{
			if(!File.Exists(filePath)) {
				return null;
			}

			try {
				var jObj = JObject.Parse(await File.ReadAllTextAsync(filePath));

				if(jObj!=null) {
					T result = (T)Activator.CreateInstance(typeof(T));

					result.id = setId;
					result.Initialize();
					result.ReadFromJson(jObj);

					return result;
				}
			}
			catch(Exception e) {
				if(logExceptions) {
					await MopBot.HandleException(e,"An error has occured when loading memory.");
				}
			}

			return null;
		}
		public static async Task Save(MemoryBase memory,string filePath)
		{
			try {
				await File.WriteAllTextAsync(filePath,memory.ToString(Formatting.Indented));
			}
			catch {}
		}
	}

	public abstract partial class MemoryBase<TPerSystemDataType> : MemoryBase
	{
		public Dictionary<string,TPerSystemDataType> systemData = new Dictionary<string,TPerSystemDataType>();
		public Dictionary<Type,Dictionary<ulong,MemoryBase>> subMemory = new Dictionary<Type,Dictionary<ulong,MemoryBase>>();
	}
}