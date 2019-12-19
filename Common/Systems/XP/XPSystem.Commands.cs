using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MopBotTwo.Common.Systems.XP
{
	public partial class XPSystem
	{
		[Command("give")]
		[RequirePermission(SpecialPermission.Owner)]
		public static async Task GiveXP(ulong numXP,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel) => await ModifyXP(xp => xp+numXP,user,server);

		[Command("take")]
		[RequirePermission(SpecialPermission.Owner)]
		public static async Task TakeXP(ulong numXP,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel) => await ModifyXP(xp => xp-numXP,user,server);

		[Command("set")]
		[RequirePermission(SpecialPermission.Owner)]
		public static async Task SetXP(ulong newXP,SocketGuildUser user,SocketGuild server) => await ModifyXP(xp => newXP,user,server);

		[Command]
		[Summary("Shows your current XP, level and rank.")]
		public async Task ShowXPCommand(SocketGuildUser user = null)
		{
			user ??= Context.socketServerUser;

			var serverMemory = MemorySystem.memory[Context.server];
			ulong xp = serverMemory[user].GetData<XPSystem,XPServerUserData>().xp;
			uint level = XPToLevel(xp);

			uint rank = (uint)serverMemory.GetSubMemories<ServerUserMemory>().Keys
				.SelectIgnoreNull(key => {
					var u = MopBot.client.GetUser(key);
					if(u==null) {
						return null;
					}
					return new KeyValuePair<ulong,ServerUserMemory>?(new KeyValuePair<ulong,ServerUserMemory>(key,serverMemory[u]));
				})
				.OrderByDescending(pair => pair?.Value.GetData<XPSystem,XPServerUserData>().xp ?? 0)
				.FirstIndex(pair => pair?.Key==user.Id)+1;

			ulong thisLevelXP = LevelToXP(level);
			ulong nextLevelXP = LevelToXP(level+1);

			var builder = MopBot.GetEmbedBuilder(Context)
				.WithAuthor(user.Username,user.GetAvatarUrl())
				.WithDescription($"**XP: **{xp-thisLevelXP}/{nextLevelXP-thisLevelXP} ({xp}/{nextLevelXP} Total)\n**Level: **{level}\n**Rank: **#{rank}");

			await Context.socketTextChannel.SendMessageAsync("",embed: builder.Build());
		}

		[Command("leaderboards")]
		[Alias("leaders","levels")]
		[Summary("Shows XP leaderboards.")]
		public async Task ShowLeaderboardsCommand()
		{
			try {
				var server = Context.server;
				var serverMemory = MemorySystem.memory[server];

				const int NumShown = 10;

				var leaders =
					serverMemory.GetSubMemories<ServerUserMemory>()
					.Select<KeyValuePair<ulong,ServerUserMemory>,(ulong userId,XPServerUserData xpUserData)>(pair => (pair.Key, pair.Value.GetData<XPSystem,XPServerUserData>()))
					.OrderByDescending(tuple => tuple.xpUserData?.xp ?? 0)
					.Select<(ulong userId, XPServerUserData xpUserData),(SocketGuildUser user, XPServerUserData xpUserData)>(tuple => (server.GetUser(tuple.userId), tuple.xpUserData))
					.Where(tuple => tuple.user!=null && tuple.xpUserData!=null)
					.Take(NumShown);

				var tuples = leaders as (SocketGuildUser user, XPServerUserData xpUserData)[] ?? leaders.ToArray();

				var (user,xpUserData) = tuples.First();

				int i = 1;

				var builder = MopBot.GetEmbedBuilder(Context)
					.WithAuthor($"#{i++} - {user.Name()} - {xpUserData.xp} Total XP (Level {XPToLevel(xpUserData.xp)})",user.GetAvatarUrl())
					.WithDescription(string.Join("\n",tuples.TakeLast(tuples.Length-1).Select(t => $"#{i++} - {t.user.Name()} - {t.xpUserData.xp} Total XP (Level {XPToLevel(t.xpUserData.xp)})")));

				await Context.socketTextChannel.SendMessageAsync(embed: builder.Build());
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
			}
		}
	}
}
