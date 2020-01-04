using MopBotTwo.Common.Systems.Currency;
using MopBotTwo.Core.DataStructures;
using System;


namespace MopBotTwo.Common.Systems.CommandShop
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
