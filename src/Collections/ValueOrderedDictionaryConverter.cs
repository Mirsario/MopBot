using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MopBotTwo.Collections
{
	public class OrderedULongDictionaryConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => objectType==typeof(OrderedULongDictionaryConverter);

		public override object ReadJson(JsonReader reader,Type objectType,object existingValue,JsonSerializer serializer)
		{
			object GetToken(JsonToken type)
			{
				var tokenType = reader.TokenType;
				if(tokenType!=type) {
					throw new JsonSerializationException($"Unexpected token: '{tokenType}'. Expected '{type}'.");
				}
				var result = reader.Value;
				reader.Read();
				return result;
			}
			
			switch(reader.TokenType) {
				case JsonToken.Null:
					return null;
				case JsonToken.StartArray:
					reader.Read();

					if(reader.TokenType==JsonToken.EndArray) {
						return new OrderedULongDictionary();
					}

					throw new JsonSerializationException("Non-empty JSON array does not make a valid Dictionary!");
				case JsonToken.StartObject: {
					var list = new List<KeyValuePair<ulong,ulong>>();

					reader.Read();
					while(reader.TokenType!=JsonToken.EndObject) {
						list.Add(new KeyValuePair<ulong,ulong>(
							ulong.Parse((string)GetToken(JsonToken.PropertyName)),
							(ulong)(long)GetToken(JsonToken.Integer)
						));
					}

					var dict = new OrderedULongDictionary();
					dict.AddRange(list);
					return dict;
				}
				default:
					throw new JsonSerializationException("Unexpected token!");
			}
		}

		public override void WriteJson(JsonWriter writer,object value,JsonSerializer serializer)
		{
			var dict = (OrderedULongDictionary)value;
			
			writer.WriteStartObject();
			foreach(var pair in dict) {
				writer.WritePropertyName(pair.Key.ToString());
				writer.WriteValue(pair.Value);
				//writer.WriteToken(JsonToken.PropertyName,pair.Key.ToString());
				//writer.WriteToken(JsonToken.Integer,pair.Value);
				//serializer.Serialize(writer,pair.Value);
			}
			writer.WriteEndObject();
		}
	}
}
