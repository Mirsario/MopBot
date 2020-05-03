using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;

namespace MopBot.Core.Systems.Status
{
	[SystemConfiguration(AlwaysEnabled = true,Description = "Manages bot's activity status, i.e. what it's 'playing'.")]
	public class StatusSystem : BotSystem
	{
		public static UserStatus currentStatus = UserStatus.Online;
		public static Activity currentActivity;
		public static bool noActivityChanging;

		private static List<Activity> activities = new List<Activity>() {
			new Activity(ActivityType.Playing,"a Mopster"),
			new Activity(ActivityType.Playing,"Janitor Class"),
			new Activity(ActivityType.Watching,"a Bucket"),
		};

		public DateTime lastActivityChange;

		public override async Task<bool> Update()
		{
			var client = MopBot.client;
			var now = DateTime.Now;

			if(!noActivityChanging && (currentActivity.name==null || (now-lastActivityChange).TotalMinutes>=5)) {
				int index,indexOf = activities.IndexOf(currentActivity);

				while((index = MopBot.Random.Next(activities.Count))==indexOf) { }

				currentActivity = activities[index];
				lastActivityChange = now;
			}

			var user = client.CurrentUser;

			if(user==null) {
				return true;
			}

			var activity = user.Activity;

			if(activity?.Name!=currentActivity.name || activity?.Type!=currentActivity.type) {
				await client.SetGameAsync(currentActivity.name,type: currentActivity.type);
			}

			if(user.Status!=currentStatus) {
				await client.SetStatusAsync(currentStatus);
			}

			return true;
		}

		public static void ForceStatus(ActivityType? activityType,string activityName,UserStatus? status,bool? noActivityChanging)
		{
			if(activityType!=null) {
				currentActivity.type = activityType.Value;
			}

			if(activityName!=null) {
				currentActivity.name = activityName;
			}

			if(status!=null) {
				currentStatus = status.Value;
			}

			if(noActivityChanging!=null) {
				StatusSystem.noActivityChanging = noActivityChanging.Value;
			}

			Task.Run(async () => { await MopBot.instance.systems.First(s => s.GetType()==typeof(StatusSystem)).Update(); }).Wait();
		}
	}
}