using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MopBotTwo.Systems;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo
{
	public class MemoryDataBase {}
	
	public abstract class MemoryBase
	{
		[JsonIgnore]
		public ulong id;

		protected virtual string Name => null; //Doesn't do anything

		public virtual void Initialize() {}

		#region IO
		public virtual void ReadFromJson(JObject jObj) {}
		public virtual void WriteToJson(ref JObject jObj)
		{
			string name = Name;
			if(!string.IsNullOrEmpty(name)) {
				jObj["name"] = name;
			}
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

		public string ToString(Formatting jsonFormatting)
		{
			var jObj = new JObject();
			WriteToJson(ref jObj);
			return jObj.ToString(jsonFormatting);
		}
		#endregion
	}
	public abstract class MemoryBase<TPerSystemDataType> : MemoryBase
	{
		public Dictionary<string,TPerSystemDataType> systemData = new Dictionary<string,TPerSystemDataType>();
		public Dictionary<Type,Dictionary<ulong,MemoryBase>> subMemory = new Dictionary<Type,Dictionary<ulong,MemoryBase>>();

		#region SystemData
		public virtual void OnDataCreated(TPerSystemDataType data) {}

		public TDataType GetData<TSystem,TDataType>() where TSystem : BotSystem where TDataType : TPerSystemDataType => (TDataType)GetData(typeof(TSystem));
		public TDataType GetData<TDataType>(Type provaiderType) where TDataType : TPerSystemDataType => (TDataType)GetData(provaiderType);
		public TPerSystemDataType GetData(Type provaiderType)
		{
			string key = provaiderType.Name;
			var infoKey = (GetType(),provaiderType.Name);
			var (_,realDataType) = MemorySystem.dataProvaiderInfo[infoKey];

			if(!systemData.TryGetValue(key,out TPerSystemDataType dataObj) || dataObj==null) {
				systemData[key] = dataObj = (TPerSystemDataType)Activator.CreateInstance(realDataType);
				OnDataCreated(dataObj);
			}

			return dataObj;
		}
		public void SetData<TSystem,TDataType>(TDataType value) where TSystem : BotSystem where TDataType : TPerSystemDataType
		{
			var dataType = typeof(TDataType);

			string provaiderName = typeof(TSystem).Name;
			if(!MemorySystem.dataProvaiderInfo.TryGetValue((GetType(),provaiderName),out var tuple) || dataType!=tuple.dataType) {
				throw new ArgumentException($@"Incorrect TDataType generic: ""{dataType}""");
			}

			if(value==null) {
				systemData.Remove(provaiderName);
			} else {
				systemData[provaiderName] = value;
			}
		}
		#endregion
		#region SubMemory
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
		#endregion

		#region IO
		public override void ReadFromJson(JObject jObj)
		{
			base.ReadFromJson(jObj);
			var type = GetType();

			JObject dataDict = jObj.Value<JObject>("perSystemData");
			if(dataDict!=null) {
				foreach(var pair in dataDict) {
					var system = pair.Key;
					if(!MemorySystem.dataProvaiderInfo.TryGetValue((type,system),out var infoTuple)) {
						continue;
					}
					try {
						systemData.Add(system,(TPerSystemDataType)pair.Value.ToObject(infoTuple.dataType));
					}
					catch {}
				}
			}

			var subMemoriesDict = jObj.Value<JObject>("subMemory");
			if(subMemoriesDict!=null) {
				foreach(var pair in subMemoriesDict) {
					if(!subMemory.TryGetFirst(p => p.Key!=null && (p.Key.Name==pair.Key || p.Key.ToString()==pair.Key),out var existingDict)) {
						continue;
					}
					if(!(pair.Value is JObject subMemoryDict)) {
						continue;
					}

					var memoryType = existingDict.Key;
					var memoryDict = existingDict.Value;

					foreach(var subPair in subMemoryDict) {
						if(!ulong.TryParse(subPair.Key,out ulong id) || !(subPair.Value is JObject jMemoryObj)) {
							continue;
						}

						try {
							if(memoryType==null) {
								throw new Exception($"memoryType is null. pair.Key: '{pair.Key}'.");
							}
							
							var memoryObj = (MemoryBase)Activator.CreateInstance(memoryType);
							memoryObj.id = id;
							memoryObj.ReadFromJson(jMemoryObj);
							memoryObj.Initialize();
							memoryDict[id] = memoryObj;
						}
						catch {}
					}
				}
			}
		}
		public override void WriteToJson(ref JObject jObj)
		{
			base.WriteToJson(ref jObj);

			if(systemData?.Count>0) {
				JObject dataDict = new JObject();
				foreach(var pair in systemData) {
					var val = JObject.FromObject(pair.Value);
					if(val.ToString(Formatting.None)!="{}") {
						dataDict[pair.Key] = val;
					}
				}
				jObj["perSystemData"] = dataDict;
			}

			if(subMemory?.Count>0) {
				var subMemoriesDict = new JObject();
				foreach(var pair in subMemory) {
					var subMemoryDict = new JObject();
					foreach(var subPair in pair.Value) {
						var subMemoryObj = new JObject();
						subPair.Value.WriteToJson(ref subMemoryObj);
						subMemoryDict[subPair.Key.ToString()] = subMemoryObj;
					}
					subMemoriesDict[pair.Key.Name] = subMemoryDict;
				}
				jObj["subMemory"] = subMemoriesDict;
			}
		}
		#endregion
	}
}