using System.Linq;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Utilities;

namespace MopBotTwo.Common.Systems.Showcase
{
	public class ShowcaseServerData : ServerData
	{
		public ShowcaseChannel[] showcaseChannels;
		public SpotlightChannel[] spotlightChannels;
		public Dictionary<EmoteType,string> emotes;

		public override void Initialize(SocketGuild server) { }

		public bool ChannelIs<T>(IChannel channel) where T : ChannelInfo
		{
			ulong channelId = channel.Id;
			return (typeof(T)==typeof(ShowcaseChannel) ? showcaseChannels : (ChannelInfo[])spotlightChannels)?.Any(c => c.id==channelId)==true;
		}
		public T GetChannelInfo<T>(IChannel channel,bool throwException = true) where T : ChannelInfo
		{
			ulong channelId = channel.Id;

			var info = (typeof(T)==typeof(ShowcaseChannel) ? showcaseChannels : (ChannelInfo[])spotlightChannels)?.FirstOrDefault(c => c.id==channelId);
			if(throwException && info==null) {
				throw new BotError($"{channel?.Name ?? "NULL"} is not a '{typeof(T).Name}'.");
			}

			return (T)info;
		}
		public bool TryGetChannelInfo<T>(IChannel channel,out T result) where T : ChannelInfo => (result = GetChannelInfo<T>(channel,false))!=null;

		public bool TryGetEmote(EmoteType emoteType,out IEmote emote)
		{
			emote = null;

			return emotes!=null && emotes.TryGetValue(emoteType,out string emoteStr) && EmoteUtils.TryParse(emoteStr,out emote);
		}
		public IEmote GetEmote(EmoteType emoteType,bool throwOnFail = true)
		{
			if(TryGetEmote(emoteType,out var emote)) {
				return emote;
			}

			if(!throwOnFail) {
				return null;
			}

			throw new BotError($"Emote '{emoteType}' hasn't been set or is missing.");
		}

		public void RemoveChannel(ulong id)
		{
			void Loop<T>(ref T[] array) where T : ChannelInfo
			{
				if(array==null) {
					return;
				}

				List<T> list = null;
				int offset = 0;

				for(int i = 0;i<array.Length;i++) {
					var info = array[i];
					if(info.id==id) {
						if(list==null) {
							list = array.ToList();
						}
						list.RemoveAt(i+offset--);
					}
				}

				if(list!=null) {
					array = list.ToArray();
				}
			}

			Loop(ref showcaseChannels);
			Loop(ref spotlightChannels);
		}
	}
}