using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopBotTwo.Utilities;

namespace MopBotTwo.Core.TypeReaders
{
	public class IEmoteTypeReader : CustomTypeReader
	{
		public override Type[] Types => new[] { typeof(IEmote) };

		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context,string input,IServiceProvider services)
		{
			//TODO: This causes a null reference exception somewhere else
			/*if(input.ToLower()=="null") {
				return TypeReaderResult.FromSuccess(null);
			}*/

			if(EmoteUtils.TryParse(input,out var result)) {
				return TypeReaderResult.FromSuccess(result);
			}

			return TypeReaderResult.FromError(CommandError.ParseFailed,$"Unable to parse emote `{input}`.");
		}
	}
}
