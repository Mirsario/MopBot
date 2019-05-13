using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable CS1998

namespace MopBotTwo
{
	public class ServerData : MemoryDataBase
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public bool? isEnabled;

		public virtual void Initialize(SocketGuild server) {}
	}

	public class ServerMemory : MemoryBase<ServerData>
	{
		public SocketGuild Server => MopBot.client.GetGuild(id);

		protected override string Name => Server?.Name;

		public ServerUserMemory this[IUser user] {
			get => GetSubMemory<ServerUserMemory>(user.Id);
			set => SetSubMemory(user.Id,value);
		}

		public override void Initialize()
		{
			base.Initialize();

			RegisterSubMemory<ServerUserMemory>();
		}
		public override void OnDataCreated(ServerData data)
		{
			data.Initialize(Server);
		}
	}
}