using Discord;

namespace MopBotTwo.Utilities
{
	public static class EmoteUtils
	{
		public static bool TryParse(string input,out IEmote result)
		{
			if(input!=null) {
				if(Emote.TryParse(input,out Emote emote)) {
					result = emote;

					return true;
				}

				if(input.Length==2) {
					result = new Emoji(input);

					return true;
				}
			}

			result = null;

			return false;
		}
		public static IEmote Parse(string input,bool throwOnFail = true)
		{
			if(TryParse(input,out var result)) {
				return result;
			}

			if(!throwOnFail) {
				return null;
			}

			throw new BotError($"Unable to parse emote `{input}`.");
		}
	}
}
