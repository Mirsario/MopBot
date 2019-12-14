using Discord;


namespace MopBotTwo.Core.Systems.Status
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
}