﻿using System.Collections.Generic;
using MopBot.Core.Systems.Memory;

#pragma warning disable 1998

namespace MopBot.Common.Systems.Tags
{
	public class TagUserData : UserData
	{
		public List<ulong> subscribedTags = new List<ulong>();
		public List<ulong> subscribedTagGroups = new List<ulong>();
	}
}