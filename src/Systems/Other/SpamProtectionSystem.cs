using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Discord.WebSocket;
using MopBotTwo.Extensions;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public class SpamProtectionSystem : BotSystem
	{
		public class SpamProtectionServerData : ServerData
		{
			public float muteTimeInSeconds = 10f;
			public float spamDetectionTime = 3f;
			public ushort spamDetectionNumMessages = 3;

			public override void Initialize(SocketGuild server) {}
		}

		public static ConcurrentDictionary<ulong,List<DateTime>> userMessageDates;

		public override async Task Initialize()
		{
			userMessageDates = new ConcurrentDictionary<ulong,List<DateTime>>();
		}

		public override void RegisterDataTypes()
		{
			RegisterDataType<ServerMemory,SpamProtectionServerData>();
		}

		public override async Task OnMessageReceived(MessageExt message)
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

			if(!userMessageDates.TryGetValue(user.Id,out var list)) {
				userMessageDates[user.Id] = list = new List<DateTime>();
			}else{
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
				//Mute
				await message.ReplyAsync("Don't spam, fool.");
			}
		}
	}
}