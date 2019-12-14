using Discord.WebSocket;
using Newtonsoft.Json;
using MopBotTwo.Collections;
using MopBotTwo.Core.Systems.Memory;

namespace MopBotTwo.Common.Systems.Currency
{
	public class CurrencyServerData : ServerData
	{
		[JsonProperty] private BotIdCollection<Currency> currencies;
		[JsonIgnore] public BotIdCollection<Currency> Currencies => currencies ?? (currencies = new BotIdCollection<Currency>());

		public override void Initialize(SocketGuild server) { }
	}
}
