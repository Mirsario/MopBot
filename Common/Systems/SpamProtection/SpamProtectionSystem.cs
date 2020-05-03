using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MopBot.Extensions;
using MopBot.Core.Systems;
using MopBot.Core.Systems.Memory;
using MopBot.Core;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.SpamProtection
{
	[SystemConfiguration(Description = "Unfinished. Detects when people flood the chat, but doesn't do anything about it yet, as there isn't a muting system.")]
	public class SpamProtectionSystem : BotSystem
	{
		public static ConcurrentDictionary<ulong,List<DateTime>> userMessageDates;

		public override async Task Initialize()
		{
			userMessageDates = new ConcurrentDictionary<ulong,List<DateTime>>();
		}
		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,SpamProtectionServerData>();
		}
		public override async Task OnMessageReceived(MessageContext message)
		{
			var user = message.socketServerUser;

			if(user==null || user.IsBot) {
				return;
			}

			var server = message.server;
			
			if(server==null) {
				return;
			}

			if(user.HasAnyPermissions("spamprotection.immune")) {
				return;
			}

			var userId = user.Id;
			var utcNow = DateTime.UtcNow;
			var serverData = server.GetMemory().GetData<SpamProtectionSystem,SpamProtectionServerData>();

			int numMessages = 1;

			if(!userMessageDates.TryGetValue(userId,out var list)) {
				userMessageDates[userId] = list = new List<DateTime>();
			} else {
				for(int i = 0;i<list.Count;i++) {
					var date = list[i];
					var totalSince = (utcNow-date).TotalSeconds;

					if(totalSince>=serverData.spamDetectionTime) {
						list.RemoveAt(i--);
						continue;
					}

					numMessages++;
				}
			}

			list.Add(message.message.Timestamp.UtcDateTime);

			if(numMessages>=serverData.spamDetectionNumMessages) {
				//TODO: Mute
				await message.ReplyAsync("Don't spam, fool.");
			}
		}
	}
}