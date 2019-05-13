using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998 //This async method lacks 'await' operators

namespace MopBotTwo.Systems
{
	[Group("currency")] [Alias("currencies","money","cash","coins","points","score")]
	[Summary("Group for anything related to currencies and points.")]
	public partial class CurrencySystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,CurrencyServerData>();
			//RegisterDataType<ServerUserMemory,CurrencyServerUserData>();
		}
		
		[Priority(-1)]
		[Command]
		public async Task ShowCurrenciesCommand()
		{
			var context = Context;
			var userId = context.user.Id;
			var currencyServerData = context.server.GetMemory().GetData<CurrencySystem,CurrencyServerData>();
			//var currencyUserData = context.socketServerUser.GetMemory().GetData<CurrencySystem,CurrencyServerUserData>();

			var currencies = currencyServerData.Currencies;
			if(currencies.Count==0) {
				throw new BotError("There are currently no currencies on this server.");
			}

			var builder = MopBot.GetEmbedBuilder(context)
				.WithAuthor($"{context.user.Name()}'s Coins & Points",context.user.GetAvatarUrl());

			foreach(var nameIdValue in currencies) {
				var id = nameIdValue.id;
				var currency = nameIdValue.value;
				ulong amount = currency.GetAmount(userId); //currencyUserData[id];

				builder.AddField(
					$"{currency.emote ?? "💰"} - **{amount} {StringUtils.ChangeForm(currency.displayName,amount==1)}**",
					$"{currency.description ?? "No description"}\r\nId: `{nameIdValue.name}`"
				);
			}

			await context.ReplyAsync(embed:builder.Build());
		}

		[Command("leaders")] [Alias("leaderboard","leaderboards","top")]
		public async Task ShowLeadersCommand(string currencyId)
		{
			var context = Context;
			var server = context.server;
			var currencyServerData = server.GetMemory().GetData<CurrencySystem,CurrencyServerData>();

			var currencies = currencyServerData.Currencies;
			if(currencies.Count==0) {
				throw new BotError("There are currently no currencies on this server.");
			}

			var currency = currencies[currencyId];

			const int NumShown = 10;
			const string UnknownUser = "Unknown User";

			var top = currency.UsersWealth.Take(NumShown).ToArray();
			if(!top.TryGetFirst(out var first)) {
				throw new BotError("There are no ranked users with that currency as of right now.");
			}
			var firstUser = server.GetUser(first.Key);

			string GetLine(bool useBold,int number,ulong userId,ulong amount,SocketGuildUser user = null)
			{
				string b = useBold ? "**" : null;
				return $"{(number<=1 ? null : $"{b}#{number}{b} - ")}{b}{amount}{b} - {(user ?? server.GetUser(userId))?.Name() ?? $"{UnknownUser} ({userId})"}";
			}

			int i = 1;
			var builder = MopBot.GetEmbedBuilder(context)
				.WithAuthor(GetLine(false,i++,first.Key,first.Value,firstUser),firstUser?.GetAvatarUrl())
				.WithFooter($"Showing {NumShown} users with most '{currency.displayName}'.");

			if(top.Length>1) {
				builder.WithDescription(string.Join("\r\n",top.TakeLast(top.Length-1).Select(p => GetLine(true,i++,p.Key,p.Value))));
			}

			await context.ReplyAsync(embed:builder.Build());
		}

		[Command("setup")]
		[RequirePermission("currency.manage")]
		public async Task SetupCurrencyCommand(string currencyId,string displayName,string description,string emote)
		{
			var context = Context;
			var currencyServerData = context.server.GetMemory().GetData<CurrencySystem,CurrencyServerData>();

			var currencies = currencyServerData.Currencies;
			if(!currencies.TryGetValue(currencyId,out Currency currency,out _)) {
				currencies.Add(currencyId,currency = new Currency());
			}

			currency.displayName = displayName;
			currency.description = description;
			currency.emote = emote;
		}
		[Command("rename")]
		[RequirePermission("currency.manage")]
		public async Task RenameCurrencyCommand(string currencyId,string newCurrencyId)
		{
			var context = Context;
			var currencyServerData = context.server.GetMemory().GetData<CurrencySystem,CurrencyServerData>();

			currencyServerData.Currencies.Rename(currencyId,newCurrencyId);
		}

		[Command("give")]
		[RequirePermission("currency.admin")]
		public async Task GiveCurrencyAdminCommand(SocketGuildUser user,[Remainder]string amountCurrencyPairs)
		{
			var context = Context;
			CurrencyAmount.GiveToUser(CurrencyAmount.ParseMultiple(amountCurrencyPairs,context.server.GetMemory()),user);
		}
		[Command("take")]
		[RequirePermission("currency.admin")]
		public async Task TakeCurrencyAdminCommand(SocketGuildUser user,[Remainder]string amountCurrencyPairs)
		{
			var context = Context;
			CurrencyAmount.TakeFromUser(CurrencyAmount.ParseMultiple(amountCurrencyPairs,context.server.GetMemory()),user);
		}

		#region Tests
		[Command("testfillmemory")]
		[RequirePermission]
		public async Task TestFillMemoryCommand(int amount)
		{
			var context = Context;
			var serverMemory = context.server.GetMemory();
			var currencies = serverMemory.GetData<CurrencySystem,CurrencyServerData>().Currencies;

			KeyValuePair<ulong,ulong>[] pairList = new KeyValuePair<ulong,ulong>[amount];

			foreach(var currency in currencies) {
				var wealth = currency.value.UsersWealth;
				
				for(int i = 0;i<amount;i++) {
					pairList[i] = new KeyValuePair<ulong,ulong>(Utils.GetRandomULong(),(ulong)MopBot.random.Next(1000));
				}

				wealth.AddRange(pairList);
			}
		}
		[Command("testclearmemory")]
		[RequirePermission]
		public async Task TestClearMemoryCommand()
		{
			var context = Context;
			var serverMemory = context.server.GetMemory();
			var currencies = serverMemory.GetData<CurrencySystem,CurrencyServerData>().Currencies;

			foreach(var currency in currencies) {
				currency.value.UsersWealth.Clear();
			}
		}
		#endregion
	}
}
