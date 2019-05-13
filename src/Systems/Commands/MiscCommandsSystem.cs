using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MopBotTwo.Systems
{
	[SystemConfiguration(EnabledByDefault = true)]
	public class MiscCommandsSystem : BotSystem
	{
		[RequirePermission(SpecialPermission.Owner,"mentionroles")]
		[Command("mentionrole")] [Alias("pingrole","mention")]
		public async Task MentionRoleCommand(IRole role)
		{
			bool wasMentionable = role.IsMentionable;
			if(!wasMentionable) {
				await role.ModifyAsync(rp => rp.Mentionable = true);
			}
			await Context.Channel.SendMessageAsync(role.Mention);
			if(!wasMentionable) {
				await role.ModifyAsync(rp => rp.Mentionable = false);
			}
			await Context.Message.DeleteAsync();
		}
	}
}