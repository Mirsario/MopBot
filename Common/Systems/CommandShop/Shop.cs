using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MopBot.Common.Systems.CommandShop
{
	[Serializable]
	public class Shop
	{
		public string displayName;
		public string description;
		public string thumbnailUrl;
		
		[JsonProperty] private ShopItem[] items;
		[JsonIgnore] public ShopItem[] Items {
			get => items;
			set => items = value.OrderByDescending(item => item.prices.Sum(p => (long)p.amount)).ToArray();
		}

		public async Task SafeItemAction(int index,Func<ShopItem,Task> action,bool throwError = true)
		{
			try {
				await action(items[index]);
			}
			catch {
				ArrayUtils.RemoveAt(ref items,index);

				if(throwError) {
					throw new BotError("There's been something wrong with that item, and so it has been removed.");
				}
			}
		}
	}
}
