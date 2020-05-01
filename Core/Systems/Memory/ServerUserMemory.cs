#pragma warning disable CS1998 //The async method lacks 'await' operator.


namespace MopBot.Core.Systems.Memory
{
	public sealed class ServerUserMemory : MemoryBase<ServerUserData>
	{
		//public SocketGuildUser User => MopBot.client.GetGuild(this.id).GetUser(id);
		//public override object[] DataConstructorArguments => new object[] { Server };

		public override void OnDataCreated(ServerUserData data)
		{
			data.Initialize();
		}
	}
}
