using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems.Permissions;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.Configuration
{
	[Group("config")]
	[Alias("configuration")]
	[Summary("Group for bot's configuration. Currently very WIP!")]
	[RequirePermission(SpecialPermission.Owner,"configuration")]
	[SystemConfiguration(AlwaysEnabled = true,Description = "Lets admins configure few important things, like bot's nickname and command prefix symbol.")]
	public partial class ConfigurationSystem : BotSystem
	{
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
	}
}