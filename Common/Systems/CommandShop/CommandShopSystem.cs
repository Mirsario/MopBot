using System.Text.RegularExpressions;
using Discord.Commands;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.CommandShop
{
	[Group("shop")]
	[Summary("Lets users 'buy' or order all kinds of things for virtual points.")]
	[SystemConfiguration(Description = "Lets users trade CurrencySystem currencies for pre-defined sudo command executions in custom shops. Commands will be executed as the user who created the shop item that's 'bought'.")]
	public partial class CommandShopSystem : BotSystem
	{
		public override void RegisterDataTypes() => RegisterDataType<ServerMemory, CommandShopServerData>();
	}
}
