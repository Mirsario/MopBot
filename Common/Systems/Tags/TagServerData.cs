using System.Collections.Generic;
using Discord.WebSocket;
using MopBot.Core.Systems.Memory;

#pragma warning disable 1998

namespace MopBot.Common.Systems.Tags
{
	public class TagServerData : ServerData
	{
		public List<ulong> globalTagGroups = new List<ulong>();

		public override void Initialize(SocketGuild server) { }
	}
}