﻿#pragma warning disable CS1998 //The async method lacks 'await' operator.

using System;

namespace MopBot.Core.Systems.Permissions
{
	[Flags]
	public enum SpecialPermission : byte
	{
		Owner,
		Admin,
		BotMaster
	}
}
