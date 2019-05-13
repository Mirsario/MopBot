#pragma warning disable CS1998

namespace MopBotTwo
{
	public class ServerUserData : MemoryDataBase {}
	public class ServerUserMemory : MemoryBase<ServerUserData>
	{
		//public SocketGuildUser User => MopBot.client.GetGuild(id);
		//public override object[] DataConstructorArguments => new object[] { Server };
	}
}
