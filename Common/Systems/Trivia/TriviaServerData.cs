﻿using System;
using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json;
using MopBot.Core.Systems.Memory;
using MopBot.Common.Systems.Currency;

namespace MopBot.Common.Systems.Trivia
{
	public class TriviaServerData : ServerData
	{
		public ulong currentChannel;
		public TriviaQuestion currentQuestion;
		public List<TriviaQuestion> questions; //Not thread safe

		public bool lockTriviaChannel;
		public bool autoClearCache;
		public ulong triviaRole;
		public ulong postIntervalInSeconds = 5 * 60;
		public DateTime lastTriviaPost;
		public string[] thumbnailUrls;
		public List<ulong> triviaChannels;
		//public SudoCommand command;
		public CurrencyAmount[] currencyRewards;

		[JsonIgnore]
		public bool IsReady {
			get {
				if (postIntervalInSeconds < TriviaSystem.MinPostIntervalInSeconds) {
					return false;
				}

				if (triviaChannels == null || triviaChannels.Count == 0) {
					return false;
				}

				if (questions == null || questions.Count == 0) {
					return false;
				}

				return true;
			}
		}

		public override void Initialize(SocketGuild server) { }
	}
}
