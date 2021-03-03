using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MopBot.Extensions;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Core.Systems.Permissions
{
	public class RequirePermissionAttribute : PreconditionAttribute
	{
		public SpecialPermission? specialPermission;
		public string[] requireAny;

		public RequirePermissionAttribute(SpecialPermission specialPermission, params string[] requireAny)
		{
			this.specialPermission = specialPermission;
			this.requireAny = requireAny;
		}
		public RequirePermissionAttribute(params string[] requireAny)
		{
			this.requireAny = requireAny;
		}

		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			//CommandInfo can be null!

			if(!(context.User is SocketGuildUser user) || !(context.Guild is SocketGuild server)) {
				return PreconditionResult.FromError("You must be in a server to use this.");
			}

			if(specialPermission.HasValue) {
				SpecialPermission thisValue = 0;

				if(server.OwnerId == user.Id) {
					thisValue |= SpecialPermission.Owner;
					thisValue |= SpecialPermission.Admin;
				} else if(server.Roles.Any(r => r.Permissions.Administrator)) {
					thisValue |= SpecialPermission.Admin;
				}

				if(user.IsBotMaster()) {
					thisValue |= SpecialPermission.BotMaster;
				}

				if(((byte)thisValue & (byte)specialPermission.Value) > 0) {
					return PreconditionResult.FromSuccess();
				}
			}

			if(requireAny != null && requireAny.Length > 0) {
				if(user.HasAnyPermissions(requireAny)) {
					return PreconditionResult.FromSuccess();
				}

				return PreconditionResult.FromError(
					requireAny.Length > 1
						? $"Missing one of the following permissions:\r\n{string.Join("\r\n", requireAny.Select(s => "`" + s + "`"))}"
						: $"Missing permission: `{requireAny[0]}`."
				);
			}

			return PreconditionResult.FromError("You do not have a permission to use this.");
		}
	}
}
