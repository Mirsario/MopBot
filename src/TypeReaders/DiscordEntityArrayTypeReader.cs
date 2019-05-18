using System;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MopBotTwo.TypeReaders
{
	public abstract class DiscordEntityArrayTypeReader<T> : CustomTypeReader where T : class
	{
		public static readonly Regex NumberRegex = new Regex(@"(\d+)",RegexOptions.Compiled);
		
		public abstract Regex ParseRegex { get; }

		public abstract Task<T> GetFromId(ICommandContext context,ulong id);
		public abstract Task<T> GetFromName(ICommandContext context,string name);
		
		public override async Task<TypeReaderResult> ReadAsync(ICommandContext context,string input,IServiceProvider services)
		{
			var matches = ParseRegex.Matches(input);

			if(matches.Count==0) {
				return TypeReaderResult.FromError(CommandError.ParseFailed,$"Parse failure. Expected an array of `{typeof(T).Name}`.");
			}

			var channels = new List<T>();

			foreach(Match match in matches) {
				var groups = match.Groups;
				string idStr = groups[1].Value;
				string idOrNameStr = idStr ?? groups[3].Value;
				 
				T entity = null;

				if(idStr==null) {
					entity = await GetFromName(context,idOrNameStr);
				}else{
					var idMatch = NumberRegex.Match(idStr);
					if(idMatch.Success && ulong.TryParse(idMatch.Groups[1].Value,out ulong id)) {
						entity = await GetFromId(context,id);
					}
				}

				if(entity==null) {
					return TypeReaderResult.FromError(CommandError.ParseFailed,$"Parse failure. Invalid value: `{idOrNameStr}`.");
				}

				channels.Add(entity);
			}

			return TypeReaderResult.FromSuccess(channels.ToArray());
		}
	}
}
