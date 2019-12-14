using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MopBotTwo.Core.Systems;
using MopBotTwo.Core.Systems.Channels;
using MopBotTwo.Core.Systems.Memory;


namespace MopBotTwo.Common.Systems.Welcoming
{
	[SystemConfiguration(Description = "Welcomes users onto the server with customizable messages.")]
	public class WelcomeSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,WelcomeServerData>();
		}

		public override async Task OnUserJoined(SocketGuildUser user)
		{
			var memory = MemorySystem.memory[user.Guild];
			var welcomeData = memory.GetData<WelcomeSystem,WelcomeServerData>();
			var channelData = memory.GetData<ChannelSystem,ChannelServerData>();
			var welcomeChannel = channelData.GetChannelByRole(ChannelRole.Welcome) as ITextChannel;
			var logsChannel = channelData.GetChannelByRole(ChannelRole.Logs) as ITextChannel;
			var rulesChannel = channelData.GetChannelByRole(ChannelRole.Rules) as ITextChannel;

			string msg;
			if(!memory.GetSubMemories<ServerUserMemory>().Any(p => p.Key==user.Id)) {
				msg = welcomeData.messageJoin;
				logsChannel?.SendMessageAsync("<@!"+user.Id+"> has joined the server.");
			} else {
				msg = welcomeData.messageRejoin;
				logsChannel?.SendMessageAsync("<@!"+user.Id+"> has re-joined the server.");
			}

			welcomeChannel?.SendMessageAsync(msg.Replace("{user}",user.Mention).Replace("{rules}",rulesChannel?.Mention ?? "{rules}"));
		}
	}
}