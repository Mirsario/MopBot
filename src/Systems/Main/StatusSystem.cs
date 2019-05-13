using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;

#pragma warning disable CS1998

namespace MopBotTwo.Systems
{
	public struct Activity
	{
		public ActivityType type;
		public string name;

		public Activity(ActivityType type,string name)
		{
			this.type = type;
			this.name = name;
		}
	}

	[SystemConfiguration(AlwaysEnabled = true)]
	public class StatusSystem : BotSystem
	{
		private static List<Activity> activities = new List<Activity>() {
			new Activity(ActivityType.Playing,"a Mopster"),
			new Activity(ActivityType.Playing,"Janitor Class"),
			new Activity(ActivityType.Watching,"a Bucket"),
		};
		public static UserStatus currentStatus = UserStatus.Online;
		public static Activity currentActivity;
		public static bool noActivityChanging;
		public DateTime lastActivityChange;

		public override async Task<bool> Update()
		{
			var client = MopBot.client;
			var now = DateTime.Now;
			if(!noActivityChanging && (currentActivity.name==null || (now-lastActivityChange).TotalMinutes>=5)) {
				int index,indexOf = activities.IndexOf(currentActivity);
				while((index = MopBot.random.Next(activities.Count))==indexOf) {}
				currentActivity = activities[index];
				lastActivityChange = now;
			}
			var user = client.CurrentUser;
			if(user==null) {
				return true;
			}
			var activity = user.Activity;
			if(activity?.Name!=currentActivity.name || activity?.Type!=currentActivity.type) {
				await client.SetGameAsync(currentActivity.name,type:currentActivity.type);
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