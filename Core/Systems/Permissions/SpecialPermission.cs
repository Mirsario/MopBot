#pragma warning disable CS1998 //The async method lacks 'await' operator.

using System;

namespace MopBotTwo.Core.Systems.Permissions
{
	[Flags]
	public enum SpecialPermission : byte
	{
		Owner = 1,
		BotMaster = 2
	}
}