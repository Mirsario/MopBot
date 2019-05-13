using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MopBotTwo.Systems;

namespace MopBotTwo
{
	public class MemoryDataConverter : JsonConverter
	{
		public override bool CanRead => true;
		public override bool CanWrite => true;

		public override bool CanConvert(Type objectType) => objectType.IsGenericType && (objectType.GetGenericTypeDefinition()==typeof(IDictionary<,>) || objectType.GetGenericTypeDefinition()==typeof(Dictionary<,>));
		//public virtual Type ChooseValueType(object key)		=> throw new NotImplementedException();

		public override object ReadJson(JsonReader reader,Type objectType,object existingValue,JsonSerializer serializer)
		{
			if(!CanConvert(objectType)) {
				throw new Exception(string.Format("This converter is not for {0}.",objectType));
			}

			var keyType = objectType.GetGenericArguments()[0];
			var valueType = objectType.GetGenericArguments()[1];
			var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType,valueType);
			var result = (IDictionary)Activator.CreateInstance(dictionaryType);

			if(reader.TokenType==JsonToken.Null) {
				return null;
			}

			string key = null;

			while(reader.Read()) { // "name":
				if(reader.TokenType==JsonToken.PropertyName) {
					key = reader.Value as string;

					if(key==null) {
						throw new JsonException("Key must be a string");
					}
				} else if(reader.TokenType==JsonToken.StartObject) {
					var split = key.Split("|");
					if(split.Length!=2) {
						throw new JsonException("Key doesn't contain Memory Type");
					}

					Type type = MemorySystem.dataProvaiderInfo[(Assembly.GetExecutingAssembly().GetType(split[0]),split[1])].dataType;

					result.Add(key,type==null ? null : JObject.Load(reader).ToObject(type));
				} else if(reader.TokenType==JsonToken.EndObject) {
					break;
				} else {
					throw new JsonException($"Unexpected token: {reader.TokenType}");
				}
			}

			return result;
		}
		public override void WriteJson(JsonWriter writer,object obj,JsonSerializer serializer)
		{
			var objectType = obj.GetType();
			if(!CanConvert(objectType)) {
				throw new Exception(string.Format("This converter is not for {0}.",objectType));
			}
			var dict = obj as IDictionary;

			writer.WriteStartObject();

			foreach(DictionaryEntry entry in dict) {
				writer.WritePropertyName(entry.Key as string);
				serializer.Serialize(writer,entry.Value);
			}

			writer.WriteEndObject();
		}
	}
}