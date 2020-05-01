using Discord.Commands;
using Discord.WebSocket;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using MopBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MopBot.Common.Systems.XP
{
	public partial class XPSystem
	{
		[Command("give")]
		[RequirePermission(SpecialPermission.Owner)]
		public async Task GiveXP(SocketGuildUser user,ulong numXP) => await ModifyXP(xp => xp+numXP,user);

		[Command("take")]
		[RequirePermission(SpecialPermission.Owner)]
		public async Task TakeXP(SocketGuildUser user,ulong numXP) => await ModifyXP(xp => xp-numXP,user);

		[Command("set")]
		[RequirePermission(SpecialPermission.Owner)]
		public async Task SetXP(SocketGuildUser user,ulong newXP) => await ModifyXP(xp => newXP,user);

		[Command]
		[Summary("Shows your current XP, level and rank.")]
		[Priority(1)]
		public async Task ShowXPCommand(SocketGuildUser user = null)
		{
			user ??= Context.socketServerUser;

			var serverMemory = MemorySystem.memory[Context.server];
			ulong xp = serverMemory[user].GetData<XPSystem,XPServerUserData>().xp;
			uint level = XPToLevel(xp);

			uint rank = (uint)serverMemory.GetSubMemories<ServerUserMemory>().Keys
				.SelectIgnoreNull(key => {
					var botUser = MopBot.client.GetUser(key);

					if(botUser==null) {
						return null;
					}

					return new KeyValuePair<ulong,ServerUserMemory>?(new KeyValuePair<ulong,ServerUserMemory>(key,serverMemory[botUser]));
				})
				.OrderByDescending(pair => pair?.Value.GetData<XPSystem,XPServerUserData>().xp ?? 0)
				.FirstIndex(pair => pair?.Key==user.Id)+1;

			ulong thisLevelXP = LevelToXP(level);
			ulong nextLevelXP = LevelToXP(level+1);

			var builder = MopBot.GetEmbedBuilder(Context)
				.WithAuthor(user.Username,user.GetAvatarUrl())
				.WithDescription($"**XP: **{xp-thisLevelXP}/{nextLevelXP-thisLevelXP} ({xp}/{nextLevelXP} Total)\r\n**Level: **{level}\r\n**Rank: **#{rank}");

			await Context.socketTextChannel.SendMessageAsync("",embed: builder.Build());
		}

		[Command("leaderboards")]
		[Alias("leaders","levels")]
		[Summary("Shows XP leaderboards.")]
		public async Task ShowLeaderboardsCommand()
		{
			var server = Context.server;
			var serverMemory = MemorySystem.memory[server];

			const int NumShown = 10;

			var leaders = serverMemory
				.GetSubMemories<ServerUserMemory>()
				.Select<KeyValuePair<ulong,ServerUserMemory>,(ulong userId,XPServerUserData xpUserData)>(pair => (pair.Key, pair.Value.GetData<XPSystem,XPServerUserData>()))
				.OrderByDescending(tuple => tuple.xpUserData?.xp ?? 0)
				.Select<(ulong userId, XPServerUserData xpUserData),(SocketGuildUser user, XPServerUserData xpUserData)>(tuple => (server.GetUser(tuple.userId), tuple.xpUserData))
				.Where(tuple => tuple.user!=null && tuple.xpUserData!=null)
				.Take(NumShown);

			var tuples = leaders as (SocketGuildUser user, XPServerUserData xpUserData)[] ?? leaders.ToArray();
			var (user,xpUserData) = tuples.First();

			int i = 1;

			var builder = MopBot.GetEmbedBuilder(Context)
				.WithAuthor($"#{i++} - {user.GetDisplayName()} - {xpUserData.xp} Total XP (Level {XPToLevel(xpUserData.xp)})",user.GetAvatarUrl())
				.WithDescription(string.Join("\r\n",tuples
					.TakeLast(tuples.Length-1)
					.Select(t => $"#{i++} - {t.user.GetDisplayName()} - {t.xpUserData.xp} Total XP (Level {XPToLevel(t.xpUserData.xp)})")
				));

			await Context.socketTextChannel.SendMessageAsync(embed:builder.Build());
		}
	}
}
