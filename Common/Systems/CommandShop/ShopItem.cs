using MopBot.Common.Systems.Currency;
using MopBot.Core.DataStructures;
using System;

namespace MopBot.Common.Systems.CommandShop
{
	[Serializable]
	public class ShopItem
	{
		public string name;
		public CurrencyAmount[] prices;
		public SudoCommand command;

		public ShopItem(string name, CurrencyAmount[] prices, SudoCommand command)
		{
			this.name = name;
			this.prices = prices;
			this.command = command;
		}
	}
}
