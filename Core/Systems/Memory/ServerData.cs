using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MopBotTwo.Core.Systems.Memory
{
	public class ServerData : MemoryDataBase
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public bool? isEnabled;

		public virtual void Initialize(SocketGuild server) {}
	}
}