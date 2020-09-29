﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Common.Systems.Currency;
using MopBot.Core;

namespace MopBot.Common.Systems.Trivia
{
	[Group("trivia")]
	[Summary("Group for anything related to trivia questions.")]
	[SystemConfiguration(Description = "Lets admins setup trivia channels, in which the bot will ask users questions and give customizable rewards to the first answerers.")]
	public partial class TriviaSystem : BotSystem
	{
		public const int MinPostIntervalInSeconds = 10;

		public static Regex regexQuestionAndAnswers = new Regex(@"(.+)\s+-\s+(.+)");
		public static Regex regexAnswers = new Regex(@"([^,]+)\s*,?\s*");
		private static Regex currentQuestionRegex;

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory, TriviaServerData>();
		}

		public override async Task ServerUpdate(SocketGuild server)
		{
			var triviaServerData = server.GetMemory().GetData<TriviaSystem, TriviaServerData>();

			if(!triviaServerData.IsReady) {
				return; //TODO: Let the admins know that this is misconfigured.
			}

			var now = DateTime.Now;

			if((now - triviaServerData.lastTriviaPost).TotalSeconds <= triviaServerData.postIntervalInSeconds) {
				return;
			}

			triviaServerData.lastTriviaPost = now;
			triviaServerData.currentChannel = triviaServerData.triviaChannels[MopBot.Random.Next(triviaServerData.triviaChannels.Count)];

			var channel = server.GetTextChannel(triviaServerData.currentChannel);

			if(channel == null) {
				return; //TODO: Same as above
			}

			TriviaQuestion[] validQuestions;

			//Find questions we didn't pick yet, handle running out of them.
			while(true) {
				lock(triviaServerData.questions) {
					validQuestions = triviaServerData.questions.Where(q => !q.wasPosted).ToArray();
				}

				if(validQuestions.Length == 0) {
					if(triviaServerData.autoClearCache && triviaServerData.questions.Count > 0) {
						ClearCache(triviaServerData);

						continue;
					} else {
						await channel.SendMessageAsync($"{server.Owner.Mention} We're out of trivia questions!\r\n\r\nAdd new questions, or..\r\n• Use `!trivia clearcache` to clear list of used questions;\r\n• Use `!trivia autoclearcache true` to automate this process, if you're fully okay with same questions being repeat;");
						return;
					}
				}

				break;
			}

			//Set new question
			triviaServerData.currentQuestion = validQuestions[MopBot.Random.Next(validQuestions.Length)];
			triviaServerData.currentQuestion.wasPosted = true;
			currentQuestionRegex = null; //This will cause a new one to be made, when needed.

			string mention = null;
			SocketRole role = null;
			bool disallowRoleMention = false;
			var currentUser = server.CurrentUser;

			if(triviaServerData.triviaRole > 0) {
				role = server.GetRole(triviaServerData.triviaRole);

				if(role != null) {
					if(!role.IsMentionable && currentUser.HasDiscordPermission(gp => gp.ManageRoles)) {
						await role.ModifyAsync(rp => rp.Mentionable = true);
						disallowRoleMention = true;
					}

					mention = role.Mention;
				} else {
					triviaServerData.triviaRole = 0;
				}
			}

			var embedBuilder = MopBot.GetEmbedBuilder(server)
				.WithAuthor("The next question is...") //.WithTitle("The next question is...")
				.WithDescription($"**{triviaServerData.currentQuestion.question}**")
				.WithFooter("Type your answer right in this channel!");

			if(triviaServerData.thumbnailUrls?.Length > 0 == true) {
				try {
					embedBuilder.WithThumbnailUrl(triviaServerData.thumbnailUrls[MopBot.Random.Next(triviaServerData.thumbnailUrls.Length)]);
				}
				catch { }
			}

			await channel.SendMessageAsync(mention, embed: embedBuilder.Build());

			if(disallowRoleMention) {
				await role.ModifyAsync(rp => rp.Mentionable = false);
			}

			if(triviaServerData.lockTriviaChannel && currentUser.HasChannelPermission(channel, DiscordPermission.ManageChannel)) {
				//Unlock the channel, since there's a question now.

				await channel.ModifyPermissions(server.EveryoneRole, op => op.SendMessages == PermValue.Deny ? op.Modify(sendMessages: PermValue.Inherit) : op);
			}
		}

		public override async Task OnMessageReceived(MessageContext context)
		{
			var server = context.server;
			var channel = context.socketTextChannel;

			if(channel == null) {
				return;
			}

			var triviaServerMemory = server.GetMemory().GetData<TriviaSystem, TriviaServerData>();
			var qa = triviaServerMemory.currentQuestion;

			if(qa == null || channel.Id != triviaServerMemory.currentChannel) {
				return;
			}

			var regex = GetCurrentQuestionRegex(triviaServerMemory);

			string text = context.content.ToLower().RemoveWhitespaces();
			var match = regex.Match(context.content);

			if(match.Success) {
				triviaServerMemory.currentQuestion = null;

				var user = context.socketServerUser;

				CurrencyAmount.TryGiveToUser(ref triviaServerMemory.currencyRewards, user, out string givenString);

				var timeSpan = DateTime.Now - triviaServerMemory.lastTriviaPost.AddSeconds(triviaServerMemory.postIntervalInSeconds);
				var embed = MopBot.GetEmbedBuilder(server)
					.WithDescription($"{user.Mention} wins{(givenString != null ? $", and gets {givenString}" : null)}!\r\nThe question was `{qa.question}`, and their answer was `{match.Groups[1].Value}`.\r\n\r\nThe next question will come up in `{timeSpan:m'm 's's'}` from now.")
					.Build();

				await channel.SendMessageAsync(embed: embed);

				var currentUser = server.CurrentUser;

				if(triviaServerMemory.lockTriviaChannel && currentUser.HasChannelPermission(channel, DiscordPermission.ManageChannel)) {
					//Lock the channel, since the question has been answered.

					await channel.ModifyPermissions(currentUser, op => op.Modify(sendMessages: PermValue.Allow)); //Make sure we're still allowed to post
					await channel.ModifyPermissions(server.EveryoneRole, op => op.Modify(sendMessages: PermValue.Deny)); //Forbid posting for everyone else
				}

				return;
			}
		}

		public static Regex GetCurrentQuestionRegex(TriviaServerData data)
			=> currentQuestionRegex ??= new Regex(@$"(?:^|[^\w])({string.Join('|', data.currentQuestion.answers.Select(a => Regex.Escape(a)))})(?=[^\w]|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static void ClearCache(TriviaServerData data)
		{
			//TODO: Perhaps track when the questions were posted, and only clear the flag on the older half?
			lock(data.questions) {
				for(int i = 0; i < data.questions.Count; i++) {
					data.questions[i].wasPosted = false;
				}
			}
		}
	}
}
