using System;
using Newtonsoft.Json;
using MopBotTwo.Collections;

namespace MopBotTwo.Common.Systems.Currency
{
	[Serializable]
	public class Currency
	{
		public string displayName;
		public string description;
		public string emote;

		[JsonProperty] private OrderedULongDictionary usersWealth; //Not storing in ServerUserData, since we need data sorted.
		[JsonIgnore] public OrderedULongDictionary UsersWealth => usersWealth ?? (usersWealth = new OrderedULongDictionary());

		public ulong GetAmount(ulong userId) => UsersWealth.TryGetValue(userId,out ulong amount) ? amount : 0;

		public string ToString(ulong amount) => $"{emote} **{amount} {StringUtils.ChangeForm(displayName,amount==1)}**";
		public override string ToString() => $"{emote} **{displayName}**";
	}
}
