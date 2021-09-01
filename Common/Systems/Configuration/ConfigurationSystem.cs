using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;

namespace MopBot.Common.Systems.Configuration
{
	[Group("config")]
	[Alias("configuration")]
	[Summary("Group for bot's configuration. Currently very WIP!")]
	[RequirePermission(SpecialPermission.Admin, "configuration")]
	[SystemConfiguration(AlwaysEnabled = true, Description = "Lets admins configure few important things, like bot's nickname and command prefix symbol.")]
	public partial class ConfigurationSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, ConfigurationServerData>();
		}

		public override async Task ServerUpdate(SocketGuild server)
		{
			var memory = server.GetMemory().GetData<ConfigurationSystem, ConfigurationServerData>();
			string nickname = memory.forcedNickname;

			var currentUser = server.GetUser(MopBot.client.CurrentUser.Id);

			if (currentUser != null && !string.Equals(currentUser.Nickname, nickname, StringComparison.InvariantCulture) && !(nickname == "MopBot" && currentUser.Nickname == null) && currentUser.HasDiscordPermission(p => p.ChangeNickname)) {
				try {
					await currentUser.ModifyAsync(u => u.Nickname = nickname);
				}
				catch (Exception e) {
					await MopBot.HandleException(e);
				}
			}
		}
	}
}
