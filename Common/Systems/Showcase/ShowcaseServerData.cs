using System.Linq;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MopBot.Core.Systems.Memory;
using MopBot.Utilities;

namespace MopBot.Common.Systems.Showcase
{
	public class ShowcaseServerData : ServerData
	{
		public List<ShowcaseChannel> showcaseChannels;
		public List<SpotlightChannel> spotlightChannels;
		public Dictionary<EmoteType, string> emotes;

		public override void Initialize(SocketGuild server) { }

		public bool ChannelIs<T>(IChannel channel) where T : ChannelInfo
		{
			ulong channelId = channel.Id;

			return (typeof(T) == typeof(ShowcaseChannel) ? showcaseChannels : (IEnumerable<ChannelInfo>)spotlightChannels)?.Any(c => c.id == channelId) == true;
		}
		public T GetChannelInfo<T>(IChannel channel, bool throwException = true) where T : ChannelInfo
		{
			ulong channelId = channel.Id;
			var info = (typeof(T) == typeof(ShowcaseChannel) ? showcaseChannels : (IEnumerable<ChannelInfo>)spotlightChannels)?.FirstOrDefault(c => c.id == channelId);

			if(throwException && info == null) {
				throw new BotError($"{channel?.Name ?? "NULL"} is not a '{typeof(T).Name}'.");
			}

			return (T)info;
		}
		public bool TryGetChannelInfo<T>(IChannel channel, out T result) where T : ChannelInfo
			=> (result = GetChannelInfo<T>(channel, false)) != null;
		public void RemoveChannel(ulong id)
		{
			showcaseChannels.RemoveAll(c => c.id == id);
			spotlightChannels.RemoveAll(c => c.id == id);
		}
		public bool TryGetEmote(EmoteType emoteType, out IEmote emote)
		{
			emote = null;

			return emotes != null && emotes.TryGetValue(emoteType, out string emoteStr) && EmoteUtils.TryParse(emoteStr, out emote);
		}
		public IEmote GetEmote(EmoteType emoteType, bool throwOnFail = true)
		{
			if(TryGetEmote(emoteType, out var emote)) {
				return emote;
			}

			if(!throwOnFail) {
				return null;
			}

			throw new BotError($"Emote '{emoteType}' hasn't been set or is missing.");
		}
	}
}
