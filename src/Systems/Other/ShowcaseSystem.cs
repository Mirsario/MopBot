using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Rest;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public class EmbedCache
	{
		public string authorName;
		public string imageUrl;
		public string description;
		public Color? color;
		public DateTime cacheDate;

		public EmbedCache(SocketGuild server)
		{
			cacheDate = DateTime.Now;
			color = MemorySystem.memory[server].GetData<CommandSystem,CommandSystem.CommandServerData>().embedColor.Value;
		}
		public EmbedBuilder ToBuilder(SocketGuild server)
		{
			var builder = MopBot.GetEmbedBuilder(server);
			if(authorName!=null) {
				builder.WithAuthor(authorName,imageUrl);
			}
			if(description!=null) {
				builder.WithDescription(description);
			}
			return builder;
		}
	}
	
	[Group("showcase")]
	[RequirePermission("showcasesystem")]
	public class ShowcaseSystem : BotSystem
	{
		#region SubClasses
		public enum ShowcaseChannelType
		{
			Showcase,
			Spotlight
		}

		public abstract class ChannelInfo
		{
			public ulong id;
			public List<ulong> rewardRoles;
		}
		public class ShowcaseChannel : ChannelInfo
		{
			public ulong spotlightChannel;
			public uint minSpotlightScore = 20;

			public SocketTextChannel GetSpotlightChannel(SocketGuild server) => spotlightChannel==0 ? null : (server.GetChannel(spotlightChannel) as SocketTextChannel);
		}
		public class SpotlightChannel : ChannelInfo
		{
			public List<ulong> spotlightedMessages;
		}

		public class ShowcaseServerData : ServerData
		{
			public ShowcaseChannel[] showcaseChannels;
			public SpotlightChannel[] spotlightChannels;
			public Dictionary<string,string> emotes;

			public override void Initialize(SocketGuild server) {}

			public bool ChannelIs<T>(IChannel channel) where T : ChannelInfo
			{
				ulong channelId = channel.Id;
				return (typeof(T)==typeof(ShowcaseChannel) ? showcaseChannels : (ChannelInfo[])spotlightChannels)?.Any(c => c.id==channelId)==true;
			}
			public T GetChannelInfo<T>(IChannel channel,bool throwException = true) where T : ChannelInfo
			{
				ulong channelId = channel.Id;

				var info = (typeof(T)==typeof(ShowcaseChannel) ? showcaseChannels : (ChannelInfo[])spotlightChannels)?.FirstOrDefault(c => c.id==channelId);
				if(throwException && info==null) {
					throw new BotError($"{channel?.Name ?? "NULL"} is not a '{typeof(T).Name}'.");
				}

				return (T)info;
			}
			public bool TryGetChannelInfo<T>(IChannel channel,out T result) where T : ChannelInfo => (result = GetChannelInfo<T>(channel,false))!=null;

			public IEmote GetEmote(SocketGuild server,string emoteId,bool throwOnNull = true)
			{
				IEmote emote;
				if(emotes==null || !emotes.TryGetValue(emoteId,out string emoteName) || (emote = server.GetEmote(emoteName))==null) {
					throw new BotError($"Emote '{emoteId}' is not assigned.");
				}
				return emote;
			}
			public bool TryGetEmote(SocketGuild server,string emoteId,out IEmote result) => (result = GetEmote(server,emoteId,false))!=null;

			public void RemoveChannel(ulong id)
			{
				void Loop<T>(ref T[] array) where T : ChannelInfo
				{
					if(array==null) {
						return;
					}
					
					List<T> list = null;
					int offset = 0;

					for(int i = 0;i<array.Length;i++) {
						var info = array[i];
						if(info.id==id) {
							if(list==null) {
								list = array.ToList();
							}
							list.RemoveAt(i+offset--);
						}
					}

					if(list!=null) {
						array = list.ToArray();
					}
				}

				Loop(ref showcaseChannels);
				Loop(ref spotlightChannels);
			}
		}
		#endregion

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,ShowcaseServerData>();
		}

		public override async Task OnMessageReceived(MessageExt message)
		{
			var server = message.server;
			var channel = message.socketTextChannel;
			if(server==null || channel==null || message.user.IsBot) {
				return;
			}
			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();
			if(!showcaseData.ChannelIs<ShowcaseChannel>(message.socketTextChannel)) {
				return;
			}
			
			if(showcaseData.TryGetEmote(server,"upvote",out var upvoteEmote)) {
				await message.TryAddReaction(upvoteEmote);
			}
			if(showcaseData.TryGetEmote(server,"downvote",out var downvoteEmote)) {
				await message.TryAddReaction(downvoteEmote);
			}
		}
		public override async Task OnReactionAdded(MessageExt message,SocketReaction reaction)
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

			if(!showcaseData.TryGetEmote(server,"upvote",out var upvoteEmote)) {
				return;
			}
			
			var downvoteEmote = showcaseData.GetEmote(server,"downvote",false);
			int numUpvotes = await GetNumReactions(message.restMessage,upvoteEmote);
			int numDownvotes = downvoteEmote==null ? 0 : await GetNumReactions(message.restMessage,downvoteEmote);
			int totalScore = numUpvotes-numDownvotes;

			if(showcaseChannelInfo.minSpotlightScore>0 && totalScore==showcaseChannelInfo.minSpotlightScore
				&& showcaseData.TryGetChannelInfo<SpotlightChannel>(spotlightChannel,out var spotlightChannelInfo)
				&& (spotlightChannelInfo.spotlightedMessages==null || !spotlightChannelInfo.spotlightedMessages.Contains(message.message.Id))
			) {
				await SpotlightPost(message,spotlightChannel);
			}
		}
		
		public async Task<int> GetNumReactions(RestUserMessage restMessage,IEmote emote)
		{
			if(restMessage==null) {
				return -1;
			}

			int count = 0;
			int numTries = 0;
			
			while(numTries<3) {
				try { await restMessage.GetReactionUsersAsync(emote,100).ForEachAsync(list => { count += list.Count; }); }
				catch {
					numTries++;
				}
				break;
			}
			return count;
		}

		public static async Task SpotlightPost(MessageExt message,SocketTextChannel spotlightChannel,bool silent = false)
		{
			var server = message.server;
			if(server==null) {
				throw new BotError("Server was null?");
			}
			var sourceTextChannel = message.socketTextChannel;
			var serverMemory = server.GetMemory();
			var showcaseData = serverMemory.GetData<ShowcaseSystem,ShowcaseServerData>();
			var sourceChannelInfo = sourceTextChannel==null ? null : showcaseData.GetChannelInfo<ShowcaseChannel>(sourceTextChannel,false);
			var spotlightChannelInfo = showcaseData.GetChannelInfo<SpotlightChannel>(spotlightChannel);
			var msg = message.message;

			string url = msg.Attachments.FirstOrDefault()?.Url ?? msg.Embeds.FirstOrDefault(e => e.Type==EmbedType.Image || e.Type==EmbedType.Gifv)?.Url;
			if(url==null) {
				throw new BotError("Couldn't get image url from that message.");
			}

			var socketServerUser = server.GetUser(message.user.Id);
			
			string congratsText = $"{message.user.Mention}, your <#{message.Channel.Id}> post has made it to {spotlightChannel.Mention}!";
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
									congratsText += $"\nYou were also given the `{role.Name}` role!";
								}
							}
							catch {}
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

			var builder = MopBot.GetEmbedBuilder(message)
				.WithColor(socketServerUser.GetColor())
				.WithAuthor($"By {socketServerUser?.Name() ?? message.user.Username}:",message.user.GetAvatarUrl(),BotUtils.GetMessageUrl(server,message.Channel,msg))
				.WithImageUrl(url);
				//.WithFooter("Final Score - {numUpvotes}/{numTotalVotes}",BotUtils.GetEmojiImageUrl("⭐"));
			
			if(!string.IsNullOrWhiteSpace(message.content)) {
				builder.WithDescription(message.content.Replace(url,"").Trim());
			}

			await spotlightChannel.SendMessageAsync(embed:builder.Build());
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

		#region ManagingCommands
		[Command("forcespotlight")] [Alias("add","spotlight")]
		[RequirePermission("showcasesystem.manage")]
		public async Task ForceSpotlight(SocketTextChannel showcaseChannel,ulong messageId,SocketTextChannel spotlightChannel,bool silent = false)
		{
			var msg = await showcaseChannel.GetMessageAsync(messageId);
			if(msg==null) {
				throw new BotError($"Unable to find message with such ID in channel `{showcaseChannel.Name}`.");
			}
			await SpotlightPost(new MessageExt(msg),spotlightChannel,silent);
		}
		/*[Command("removefromspotlight")] [Alias("remove")]
		[RequirePermission("showcasesystem.manage")]
		public async Task RemoveFromSpotlight(SocketTextChannel spotlightChannel,ulong messageId)
		{
			var msg = await spotlightChannel.GetMessageAsync(messageId);
			if(msg==null) {
				throw new BotError($"Unable to find message with such ID in channel `{spotlightChannel.Name}`.");
			}
			var server = Context.server;
			var serverMemory = server.GetMemory();
			var showcaseData = serverMemory.GetData<ShowcaseSystem,ShowcaseServerData>();
		}*/
		#endregion

		#region ConfigCommands
		[Command("removechannel")]
		[RequirePermission("showcasesystem.configure")]
		public async Task RemoveChannel(SocketTextChannel channel)
		{
			var showcaseData = Context.server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();
			
			showcaseData.RemoveChannel(channel.Id);
		}

		[Command("setupchannel showcase")]
		[RequirePermission("showcasesystem.configure")]
		public async Task SetupChannelShowcase(SocketTextChannel channel,[Remainder]params SocketRole[] rewardRoles)
			=> await SetupChannelShowcase(channel,null,0,rewardRoles);

		[Command("setupchannel showcase")]
		[RequirePermission("showcasesystem.configure")]
		public async Task SetupChannelShowcase(SocketTextChannel channel,SocketTextChannel spotlightChannel,uint spotlightScore,[Remainder]params SocketRole[] rewardRoles)
		{
			if(channel==spotlightChannel) {
				throw new BotError("'channel' can't be the same value as 'spotlightChannel'.");
			}
			
			var server = Context.server;
			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();

			if(spotlightChannel!=null && !showcaseData.ChannelIs<SpotlightChannel>(spotlightChannel)) {
				throw new BotError($"Channel <#{spotlightChannel.Id}> is not a spotlight channel. Setup it as one first before setting up showcase channels for it.");
			}
			
			showcaseData.RemoveChannel(channel.Id);
			ArrayUtils.Add(ref showcaseData.showcaseChannels,new ShowcaseChannel {
				id = channel.Id,
				spotlightChannel = spotlightChannel==null ? 0 : spotlightChannel.Id,
				minSpotlightScore = spotlightScore,
				rewardRoles = (rewardRoles==null || rewardRoles.Length==0) ? null : rewardRoles.SelectIgnoreNull(role => role.Id).ToList()
			});
		}

		[Command("setupchannel spotlight")]
		[RequirePermission("showcasesystem.configure")]
		public async Task SetupChannelSpotlight(SocketTextChannel channel,[Remainder]params SocketRole[] rewardRoles)
		{
			var server = Context.server;
			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();

			showcaseData.RemoveChannel(channel.Id);
			ArrayUtils.Add(ref showcaseData.spotlightChannels,new SpotlightChannel {
				id = channel.Id,
				rewardRoles = (rewardRoles==null || rewardRoles.Length==0) ? null : rewardRoles.SelectIgnoreNull(role => role.Id).ToList()
			});
		}

		[Command("setemote")]
		[RequirePermission("showcasesystem.configure")]
		public async Task SetEmote(string type,string emote)
		{
			var regex = new Regex(@"<:(\w+):\d+>");
			var match = regex.Match(emote);
			if(!match.Success) {
				throw new BotError("Invalid emote.");
			}
			string realEmote = match.Groups[1].Value;
			
			var server = Context.server;
			var showcaseData = server.GetMemory().GetData<ShowcaseSystem,ShowcaseServerData>();

			var dict = showcaseData.emotes ?? (showcaseData.emotes = new Dictionary<string,string>());
			dict[type] = realEmote;
		}
		#endregion

		/*
		[Command("score")]
		public async Task ScoreCommand()
		{
			await ScoreCommand(Context.socketServerUser);
		}
		[Command("score")]
		public async Task ScoreCommand(SocketGuildUser user)
		{
			var server = Context.server;
			if(server==null) {
				return;
			}
			GetInfo(server,out var serverMemory,out var showcaseData,out var upvoteEmote,out var downvoteEmote);
			if(showcaseData.isEnabled!=true || showcaseData.showcaseChannels==null || showcaseData.showcaseChannels.Length==0) {
				return;
			}
			var botMessage = await Context.ReplyAsync("Searching... Please wait.",false);
			int numMessages = 5;
			ulong authorId = user.Id;
			List<IMessage> userPosts = new List<IMessage>();
			for(int i = 0;i<showcaseData.showcaseChannels.Length;i++) {
				var channelInfo = showcaseData.showcaseChannels[i];
				if(!(server.GetChannel(channelInfo.id) is IMessageChannel channel)) {
					continue;
				}
				var messages = channel.GetMessagesAsync(400);
				await messages.ForEachAsync(c => {
					foreach(var m in c) {
						if(m.Attachments.Count!=0 && m.Author.Id==authorId) {
							userPosts.Add(m);
						}
					}
				});
			}
			if(userPosts.Count==0) {
				await botMessage.ModifyAsync(m => m.Content = $"{Context.user.Mention} Couldn't find any *recent* showcases by {(Context.user.Id==user.Id ? "you" : user.Name())}.");
				return;
			}
			userPosts = userPosts.OrderByDescending(m => m.Timestamp).Take(numMessages).Reverse().ToList();
			string text = "";
			int totalScore = 0;
			float totalRatio = 0f;
			for(int i = 0;i<userPosts.Count;i++) {
				var post = userPosts[i];
				if(!(post is RestUserMessage restPost)) {
					await Context.ReplyAsync("Something broke, so don't use this.");
					return;
				}
				int numUpvotes = await GetNumReactions(restPost,upvoteEmote);
				int numDownvotes = downvoteEmote==null ? 0 : await GetNumReactions(restPost,downvoteEmote);
				int score = numUpvotes-numDownvotes;
				float ratio = numDownvotes==0 ? numUpvotes : numUpvotes/(float)numDownvotes;
				text += $"**{post.CreatedAt.UtcDateTime.ToString("dd'/'MM'/'yyyy")}**-<#{post.Channel.Id}>-{numUpvotes}-{numDownvotes}={score}{(ratio==0f ? "" : $" ({(100-100f/ratio).ToString("0.00")}%)")}\n";
				totalScore += score;
				totalRatio += ratio;
			}
			float averageRatio = totalRatio/userPosts.Count;
			text += $"**Average score:**{totalScore/(float)userPosts.Count}\n**Average ratio:**{averageRatio}{(averageRatio==0f ? "" : $" ({(100-100f/averageRatio).ToString("0.00")}%)")}";
			var builder = MopBot.GetEmbedBuilder(Context);
			builder.WithAuthor($"{user.Name()}'s recent scores",user.GetAvatarUrl());
			builder.WithDescription(text);
			await botMessage.ModifyAsync(m => {
				m.Content = $"{Context.user.Mention} Done.";
				m.Embed = builder.Build();
			});
		}
		[Command("scoreleaders")]
		public async Task ScoreLeadersCommand()
		{
			try {
				var server = Context.server;
				if(server==null) {
					return;
				}
				GetInfo(server,out var serverMemory,out var showcaseData,out var upvoteEmote,out var downvoteEmote);
				if(showcaseData.isEnabled!=true || showcaseData.showcaseChannels==null || showcaseData.showcaseChannels.Length==0) {
					return;
				}
				if(serversSearchingLeadersOn.TryGetFirst(t => t.server==Context.server.Id,out var tuple)) {
					await Context.ReplyAsync($"A search is already ongoing on this server for channel <#{tuple.channel}>.");
					return;
				}
				serversSearchingLeadersOn.Add((Context.server.Id,Context.Channel.Id));

				EmbedCache cache = showcaseData.leadersCache;
				if(cache==null || (DateTime.Now-cache.cacheDate).TotalHours>=24) {
					await Context.ReplyAsync("Searching... Please wait,this may take a pretty long time. You'll be pinged once it's done.",false);
					const int numMessages = 5;
					const int numLeaders = 10;
					var userPosts = new Dictionary<IUser,List<IMessage>>();
					for(int i = 0;i<showcaseData.showcaseChannels.Length;i++) {
						var channelInfo = showcaseData.showcaseChannels[i];
						if(!(server.GetChannel(channelInfo.id) is IMessageChannel channel)) {
							continue;
						}
						var messages = channel.GetMessagesAsync(500);
						await messages.ForEachAsync(c => {
							foreach(var m in c) {
								if(m.Attachments.Count==0) {
									continue;
								}
								if(!userPosts.TryGetValue(m.Author,out var list)) {
									userPosts[m.Author] = list = new List<IMessage>();
								}
								if(list.Count<numMessages) {
									list.Add(m);
								}
							}
						});
					}
					if(userPosts.Count==0) {
						await Context.ReplyAsync("Error. Are all showcase channels empty?");
						return;
					}
					var userValues = new Dictionary<IUser,(float averageScore,float averageRatio)>();
					foreach(var pair in userPosts) {
						int totalScore = 0;
						float totalRatio = 0f;
						var list = pair.Value;
						foreach(var post in list) {
							if(!(post is RestUserMessage restPost)) {
								await Context.ReplyAsync("Rip, message isn't RestUserMessage");
								return;
							}
							int numUpvotes = await GetNumReactions(restPost,upvoteEmote);
							int numDownvotes = downvoteEmote==null ? 0 : await GetNumReactions(restPost,downvoteEmote);
							int score = numUpvotes-numDownvotes;
							float ratio = numDownvotes==0 ? numUpvotes : numUpvotes/(float)numDownvotes;
							totalScore += score;
							totalRatio += ratio;
						}
						userValues[pair.Key] = (totalScore/(float)list.Count,totalRatio/list.Count);
					}
					var leaders = userValues.OrderByDescending(p => p.Value.averageScore).Take(numLeaders).Select(p => (server.GetUser(p.Key.Id) ?? p.Key,p.Value.averageScore,p.Value.averageRatio));
					var leader = leaders.First();
					var builder = MopBot.GetEmbedBuilder(Context);
					int j = 2;
					showcaseData.leadersCache = cache = new EmbedCache(Context.server) {
						authorName = $"#1-{leader.Item1.Name()}-{leader.averageScore} avg. score{(leader.averageRatio==0f ? "" : $" ({(100-100f/leader.averageRatio).ToString("0.00")}%)")}",
						imageUrl = leader.Item1.GetAvatarUrl(),
						description = string.Join("\n",leaders.TakeLast(leaders.Count()-1).Select(t => {
							string result = $"{MopBot.hackSpaceChar.Repeat(5*(j==9 ? 2 : j==10 ? 4 : 1))}**#{j}**-{t.Item1.Name()}-{t.averageScore} avg. score{(t.averageRatio==0f ? "" : $" ({(100-100f/t.averageRatio).ToString("0.00")}%)")}";
							j++;
							return result;
						}))
					};
				}
				await Context.socketTextChannel.SendMessageAsync($"{Context.user.Mention} {cache.cacheDate.ToString("dd'/'MM'/'yyyy")} leaderboards (based on ~500 last posts in every showcase channel)",embed:cache.ToBuilder(Context.server).Build());
				serversSearchingLeadersOn.RemoveAll(t => t.server==Context.server.Id);
			}
			catch(Exception e) {
				serversSearchingLeadersOn.RemoveAll(t => t.server==Context.server.Id);
				await MopBot.HandleException(e);
			}
		}
		*/
	}
}