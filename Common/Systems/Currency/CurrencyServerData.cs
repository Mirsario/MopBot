using Discord.WebSocket;
using Newtonsoft.Json;
using MopBot.Collections;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.Currency
{
	public class CurrencyServerData : ServerData
	{
		[JsonProperty] private BotIdCollection<Currency> currencies;
		[JsonIgnore] public BotIdCollection<Currency> Currencies => currencies ??= new BotIdCollection<Currency>();

		public override void Initialize(SocketGuild server) { }
	}
}
