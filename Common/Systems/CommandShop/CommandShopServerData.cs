using System;
using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.CommandShop
{
	public class CommandShopServerData : ServerData
	{
		[JsonProperty] private Dictionary<string, Shop> shops;
		[JsonIgnore] public Dictionary<string, Shop> Shops => shops ??= new Dictionary<string, Shop>(StringComparer.InvariantCultureIgnoreCase);

		public override void Initialize(SocketGuild server) { }
	}
}
