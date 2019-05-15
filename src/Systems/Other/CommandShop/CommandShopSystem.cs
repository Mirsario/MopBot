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
	[SystemConfiguration(Description = "Lets users trade CurrencySystem currencies for pre-defined sudo command executions in custom shops. Commands will be executed as the user who created the shop item that's 'bought'.")]
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
