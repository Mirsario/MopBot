using System;
using Discord;
using Discord.WebSocket;
using MopBot.Core.Systems.Commands;
using Color = MopBot.Core.DataStructures.Color;
using MopBot.Extensions;

namespace MopBot.Common.Systems.Showcase
{
	public class EmbedCache
	{
		public string authorName;
		public string imageUrl;
		public string description;
		public Color? color;
		public DateTime cacheDate;

		public EmbedCache(SocketGuild server)
		{
			cacheDate = DateTime.Now;
			color = server.GetMemory().GetData<CommandSystem, CommandServerData>().embedColor.Value;
		}

		public EmbedBuilder ToBuilder(SocketGuild server)
		{
			var builder = MopBot.GetEmbedBuilder(server);

			if (authorName != null) {
				builder.WithAuthor(authorName, imageUrl);
			}

			if (description != null) {
				builder.WithDescription(description);
			}

			return builder;
		}
	}
}
