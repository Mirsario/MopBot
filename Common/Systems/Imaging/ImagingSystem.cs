using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems;

namespace MopBot.Common.Systems.Imaging
{
	[Group("imaging")]
	[Alias("imageg", "img")]
	[Summary("Group for random commands related to working with images")]
	[SystemConfiguration(EnabledByDefault = true, Description = "Currently only contains an avatar-getting command.")]
	public class ImagingSystem : BotSystem
	{
		[Command("avatar")]
		public async Task AvatarCommand(SocketGuildUser user, [Remainder] string args = null)
		{
			var embed = MopBot.GetEmbedBuilder(Context)
				.WithAuthor($"{user.GetDisplayName()}'s avatar")
				.WithImageUrl(user.GetAvatarUrl(size: 1024))
				.WithFooter($"Requested by {Context.socketServerUser.GetDisplayName()}", Context.user.GetAvatarUrl())
				.Build();

			await Context.ReplyAsync(embed, false);
		}
	}
}