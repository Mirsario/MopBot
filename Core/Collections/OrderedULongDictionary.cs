using Newtonsoft.Json;

namespace MopBot.Collections
{
	[JsonConverter(typeof(OrderedULongDictionaryConverter))]
	public class OrderedULongDictionary : ValueOrderedDictionary<ulong,ulong> {}
}
