using Discord;

namespace MopBot.Core.DataStructures
{
	public struct EmoteInfo
	{
		public readonly Emote Emote;
		public readonly Emoji Emoji;

		public bool IsCustom => Emote!=null;

		public EmoteInfo(Emote emote)
		{
			Emote = emote;
			Emoji = null;
		}
		public EmoteInfo(Emoji emoji)
		{
			Emoji = emoji;
			Emote = null;
		}
	}
}
