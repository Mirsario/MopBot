using System;
using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json;
using MopBotTwo.Core.Systems.Memory;
using MopBotTwo.Common.Systems.Currency;

namespace MopBotTwo.Common.Systems.Trivia
{
	public class TriviaServerData : ServerData
	{
		public ulong currentChannel;
		public TriviaQuestion currentQuestion;
		public List<TriviaQuestion> questions; //Not thread safe

		public bool lockTriviaChannel;
		public bool autoClearCache;
		public ulong triviaRole;
		public ulong postIntervalInSeconds = 5*60;
		public DateTime lastTriviaPost;
		public string[] thumbnailUrls;
		public List<ulong> triviaChannels;
		//public SudoCommand command;
		public CurrencyAmount[] currencyRewards;

		[JsonIgnore] public bool IsReady => postIntervalInSeconds>=TriviaSystem.MinPostIntervalInSeconds && triviaChannels!=null && triviaChannels.Count!=0 && questions!=null && questions.Count!=0;

		public override void Initialize(SocketGuild server) { }
	}
}
