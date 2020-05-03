using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Core;
using MopBot.Utilities;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.XP
{
	[Group("xp")] [Alias("exp","rank")]
	[Summary("Command to check your XP, and a group for managing the system.")]
	[SystemConfiguration(Description = "A small XP/leveling system.")]
	public partial class XPSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerUserMemory,XPServerUserData>();
			RegisterDataType<ServerMemory,XPServerData>();
		}

		public override async Task Initialize() { }
		public override async Task OnMessageReceived(MessageContext message)
		{
			var server = message.server;

			if(server==null || message.isCommand || message.socketServerUser.IsBot) {
				return;
			}

			var serverMemory = MemorySystem.memory[server];
			var user = message.socketServerUser;
			var xpUserData = serverMemory[user].GetData<XPSystem,XPServerUserData>();
			var now = DateTime.Now;

			xpUserData.lastXPReceive = now;

			ulong xp = GetMessageXP(message);

			await GiveXP(user,xp);
		}
		public override async Task OnMessageDeleted(MessageContext message)
		{
			if(message==null ||  message.isCommand || message.server==null || message.socketServerUser==null || message.socketServerUser.IsBot) {
				return;
			}

			ulong xp = GetMessageXP(message);

			await TakeXP(message.socketServerUser,xp);
		}

		public static ulong GetMessageXP(MessageContext message)
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
			if(xp==0) {
				return 1;
			}

			return (uint)Math.Floor(Math.Sqrt(xp/3))+1;
		}
		public static ulong LevelToXP(uint level)
		{
			ulong lvl = level-1;

			return 3*lvl*lvl;
		}

		private static async Task ModifyXP(Func<ulong,ulong> xpModifier,SocketGuildUser user,SocketUserMessage message = null)
		{
			var server = user.Guild;
			var serverMemory = MemorySystem.memory[server];
			var userMemory = serverMemory[user];
			var xpUserData = userMemory.GetData<XPSystem,XPServerUserData>();

			uint prevLvl = XPToLevel(xpUserData.xp);

			xpUserData.xp = xpModifier(xpUserData.xp);

			uint newLvl = XPToLevel(xpUserData.xp);

			if(message!=null) {
				if(newLvl>prevLvl) {
					try {
						await message.AddReactionAsync(EmoteUtils.Parse("🌟"));
					}
					catch(Exception e) {
						await MopBot.HandleException(e);
					}
				}

				/*var mentionChannel = (ITextChannel)(serverMemory.GetData<ChannelSystem,ChannelServerData>().GetChannelByRole(ChannelRole.BotArea) ?? channel);
				string text = $"{user.Name()} has just reached level {newLvl}! :star:";

				var xpServerData = serverMemory.GetData<XPSystem,XPServerData>();

				if(xpServerData.levelRewards.TryGetValue(newLvl,out ulong[] roleIds)) {
					var oldAccessLevel = user.GetAccessLevel();
					var oldCommandList = CommandService.commands.Where(h => oldAccessLevel>=h.minAccessLevel);
					var roles = roleIds.Select(id => server.GetRole(id));

					text += $"\r\nThe following roles are now available to them:```{string.Join("\r\n",roles.Select(role => role.Name))}```";

					await user.AddRolesAsync(roles);

					var newAccessLevel = user.GetAccessLevel(roleIds);
					var newCommandList = CommandService.commands.Where(h => newAccessLevel>=h.minAccessLevel);
					var newCommandsOnly = newCommandList.Where(h => !oldCommandList.Contains(h)).ToArray();

					if(newCommandsOnly.Length>0) {
						text += $"\r\nThe following commands are now available to them:```{string.Join("\r\n",newCommandsOnly.Select(h => $"{string.Join("/",h.aliases)}-{h.description}"))}```";
					}
				}

				await mentionChannel.SendMessageAsync(text);*/
			}
		}
	}
}