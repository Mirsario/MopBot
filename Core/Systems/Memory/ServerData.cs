using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace MopBot.Core.Systems.Memory
{
	[Serializable]
	public class ServerData : MemoryDataBase
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public bool? isEnabled;

		public virtual void Initialize(SocketGuild server) { }
	}
}