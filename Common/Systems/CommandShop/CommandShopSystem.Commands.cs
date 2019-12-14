using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using MopBotTwo.Common.Systems.Currency;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Extensions;


namespace MopBotTwo.Common.Systems.CommandShop
{
	public partial class CommandShopSystem
	{
		[Command("setup")]
		[RequirePermission(SpecialPermission.Owner,"commandshop.manage")]
		public async Task SetupShopCommand(string shopId,string displayName,string description = null,string thumbnailUrl = null)
		{
			StringUtils.CheckAndLowerStringId(ref shopId);
			MopBot.CheckForNullOrEmpty(displayName,nameof(displayName));
			
			var context = Context;
			var cmdShopServerData = context.server.GetMemory().GetData<CommandShopSystem,CommandShopServerData>();

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				shops[shopId] = shop = new Shop();
			}

			shop.displayName = displayName;
			shop.description = description;
			shop.thumbnailUrl = thumbnailUrl;
		}
		[Command("rename")]
		[RequirePermission(SpecialPermission.Owner,"commandshop.manage")]
		public async Task RenameShopCommand(string shopId,string newShopId)
		{
			StringUtils.CheckAndLowerStringId(ref newShopId);

			var context = Context;
			var cmdShopServerData = context.server.GetMemory().GetData<CommandShopSystem,CommandShopServerData>();

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				throw new BotError($"No such shop: `{shopId}`.");
			}

			shops.Remove(shopId);
			shops[newShopId] = shop;
		}

		[Command("additem")]
		[RequirePermission(SpecialPermission.Owner,"commandshop.manage")]
		public async Task AddItemCommand(string shopId,string itemName,string itemPrice,string itemCommand)
		{
			MopBot.CheckForNullOrEmpty(itemName,nameof(itemName));
			MopBot.CheckForNullOrEmpty(itemCommand,nameof(itemCommand));

			var context = Context;
			var serverMemory = context.server.GetMemory();
			var cmdShopServerData = serverMemory.GetData<CommandShopSystem,CommandShopServerData>();

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				throw new BotError($"No such shop: `{shopId}`.");
			}

			var item = new ShopItem(itemName,CurrencyAmount.ParseMultiple(itemPrice,serverMemory),new SudoCommand(itemCommand,Context.user.Id));
			
			lock(shop) {
				shop.Items = shop.Items?.Append(item)?.ToArray() ?? new[] { item };
			}
		}
		[Command("removeitem")] [Alias("deleteitem","delitem","rmitem")]
		[RequirePermission(SpecialPermission.Owner,"commandshop.manage")]
		public async Task RemoveItemCommand(string shopId,int itemId)
		{
			var context = Context;
			var cmdShopServerData = context.server.GetMemory().GetData<CommandShopSystem,CommandShopServerData>();

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				throw new BotError($"No such shop: `{shopId}`.");
			}

			itemId--;

			lock(shop) {
				if(shop.Items==null || itemId<0 || itemId>=shop.Items.Length) {
					throw new BotError($"Invalid item id.");
				}
				shop.Items = shop.Items.Length<=1 ? null : shop.Items.ExceptIndex(itemId).ToArray();
			}
		}
		
		[Command("buy")]
		public async Task BuyItemCommand(string shopId,int itemId)
		{
			var context = Context;
			var userId = context.socketServerUser.Id;
			var server = context.server;
			var memory = server.GetMemory();
			var cmdShopServerData = memory.GetData<CommandShopSystem,CommandShopServerData>();
			//var currencyServerUserData = memory[context.user].GetData<CurrencySystem,CurrencyServerUserData>();
			var currencies = memory.GetData<CurrencySystem,CurrencyServerData>().Currencies;

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				throw new BotError($"No such shop: `{shopId}`.");
			}

			itemId--;

			if(shop.Items==null || itemId<0 || itemId>=shop.Items.Length) {
				throw new BotError($"Invalid item id.");
			}

			string error = null;
			//Any exceptions inside this delegate will remove the item from the shop. 
			await shop.SafeItemAction(itemId,async item => {
				//if(item.prices.TryGetFirst(p => currencyServerUserData[p.currency]<p.amount,out var price)) {
				if(item.prices.TryGetFirst(p => currencies[p.currency].GetAmount(userId)<p.amount,out var p)) {
					//error = $"Not enough {currencies[price.currency]}. You need **{price.amount}**, but you only have **{currencyServerUserData[price.currency]}**.";
					var currency = currencies[p.currency];
					error = $"Not enough {currency}. You need **{p.amount}**, but you only have **{currency.GetAmount(userId)}**.";
					return;
				}
				
				await item.command.Execute(context,(sc,cmd) => cmd.Replace("{item}",item.name,sc));
				
				//Take the moneys
				CurrencyAmount.TakeFromUser(item.prices,Context.socketServerUser);
			});

			if(error!=null) {
				throw new BotError(error);
			}
		}
		[Command("show")] [Alias("list")]
		public async Task ShowShopCommand(string shopId)
		{
			var context = Context;
			var server = context.server;
			var memory = server.GetMemory();
			var cmdShopServerData = memory.GetData<CommandShopSystem,CommandShopServerData>();
			var currencyServerData = memory.GetData<CurrencySystem,CurrencyServerData>();

			var shops = cmdShopServerData.Shops;
			if(!shops.TryGetValue(shopId,out Shop shop)) {
				throw new BotError($"No such shop: `{shopId}`.");
			}
			
			var embedBuilder = MopBot.GetEmbedBuilder(server)
				.WithTitle(shop.displayName)
				.WithDescription(shop.description)
				.WithThumbnailUrl(shop.thumbnailUrl)
				.WithFooter(@"Use ""!shop buy rewards <item number>"" to buy stuff!");
			
			var items = shop.Items;
			if(items!=null) {
				for(int i = 0;i<items.Length;i++) {
					ShopItem item = items[i];
					await shop.SafeItemAction(i,throwError:false,action: async item => {
						embedBuilder.AddField($"#{i+1} - {item.name}",$"Costs {string.Join(", ",item.prices.Select(price => currencyServerData.Currencies[price.currency].ToString(price.amount)))}");
					});
				}
			}

			await context.ReplyAsync(embedBuilder.Build());
		}
	}
}
