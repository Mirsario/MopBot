using System.Collections.Generic;
using MopBot.Core.Systems.Memory;

#pragma warning disable 1998

namespace MopBot.Common.Systems.Tags
{
	public class TagGlobalData : GlobalData
	{
		public Dictionary<ulong,TagGroup> tagGroups = new Dictionary<ulong,TagGroup>();
		public Dictionary<ulong,Tag> tags = new Dictionary<ulong,Tag>();
	}
}