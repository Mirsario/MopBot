using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Extensions;

namespace MopBot.Common.Systems.Welcoming
{
	[SystemConfiguration(Description = "Welcomes users onto the server with customizable messages.")]
	public partial class WelcomeSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,WelcomeServerData>();
		}

		public override async Task OnUserJoined(SocketGuildUser user)
		{
			var server = user.Guild;
			var memory = server.GetMemory();
			var welcomeData = memory.GetData<WelcomeSystem,WelcomeServerData>();

			if(!server.TryGetTextChannel(welcomeData.channel,out var welcomeChannel)) {
				return;
			}

			string msg;

			if(!memory.GetSubMemories<ServerUserMemory>().Any(p => p.Key==user.Id)) {
				msg = welcomeData.messageJoin;
			} else {
				msg = welcomeData.messageRejoin;
			}

			await welcomeChannel.SendMessageAsync(user.Mention,embed:MopBot.GetEmbedBuilder(server).WithDescription(msg).Build());
		}
	}
}