using Discord;
using Discord.WebSocket;

namespace MopBot.Core.Systems.Memory
{
	public sealed class ServerMemory : MemoryBase<ServerData>
	{
		public SocketGuild Server => MopBot.client.GetGuild(id);

		protected override string Name => Server?.Name;

		public ServerUserMemory this[IUser user] {
			get => GetSubMemory<ServerUserMemory>(user.Id);
			set => SetSubMemory(user.Id, value);
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
