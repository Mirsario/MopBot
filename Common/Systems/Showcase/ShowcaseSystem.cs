using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems.Memory;
using MopBot.Core.Systems.Permissions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Channels;
using MopBot.Core;

namespace MopBot.Common.Systems.Showcase
{
	[Group("showcase")]
	[Summary("Group for commands for managing the showcase system, which let's admins setup channels with reaction-based voting and spotlighting.")]
	[RequirePermission(SpecialPermission.Owner,"showcasesystem")]
	[SystemConfiguration(Description = "Lets admins setup channels with reaction-based voting. There's also spotlighting support, which moves channels with X score to a selected channel, and also gives author customizable rewards if needed.")]
	public partial class ShowcaseSystem : BotSystem
	{
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ShowcaseServerData>();
		}

		public override async Task OnMessageReceived(MessageContext context)
		{
			var server = context.server;
			var channel = context.socketTextChannel;

			if(server==null || channel==null || context.user.IsBot) {
				return;
			}

			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();

			if(!showcaseData.ChannelIs<ShowcaseChannel>(context.socketTextChannel)) {
				return;
			}

			//TODO: Check permissions?
			try {
				if(showcaseData.TryGetEmote(EmoteType.Upvote,out var upvoteEmote)) {
					await context.Message.AddReactionAsync(upvoteEmote);
				}

				if(showcaseData.TryGetEmote(EmoteType.Downvote,out var downvoteEmote)) {
					await context.Message.AddReactionAsync(downvoteEmote);
				}
			}
			catch {}
		}
		public override async Task OnReactionAdded(MessageContext message,SocketReaction reaction)
		{
			var server = message.server;

			if(server==null || message.user.IsBot) {
				return;
			}

			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();

			if(message.Channel==null || !showcaseData.TryGetChannelInfo<ShowcaseChannel>(message.Channel,out var showcaseChannelInfo)) {
				return;
			}

			var spotlightChannel = showcaseChannelInfo.GetSpotlightChannel(server);

			if(showcaseChannelInfo.GetSpotlightChannel(server)==null) {
				return;
			}

			if(!showcaseData.TryGetEmote(EmoteType.Upvote,out var upvoteEmote)) {
				return;
			}

			int numUpvotes = Math.Max(1,await GetNumReactions(message.message,upvoteEmote));
			int numDownvotes = showcaseData.TryGetEmote(EmoteType.Downvote,out var downvoteEmote) ? Math.Max(1,await GetNumReactions(message.message,downvoteEmote)) : 1;
			int totalScore = numUpvotes-numDownvotes;

			if(showcaseChannelInfo.minSpotlightScore==0 || totalScore!=showcaseChannelInfo.minSpotlightScore) {
				return;
			}

			if(!showcaseData.TryGetChannelInfo<SpotlightChannel>(spotlightChannel,out var spotlightChannelInfo)) {
				return;
			}

			if(spotlightChannelInfo.spotlightedMessages!=null && spotlightChannelInfo.spotlightedMessages.Contains(message.message.Id)) {
				return;
			}

			await SpotlightPost(message,spotlightChannel,votes: (numUpvotes, numDownvotes));
		}

		public static async Task<int> GetNumReactions(IMessage message,IEmote emote)
		{
			if(emote==null) {
				return 0;
			}

			int count = 0;

			if(message is IUserMessage userMessage) {
				await userMessage.GetReactionUsersAsync(emote,100).ForEachAsync(list => { count += list.Count; });
			} else {
				Console.WriteLine($"Unable to get the amount of reactions. Message type is '{message?.GetType()?.Name ?? "null"}'.");
			}

			return count;
		}

		public static async Task SpotlightPost(MessageContext context,SocketTextChannel spotlightChannel,bool silent = false,(int upvotes,int downvotes)? votes = null)
		{
			var server = context.server;
			var sourceTextChannel = context.socketTextChannel;
			var serverMemory = server.GetMemory();
			var showcaseData = serverMemory.GetData<ShowcaseSystem,ShowcaseServerData>();
			var sourceChannelInfo = sourceTextChannel==null ? null : showcaseData.GetChannelInfo<ShowcaseChannel>(sourceTextChannel,false);
			var spotlightChannelInfo = showcaseData.GetChannelInfo<SpotlightChannel>(spotlightChannel);
			var msg = context.message;

			string url = msg.Attachments.FirstOrDefault()?.Url ?? msg.Embeds.FirstOrDefault(e => e.Type==EmbedType.Image || e.Type==EmbedType.Gifv)?.Url;

			if(url==null) {
				throw new BotError("Couldn't get image url from that message.");
			}

			var socketServerUser = server.GetUser(context.user.Id);

			string congratsText = $"{context.user.Mention}, your <#{context.Channel.Id}> post has made it to {spotlightChannel.Mention}!";

			async Task TryGiveRewards(List<ulong> roles)
			{
				if(roles==null) {
					return;
				}

				for(int i = 0;i<roles.Count;i++) {
					ulong roleId = roles[i];

					SocketRole role = server.GetRole(roleId);

					if(role==null) {
						roles.RemoveAt(i--);

						continue;
					}

					if(server.CurrentUser.HasDiscordPermission(gp => gp.ManageRoles)) {
						if(!socketServerUser.Roles.Any(r => r.Id==role.Id)) {
							try {
								await socketServerUser.AddRoleAsync(role);

								if(!silent) {
									congratsText += $"\r\nYou were also given the `{role.Name}` role!";
								}
							}
							catch { }
						}
					} else {
						var channelMemory = serverMemory.GetData<ChannelSystem,ChannelServerData>();

						if(channelMemory.TryGetChannelByRoles(out var logChannel,ChannelRole.Logs,ChannelRole.BotArea,ChannelRole.Default)) {
							throw new BotError($"Unable to grant `{role.Name}` role to user {socketServerUser.Username}#{socketServerUser.Discriminator}: Missing `Manage Roles` permission.");
						}
					}
				}
			}

			await TryGiveRewards(sourceChannelInfo?.rewardRoles);
			await TryGiveRewards(spotlightChannelInfo.rewardRoles);

			var link = BotUtils.GetMessageUrl(server,context.Channel,msg);

			string content = context.content?.Replace(url,"")?.Trim();

			if(!string.IsNullOrWhiteSpace(content)) {
				content += "\r\n";
			}

			content += $@"[\[source\]]({link})";

			showcaseData.TryGetEmote(EmoteType.Upvote,out var upvoteEmote);
			showcaseData.TryGetEmote(EmoteType.Downvote,out var downvoteEmote);

			var (numUpvotes,numDownvotes) = votes ?? (
				upvoteEmote==null ? 0 : await GetNumReactions(context.message,upvoteEmote),
				downvoteEmote==null ? 0 : await GetNumReactions(context.message,downvoteEmote)
			);

			numUpvotes -= 1;
			numDownvotes -= 1;

			var builder = MopBot.GetEmbedBuilder(context)
				.WithColor(socketServerUser.GetColor())
				//.WithTitle("[Click here to jump to the original message]").WithUrl(link)
				.WithAuthor($"By {socketServerUser?.GetDisplayName() ?? context.user.Username}:",context.user.GetAvatarUrl())
				.WithDescription(content)
				.WithImageUrl(url)
				.WithFooter($"Final Score - {numUpvotes/(float)(numUpvotes+numDownvotes)*100f:0.00}%","https://i.imgur.com/Wh8s8Gp.png"); //TODO: This url is a png of discord's star emoji. Find a proper way to get the image off discord's servers? Their image urls seem to change over time.

			await spotlightChannel.SendMessageAsync(embed: builder.Build());

			var spotlightedMessages = Utils.GetSafe(ref spotlightChannelInfo.spotlightedMessages);

			if(!spotlightedMessages.Contains(msg.Id)) {
				spotlightedMessages.Add(msg.Id);
			}

			if(!silent) {
				var general = server.GetMemory()?.GetData<ChannelSystem,ChannelServerData>()?.GetChannelByRole(ChannelRole.Default);

				if(general!=null) {
					await ((IMessageChannel)general).SendMessageAsync(congratsText);
				}
			}
		}
	}
}