using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public enum SpecialPermission
	{
		Owner
	}

	public class RequirePermissionAttribute : PreconditionAttribute
	{
		public SpecialPermission? specialPermission;
		public string[] requireAny;

		public RequirePermissionAttribute(SpecialPermission specialPermission,params string[] requireAny)
		{
			this.specialPermission = specialPermission;
			this.requireAny = requireAny;
		}
		public RequirePermissionAttribute(params string[] requireAny)
		{
			this.requireAny = requireAny;
		}
		public RequirePermissionAttribute()
		{
			requireAny = new string[] {};
		}

		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,CommandInfo command,IServiceProvider services)
		{
			//CommandInfo can be null!

			if(!(context.User is SocketGuildUser user) || !(context.Guild is SocketGuild server)) {
				return PreconditionResult.FromError("You must be in a server to use this.");
			}

			if(user.IsBotMaster() || (specialPermission.HasValue && specialPermission.Value==SpecialPermission.Owner && server.OwnerId==user.Id)) {
				return PreconditionResult.FromSuccess();
			}

			if(!user.HasAnyPermissions(requireAny)) {
				return PreconditionResult.FromError("You do not have a permission to use this.");
			}

			return PreconditionResult.FromSuccess();
		}
	}
}