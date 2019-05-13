using System;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public partial class CommandShopSystem
	{
		[Serializable]
		public class ShopItem
		{
			public string name;
			public CurrencyAmount[] prices;
			public SudoCommand command;

			public ShopItem(string name,CurrencyAmount[] prices,SudoCommand command)
			{
				this.name = name;
				this.prices = prices;
				this.command = command;
			}
		}
	}
}
