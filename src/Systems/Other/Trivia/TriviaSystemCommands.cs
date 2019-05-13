using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;
using MopBotTwo.Extensions;

#pragma warning disable CS1998 //This async method lacks 'await' operators

namespace MopBotTwo.Systems
{
	public partial class TriviaSystem : BotSystem
	{
		[Command("skip")]
		[RequirePermission("triviasystem.skip")]
		public async Task SkipQuestionCommand()
		{
			Context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().lastTriviaPost = default;
		}
		[Command("clearcache")]
		[RequirePermission("triviasystem.manage")]
		public async Task ClearCacheCommand()
		{
			ClearCache(Context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>());
		}

		#region QuestionCommands
		//TODO: There's some very similar uploading & downloading code in MemorySystem.cs, should really make such code shared.
		[Command("getquestions")]
		[RequirePermission("triviasystem.manage")]
		[Summary("Sends you all current questions in JSON format.")]
		public async Task GetQuestionsCommand([Remainder]string args = "")
		{
			var user = Context.user;
			var server = Context.server;
			var memory = server.GetMemory();
			var triviaServerMemory = memory.GetData<TriviaSystem,TriviaServerData>();
			var questions = triviaServerMemory.questions;
			if(questions==null || questions.Count==0) {
				throw new BotError("There are currently no questions in the database.");
			}

			IMessageChannel textChannel;
			if(args?.ToLower()!="here") {
				textChannel = await user.GetOrCreateDMChannelAsync() ?? throw new BotError("I'm unable to send you a private message. Use `!trivia getquestions here` to post the data right in this channel (everyone will be able to see answers to them!)");
			}else{
				textChannel = Context.Channel;
			}

			var dictionary = new Dictionary<string,string[]>();

			lock(questions) {
				for(int i = 0;i<questions.Count;i++) {
					var q = questions[i];
					dictionary.Add(q.question,q.answers);
				}
			}

			var json = JsonConvert.SerializeObject(dictionary,Formatting.Indented);

			using(MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) {
				await textChannel.SendFileAsync(stream,"TriviaQuestions.json","Here's all the questions as a JSON file.");
			}
		}
		[Command("setquestions")]
		[RequirePermission("triviasystem.manage")]
		[Summary("Replaces current questions with (string question -> string[] answers) dictionary from a JSON file.")]
		public async Task SetQuestionsCommand(string url = null)
		{
			var server = Context.server;
			if(!Context.socketMessage.Attachments.TryGetFirst(a => a.Filename.EndsWith(".txt") || a.Filename.EndsWith(".json"),out Attachment file) && url==null) {
				await Context.ReplyAsync("Expected a .json file attachment or a link to it.");
				return;
			}

			string filePath = MopBot.GetTempFileName("TriviaQuestions",".json");
			string urlString = file?.Url ?? url;

			if(!Uri.TryCreate(urlString,UriKind.Absolute,out Uri uri)) {
				await Context.ReplyAsync($"Invalid Url: `{urlString}`.");
				return;
			}

			using(var client = new WebClient()) {
				try {
					client.DownloadFile(uri,filePath);
				}
				catch(Exception e) {
					if(File.Exists(filePath)) {
						File.Delete(filePath);
					}
					throw new BotError($"`{e.GetType().Name}` exception has occured during file download.");
				}
			}

			var json = File.ReadAllText(filePath);

			File.Delete(filePath);

			Dictionary<string,string[]> dict;
			try {
				dict = JsonConvert.DeserializeObject<Dictionary<string,string[]>>(json);
			}
			catch(Exception e) {
				throw new BotError($"Failed to parse the JSON file: `{e.Message}`.");
			}

			if(dict==null) {
				throw new BotError("Failed to parse the JSON file: Unknown error.");
			}

			var triviaServerData = server.GetMemory().GetData<TriviaSystem,TriviaServerData>();
			var questions = triviaServerData.questions ?? (triviaServerData.questions = new List<TriviaQuestion>());

			foreach(var pair in dict) {
				var key = pair.Key;
				if(string.IsNullOrWhiteSpace(key)) {
					throw new BotError("Failed to parse the JSON file: Some question is null.");
				}
				var value = pair.Value;
				if(value==null || value.Length==0) {
					throw new BotError($"Failed to parse the JSON file: Question `{key}`'s answers are missing or are null.");
				}
				questions.Add(new TriviaQuestion(key,value));
			}
		}
		
		[Command("addquestion")]
		[RequirePermission("triviasystem.manage")]
		[Summary("Replaces current questions with (string question -> string[] answers) dictionary from a JSON file.")]
		public async Task AddQuestionCommand([Remainder]string questionAndAnswers)
		{
			var server = Context.server;
			var triviaServerData = server.GetMemory().GetData<TriviaSystem,TriviaServerData>();
			var questions = triviaServerData.questions ?? (triviaServerData.questions = new List<TriviaQuestion>());

			lock(questions) {
				var qaMatches = regexQuestionAndAnswers.Matches(questionAndAnswers);
				foreach(Match match in qaMatches) {
					string question = match.Groups[1].Value;
					var answers = regexAnswers.Matches(match.Groups[2].Value).Select(m => m.Groups[1].Value).ToArray();

					questions.Add(new TriviaQuestion(question,answers));
				}
			}
		}
		#endregion

		#region ConfigCommands
		//TODO: (!!!) Need proper configuration helper thing, to replace "setinterval" and "setchannel" commands with one...

		[Command("setchannel")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetChannelCommand(SocketTextChannel channel)
		{
			Context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().triviaChannel = channel.Id;
		}
		[Command("setrole")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetRoleCommand(SocketRole role)
		{
			var context = Context;
			context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().triviaRole = role?.Id ?? 0;
		}
		[Command("setinterval")] [Alias("setpostinterval")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetIntervalCommand(ulong intervalInSeconds)
		{
			if(intervalInSeconds<MinPostIntervalInSeconds) {
				throw new BotError($"Interval must be at least {MinPostIntervalInSeconds} seconds.");
			}

			var context = Context;
			context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().postIntervalInSeconds = intervalInSeconds;
		}
		[Command("setthumbnails")] [Alias("setimages")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetThumbnailUrlsCommand(params string[] urls)
		{
			var newUrls = new List<string>();

			for(int i = 0;i<urls.Length;i++) {
				string url = urls[i];
				if(!Uri.TryCreate(url,UriKind.Absolute,out var realUrl)) {
					throw new BotError($"Url #{i+1} is invalid");
				}
				newUrls.Add(realUrl.ToString());
			}
			
			var context = Context;
			context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().thumbnailUrls = newUrls.ToArray();
		}
		[Command("setlockchannel")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetLockChannelCommand(bool doLockChannel)
		{
			Context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().lockTriviaChannel = doLockChannel;
		}
		[Command("setautoclearcache")] [Alias("autoclearcache")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetAutoClearCacheCommand(bool doClearCacheAutomatically)
		{
			Context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().autoClearCache = doClearCacheAutomatically;
		}
		[Command("setcurrencyrewards")] [Alias("setrewards")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetCurrencyRewardsCommand([Remainder]string currencyAmountPairs = null)
		{
			var context = Context;
			var serverMemory = context.server.GetMemory();
			serverMemory.GetData<TriviaSystem,TriviaServerData>().currencyRewards = string.IsNullOrWhiteSpace(currencyAmountPairs) ? null : CurrencyAmount.ParseMultiple(currencyAmountPairs,serverMemory);
		}
		/*[Command("setcommand")]
		[RequirePermission("triviasystem.manage")]
		public async Task SetCommandCommand([Remainder]string command = null)
		{
			var context = Context;
			context.server.GetMemory().GetData<TriviaSystem,TriviaServerData>().command = string.IsNullOrWhiteSpace(command) ? null : new SudoCommand(command,context.user.Id);
		}*/
		#endregion
	}
}
