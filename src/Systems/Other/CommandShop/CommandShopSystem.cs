using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;

#pragma warning disable CS1998 //This async method lacks 'await' operators

namespace MopBotTwo.Systems
{
	[Group("shop")]
	[Summary("Lets users 'buy' or order all kinds of things for virtual points.")]
	public partial class CommandShopSystem : BotSystem
	{
		public class CommandShopServerData : ServerData
		{
			[JsonProperty] private Dictionary<string,Shop> shops;
			[JsonIgnore] public Dictionary<string,Shop> Shops => shops ?? (shops = new Dictionary<string,Shop>(StringComparer.InvariantCultureIgnoreCase));

			public override void Initialize(SocketGuild server) {}
		}

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,CommandShopServerData>();
		}
	}
}
