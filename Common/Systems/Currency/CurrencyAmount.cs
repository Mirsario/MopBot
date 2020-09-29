using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Discord.WebSocket;
using MopBot.Extensions;
using MopBot.Collections;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.Currency
{
	[Serializable]
	public struct CurrencyAmount
	{
		private static readonly Regex ParseRegex = new Regex(@"(\d+)\s+(\w+)(\s*,\s*)?", RegexOptions.Compiled);

		public ulong currency;
		public ulong amount;

		public CurrencyAmount(ulong currency, ulong amount)
		{
			this.currency = currency;
			this.amount = amount;
		}

		public static bool TryGiveToUser(ref CurrencyAmount[] currencyAmounts, SocketGuildUser user, out string givenString)
			=> TryModifyForUser(ref currencyAmounts, user, out givenString, (currency, c, value) => Utils.SafeAdd(value, c.amount));
		public static bool TryTakeFromUser(ref CurrencyAmount[] currencyAmounts, SocketGuildUser user, out string takenString)
			=> TryModifyForUser(ref currencyAmounts, user, out takenString, (currency, c, value) => c.amount >= value ? 0 : value - c.amount);

		public static void GiveToUser(CurrencyAmount[] currencyAmounts, SocketGuildUser user)
			=> ModifyForUser(currencyAmounts, user, (currency, c, value) => Utils.SafeAdd(value, c.amount));
		public static void TakeFromUser(CurrencyAmount[] currencyAmounts, SocketGuildUser user)
			=> ModifyForUser(currencyAmounts, user, (currency, c, value) => c.amount >= value ? 0 : value - c.amount);

		private static bool TryModifyForUser(ref CurrencyAmount[] currencyAmounts, SocketGuildUser user, out string givenString, Func<Currency, CurrencyAmount, ulong, ulong> func)
		{
			if(currencyAmounts == null) {
				givenString = null;

				return false;
			}

			StringBuilder sb = null;
			List<CurrencyAmount> list = new List<CurrencyAmount>();

			ModifyForUser(currencyAmounts, user, (currency, c, value) => {
				list.Add(c);

				if(sb == null) {
					sb = new StringBuilder();
				} else {
					sb.Append(", ");
				}

				sb.Append(currency.ToString(c.amount));

				return func(currency, c, value);
			});

			if(list.Count != currencyAmounts.Length) {
				currencyAmounts = list.ToArray();
			}

			givenString = sb?.ToString();

			return currencyAmounts.Length > 0;
		}
		private static void ModifyForUser(CurrencyAmount[] currencyAmounts, SocketGuildUser user, Func<Currency, CurrencyAmount, ulong, ulong> func)
		{
			if(currencyAmounts == null) {
				throw new BotError(new ArgumentNullException(nameof(currencyAmounts)));
			}

			var userId = user.Id;
			var currencies = user.Guild.GetMemory().GetData<CurrencySystem, CurrencyServerData>().Currencies;

			for(int i = 0; i < currencyAmounts.Length; i++) {
				var c = currencyAmounts[i];
				var currency = currencies[c.currency];
				var wealth = currency.UsersWealth;

				ulong newAmount = func(currency, c, wealth.TryGetValue(userId, out ulong value) ? value : 0);

				if(newAmount == 0) {
					wealth.Remove(newAmount);
				} else {
					wealth[userId] = newAmount;
				}
			}
		}

		public static CurrencyAmount[] ParseMultiple(string str, ServerMemory serverMemory) => ParseMultiple(str, serverMemory.GetData<CurrencySystem, CurrencyServerData>().Currencies);
		public static CurrencyAmount[] ParseMultiple(string str, BotIdCollection<Currency> currencies)
		{
			MatchCollection matches;

			if(!string.IsNullOrWhiteSpace(str) || str.Length <= 2) {
				StringUtils.RemoveQuotemarks(ref str);

				matches = ParseRegex.Matches(str);
			} else {
				matches = null;
			}

			//Entire input must be captured by regex
			if(matches == null || matches.Count == 0 || matches.Sum(m => m.Length) != str.Length) {
				throw new BotError($"Unable to parse `{str ?? "Null"}`:\r\nExpected it to be in `amount currency, ...` format.\r\n**Example:** `102 diamonds, 1 triviapoints`.");
			};

			return matches.Select(m => {
				string name = m.Groups[2].Value;
				int length = name.Length;

				if(!currencies.TryGetIdFromName(name, out ulong id)) {
					//Accepts both plural and singular names
					if(length == 1 || !currencies.TryGetIdFromName(name.EndsWith('s') ? name.Remove(length - 1, 1) : name + "s", out id)) {
						throw new BotError($"Unknown currency: {name}");
					}
				}

				return new CurrencyAmount(id, ulong.Parse(m.Groups[1].Value));
			}).ToArray();
		}
	}
}
