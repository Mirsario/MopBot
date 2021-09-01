using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MopBot.Extensions;

namespace MopBot.Core.Systems.Memory
{
	public partial class MemoryBase<TPerSystemDataType>
	{
		public override void ReadFromJson(JObject jObj)
		{
			base.ReadFromJson(jObj);

			var type = GetType();

			JObject dataDict = jObj.Value<JObject>("perSystemData");

			if (dataDict != null) {
				foreach (var pair in dataDict) {
					var system = pair.Key;

					if (!MemorySystem.dataProvaiderInfo.TryGetValue((type, system), out var infoTuple)) {
						Console.WriteLine($"Unable to get data provaider info with tuple ({type.Name},{system}).");

						continue;
					}

					try {
						systemData.Add(system, (TPerSystemDataType)pair.Value.ToObject(infoTuple.dataType));
					}
					catch { }
				}
			}

			var subMemoriesDict = jObj.Value<JObject>("subMemory");

			if (subMemoriesDict != null) {
				foreach (var pair in subMemoriesDict) {
					if (!subMemory.TryGetFirst(p => p.Key != null && (p.Key.Name == pair.Key || p.Key.ToString() == pair.Key), out var existingDict)) {
						continue;
					}

					if (pair.Value is not JObject subMemoryDict) {
						continue;
					}

					var memoryType = existingDict.Key;
					var memoryDict = existingDict.Value;

					try {
						if (memoryType == null) {
							throw new Exception($"'{nameof(memoryType)}' is null. {nameof(pair)}.{nameof(pair.Key)}: '{pair.Key}'.");
						}

						foreach (var subPair in subMemoryDict) {
							if (!ulong.TryParse(subPair.Key, out ulong id)) {
								Console.WriteLine($"Invalid key: '{id}'.");

								continue;
							}

							if (subPair.Value is not JObject jMemoryObj) {
								Console.WriteLine($"subPair.Value is not a JObject.");

								continue;
							}

							var memoryObj = (MemoryBase)Activator.CreateInstance(memoryType);

							memoryObj.id = id;
							memoryObj.Initialize();
							memoryObj.ReadFromJson(jMemoryObj);

							memoryDict[id] = memoryObj;
						}
					}
					catch { }
				}
			}
		}

		public override void WriteToJson(ref JObject jObj)
		{
			base.WriteToJson(ref jObj);

			if (systemData?.Count > 0) {
				JObject dataDict = new JObject();

				foreach (var pair in systemData) {
					var val = JObject.FromObject(pair.Value);

					if (val.ToString(Formatting.None) != "{}") {
						dataDict[pair.Key] = val;
					}
				}

				jObj["perSystemData"] = dataDict;
			}

			if (subMemory?.Count > 0) {
				var subMemoriesDict = new JObject();

				foreach (var pair in subMemory) {
					var subMemoryDict = new JObject();

					foreach (var subPair in pair.Value) {
						var subMemoryObj = new JObject();

						subPair.Value.WriteToJson(ref subMemoryObj);

						subMemoryDict[subPair.Key.ToString()] = subMemoryObj;
					}

					subMemoriesDict[pair.Key.Name] = subMemoryDict;
				}

				jObj["subMemory"] = subMemoriesDict;
			}
		}
	}
}
