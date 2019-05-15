using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[SystemConfiguration(Description = "A small XP/leveling system.")]
	public class XPSystem : BotSystem
	{
		public class XPServerUserData : ServerUserData
		{
			public DateTime lastXPReceive;
			public ulong xp;
		}
		public class XPServerData : ServerData
		{
			public bool? onlyAnnounceRoleRewards;
			public ulong? xpReceiveDelay;
			public Dictionary<uint,ulong[]> levelRewards;

			public override void Initialize(SocketGuild server)
			{
				xpReceiveDelay = (ulong?)300;
				levelRewards = new Dictionary<uint,ulong[]>();
				onlyAnnounceRoleRewards = true;
			}
		}
		
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerUserMemory,XPServerUserData>();
			RegisterDataType<ServerMemory,XPServerData>();
		}

		public override async Task Initialize() {}
		public override async Task OnMessageReceived(MessageExt message)
		{
			var server = message.server;
			if(server==null || message.isCommand || message.socketServerUser.IsBot) {
				return;
			}
			var serverMemory = MemorySystem.memory[server];
			var user = message.socketServerUser;
			var xpUserData = serverMemory[user].GetData<XPSystem,XPServerUserData>();
			var now = DateTime.Now;
			//if((now-xpUserData.lastXPReceive).TotalSeconds>=serverMemory.GetData<XPSystem,XPServerData>().xpReceiveDelay) {
			xpUserData.lastXPReceive = now;
			ulong xp = GetMessageXP(message);
			await GiveXP(xp,user,server,message.socketServerChannel);
			//}
		}
		public override async Task OnMessageDeleted(MessageExt message)
		{
			var server = message.server;
			if(server==null || message.isCommand || message.socketServerUser.IsBot) {
				return;
			}
			var user = message.socketServerUser;
			ulong xp = GetMessageXP(message);
			await TakeXP(xp,user,server,message.socketServerChannel);
		}

		#region ActionFunctions
		[RequirePermission]
		[Command("xp give")]
		public static async Task GiveXP(ulong numXP,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel,SocketMessage message = null) => await ModifyXP(xp => xp+numXP,user,server,channel);
		[RequirePermission]
		[Command("xp take")]
		public static async Task TakeXP(ulong numXP,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel,SocketMessage message = null) => await ModifyXP(xp => xp-numXP,user,server,channel);
		[RequirePermission]
		[Command("xp set")]
		public static async Task SetXP(ulong newXP,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel,SocketMessage message = null) => await ModifyXP(xp => newXP,user,server,channel);
		private static async Task ModifyXP(Func<ulong,ulong> xpModifier,SocketGuildUser user,SocketGuild server,SocketGuildChannel channel,SocketMessage message = null)
		{
			var serverMemory = MemorySystem.memory[server];
			var userMemory = serverMemory[user];
			var xpUserData = userMemory.GetData<XPSystem,XPServerUserData>();
			uint prevLvl = XPToLevel(xpUserData.xp);
			xpUserData.xp = xpModifier(xpUserData.xp);
			uint newLvl = XPToLevel(xpUserData.xp);
			if(message!=null) {
				async Task AddMultipleReactions(IEnumerable<string> emotes)
				{
					foreach(var emote in emotes) {
						await message.AddReactionAsync(server,server.GetEmote(emote));
					}
				}
				if(newLvl>prevLvl) {
					try {
						await AddMultipleReactions(new[] {
							"regional_indicator_l",
							"regional_indicator_e",
							"regional_indicator_v",
							"regional_indicator_e",
							"regional_indicator_l",
							"regional_indicator_u",
							"regional_indicator_p",
							"star",
						});
					}
					catch(Exception e) {
						await MopBot.HandleException(e);
					}
				} else if(newLvl<prevLvl) {}
				/*var mentionChannel = (ITextChannel)(serverMemory.GetData<ChannelSystem,ChannelServerData>().GetChannelByRole(ChannelRole.BotArea) ?? channel);
				string text=		$"{user.Name()} has just reached level {newLvl}! :star:";

				var xpServerData = serverMemory.GetData<XPSystem,XPServerData>();
				if(xpServerData.levelRewards.TryGetValue(newLvl,out ulong[] roleIds)) {
					//var oldAccessLevel = user.GetAccessLevel();
					//var oldCommandList = CommandService.commands.Where(h => oldAccessLevel>=h.minAccessLevel);
					//var roles = roleIds.Select(id => server.GetRole(id));
					//text+=					$"\nThe following roles are now available to them:```{string.Join("\n",roles.Select(role => role.Name))}```";
					//await user.AddRolesAsync(roles);
					//var newAccessLevel = user.GetAccessLevel(roleIds);
					//var newCommandList = CommandService.commands.Where(h => newAccessLevel>=h.minAccessLevel);
					//var newCommandsOnly = newCommandList.Where(h => !oldCommandList.Contains(h)).ToArray();
					//if(newCommandsOnly.Length>0) {
					//	text+=				$"\nThe following commands are now available to them:```{string.Join("\n",newCommandsOnly.Select(h => $"{string.Join("/",h.aliases)}-{h.description}"))}```";
					//}
				}
				await mentionChannel.SendMessageAsync(text);*/
			}
		}
		#endregion

		#region Commands
		[Summary("Shows your current XP, level and rank.")]
		[Command("xp")]
		[Alias("rank","level")]
		public async Task ShowXPCommand(SocketGuildUser user = null)
		{
			if(user==null) {
				user = Context.socketServerUser;
			}
			var serverMemory = MemorySystem.memory[Context.server];
			ulong xp = serverMemory[user].GetData<XPSystem,XPServerUserData>().xp;
			uint level = XPToLevel(xp);

			uint rank = (uint)serverMemory.GetSubMemories<ServerUserMemory>().Keys.SelectIgnoreNull(key => {
				var u = MopBot.client.GetUser(key);
				if(u==null) {
					return null;
				}
				return new KeyValuePair<ulong,ServerUserMemory>?(new KeyValuePair<ulong,ServerUserMemory>(key,serverMemory[u]));
			}).OrderByDescending(pair => pair?.Value.GetData<XPSystem,XPServerUserData>().xp ?? 0).FirstIndex(pair => pair?.Key==user.Id)+1;

			ulong thisLevelXP = LevelToXP(level);
			ulong nextLevelXP = LevelToXP(level+1);

			var builder = MopBot.GetEmbedBuilder(Context);
			builder.WithAuthor(user.Username,user.GetAvatarUrl());
			builder.WithDescription($"**XP: **{xp-thisLevelXP}/{nextLevelXP-thisLevelXP} ({xp}/{nextLevelXP} Total)\n**Level: **{level}\n**Rank: **#{rank}");
			await Context.socketTextChannel.SendMessageAsync("",embed:builder.Build());
		}

		[Summary("Shows XP leaderboards.")]
		[Command("leaderboards")]
		[Alias("leaders","levels")]
		public async Task ShowLeaderboardsCommand()
		{
			try {
				var server = Context.server;
				var serverMemory = MemorySystem.memory[server];

				const int numShown = 10;
				var leaders =
					serverMemory.GetSubMemories<ServerUserMemory>()
					.Select<KeyValuePair<ulong,ServerUserMemory>,(ulong userId,XPServerUserData xpUserData)>(
						pair => (pair.Key,pair.Value.GetData<XPSystem,XPServerUserData>())
					).OrderByDescending(tuple => tuple.xpUserData?.xp ?? 0)
					.Select<(ulong userId,XPServerUserData xpUserData),(SocketGuildUser user,XPServerUserData xpUserData)>(tuple => (server.GetUser(tuple.userId),tuple.xpUserData))
					.Where(tuple => tuple.user!=null && tuple.xpUserData!=null)
					.Take(numShown);
				var tuples = leaders as (SocketGuildUser user,XPServerUserData xpUserData)[] ?? leaders.ToArray();
				var (user,xpUserData) = tuples.First();

				int i = 1;
				var builder = MopBot.GetEmbedBuilder(Context);
				builder.WithAuthor($"#{i++} - {user.Name()} - {xpUserData.xp} Total XP (Level {XPToLevel(xpUserData.xp)})",user.GetAvatarUrl());
				builder.WithDescription(string.Join("\n",tuples.TakeLast(tuples.Length-1).Select(t => $"#{i++} - {t.user.Name()} - {t.xpUserData.xp} Total XP (Level {XPToLevel(t.xpUserData.xp)})")));
				await Context.socketTextChannel.SendMessageAsync(embed:builder.Build());
			}
			catch(Exception e) {
				await MopBot.HandleException(e);
			}
		}
		#endregion

		#region Functions
		public static ulong GetMessageXP(MessageExt message)
		{
			ulong xp = (ulong)Math.Min(1,Math.Max(10,Regex.Matches(message.content,@"\w+").Count/5));
			string text = message.content.ToLower() ?? "";
			if(text.Contains("welcome") || text.Contains("hello") || text.Contains("hi") || text.Contains("hey")) {
				xp += 5;
			} else if(message.message.Attachments.Any(a => a.Width>=256 && a.Height>=256) || message.message.Embeds.Any(e => e.Type==EmbedType.Image || e.Type==EmbedType.Video)) {
				xp += 5;
			}
			return xp;
		}
		public static uint XPToLevel(ulong xp)
		{
			//Shitcode ahead!
			uint lvl = 2;
			while(true) {
				ulong lvlXP = LevelToXP(lvl);
				if(xp<lvlXP) {
					return lvl-1;
				}
				lvl++;
			}
		}
		public static ulong LevelToXP(uint level)
		{
			ulong lvl = level-1;
			return 3*lvl*lvl*lvl+lvl;
		}
		#endregion
	}
}