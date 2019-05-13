using Newtonsoft.Json;

namespace MopBotTwo.Collections
{
	[JsonConverter(typeof(OrderedULongDictionaryConverter))]
	public class OrderedULongDictionary : ValueOrderedDictionary<ulong,ulong> {}
}
