using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	[Group("config")]
	[Alias("configuration")]
	[Summary("Group for bot's configuration. Currently very WIP!")]
	[RequirePermission(SpecialPermission.Owner,"configuration")]
	[SystemConfiguration(AlwaysEnabled = true)]
	public class ConfigurationSystem : BotSystem
	{
		public class ConfigurationServerData : ServerData
		{
			public string forcedNickname;

			public override void Initialize(SocketGuild server)
			{
				forcedNickname = "MopBot";
			}
		}

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ConfigurationServerData>();
		}

		public override async Task ServerUpdate(SocketGuild server)
		{
			var memory = server.GetMemory().GetData<ConfigurationSystem,ConfigurationServerData>();
			string nickname = memory.forcedNickname;
			
			//var utcNow = DateTime.UtcNow;
			//if(utcNow.Month==4 && (utcNow.Day==1 || (utcNow.Day==2 && utcNow.Hour<7))) {
			//	nickname = "Villager"; //hahayes
			//}

			var currentUser = server.GetUser(MopBot.client.CurrentUser.Id);
			if(currentUser!=null && !string.Equals(currentUser.Nickname,nickname,StringComparison.InvariantCulture) && !(nickname=="MopBot" && currentUser.Nickname==null) && currentUser.HasDiscordPermission(p => p.ChangeNickname)) {
				try {
					await currentUser.ModifyAsync(u => u.Nickname = nickname);
				}
				catch(Exception e) {
					await MopBot.HandleException(e);
				}
			}
		}

		[Command("nickname")] [Alias("nick")]
		public async Task NicknameCommand([Remainder]string text) => Context.server.GetMemory().GetData<ConfigurationSystem,ConfigurationServerData>().forcedNickname = text;
		
		[Command("commandsymbol")] [Alias("cmdsymbol")]
		public async Task CommandPrefixCommand(char symbol) => Context.server.GetMemory().GetData<CommandSystem,CommandSystem.CommandServerData>().commandPrefix = symbol;
	}
}