using Discord;

namespace MopBotTwo.Core.Systems.Memory
{
	public sealed class Memory : MemoryBase<GlobalData>
	{
		public ServerMemory this[IGuild server] {
			get => GetSubMemory<ServerMemory>(server.Id);
			set => SetSubMemory(server.Id,value);
		}
		public UserMemory this[IUser user] {
			get => GetSubMemory<UserMemory>(user.Id);
			set => SetSubMemory(user.Id,value);
		}

		public override void Initialize()
		{
			base.Initialize();

			RegisterSubMemory<UserMemory>();
			RegisterSubMemory<ServerMemory>();
		}
		public override void OnDataCreated(GlobalData data)
		{
			data.Initialize();
		}
	}
}