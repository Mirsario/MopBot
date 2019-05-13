using System;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public partial class CommandShopSystem
	{
		[Serializable]
		public class Shop
		{
			public string displayName;
			public string description;
			public string thumbnailUrl;
			
			public ShopItem[] items;

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
}
