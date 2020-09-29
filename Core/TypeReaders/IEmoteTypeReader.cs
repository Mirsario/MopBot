using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopBot.Utilities;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.TypeReaders
{
	public class IEmoteTypeReader : CustomTypeReader
	{
		public override Type[] Types => new[] { typeof(IEmote) };

		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			if(EmoteUtils.TryParse(input, out var result)) {
				return TypeReaderResult.FromSuccess(result);
			}

			return TypeReaderResult.FromError(CommandError.ParseFailed, $"Unable to parse emote `{input}`.");
		}
	}
}
