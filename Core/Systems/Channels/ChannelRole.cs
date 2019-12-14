#pragma warning disable CS1998 //The async method lacks 'await' operator.

namespace MopBotTwo.Core.Systems.Channels
{
	//TODO: Tbh, remove this, or rewrite without the enum.
	public enum ChannelRole
	{
		Default,
		Welcome, //TODO: Get rid of this.
		Logs,
		BotArea,
		Rules
	}
}