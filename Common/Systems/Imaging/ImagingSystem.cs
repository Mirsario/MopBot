using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBotTwo.Extensions;
using MopBotTwo.Core.Systems;

namespace MopBotTwo.Common.Systems.Imaging
{
	[Group("imaging")]
	[Alias("img","image")]
	[Summary("Group for random commands related to working with images")]
	[SystemConfiguration(EnabledByDefault = true,Description = "Currently only contains an avatar-getting command.")]
	public class ImagingSystem : BotSystem
	{
		[Command("avatar")]
		public async Task AvatarCommand(SocketGuildUser user,[Remainder]string args = null)
		{
			var embed = MopBot.GetEmbedBuilder(Context)
				.WithAuthor($"{user.GetDisplayName()}'s avatar")
				.WithImageUrl(user.GetAvatarUrl())
				.WithFooter($"Requested by {Context.socketServerUser.GetDisplayName()}",Context.user.GetAvatarUrl())
				.Build();

			await Context.ReplyAsync(embed,false);
		}
	}
}