using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json;
using MopBot.Core.Systems.Memory;
using MopBot.Core;

#pragma warning disable CS1998 //Async method lacks 'await' operators and will run synchronously

namespace MopBot.Common.Systems.Changelogs
{
	public class ChangelogServerData : ServerData
	{
		public ulong changelogChannel;
		public Dictionary<string,ChangelogEntryType> entryTypes;
		public List<ChangelogEntry> entries;
		public string currentVersion;
		public uint nextEntryId;
		
		[JsonIgnore]
		public uint NextEntryId {
			get {
				if(entries!=null && entries.Any(i => i.entryId==nextEntryId)) {
					nextEntryId = entries.Max(i => i.entryId)+1;
				}

				return nextEntryId;
			}
		}

		public override void Initialize(SocketGuild server)
		{
			entryTypes = new Dictionary<string,ChangelogEntryType> {
				{ "added",			new ChangelogEntryType("Added",":new:") },
				{ "changed",		new ChangelogEntryType("Changed",":asterisk:") },
				{ "fixed",			new ChangelogEntryType("Fixed",":hammer:") },
				{ "optimization",	new ChangelogEntryType("Optimization",":zap:") },
				{ "removed",		new ChangelogEntryType("Removed",":gun:") },
			};
			entries = new List<ChangelogEntry>();
		}

		public ChangelogEntry NewEntry(string type,string text)
		{
			var newEntry = new ChangelogEntry(NextEntryId,type,text);

			nextEntryId++;

			entries.Add(newEntry);

			return newEntry;
		}
		public bool GetChangelogChannel(out SocketTextChannel channel)
		{
			if(changelogChannel==0) {
				channel = null;
				return false;
			}

			channel = (SocketTextChannel)MopBot.client.GetChannel(changelogChannel);

			return channel!=null;
		}

		public async Task UnpublishEntry(ChangelogEntry entry,SocketGuild server)
		{
			if(entry.messageId==0 || entry.channelId==0) {
				return;
			}

			var oldChannel = server.GetChannel(entry.channelId);

			if(oldChannel!=null && oldChannel is SocketTextChannel oldTextChannel) {
				var oldMessage = await oldTextChannel.GetMessageAsync(entry.messageId);

				if(oldMessage!=null) {
					await oldMessage.DeleteAsync();
				}
			}
		}
		public async Task PublishEntry(ChangelogEntry entry,SocketTextChannel channel)
		{
			await UnpublishEntry(entry,channel.Guild);

			if(!entryTypes.TryGetValue(entry.type,out var entryType)) {
				throw new BotError($"Unknown entry type: `{entry.type}`.");
			}

			var message = await channel.SendMessageAsync($"{entryType.discordPrefix} - #**{entry.entryId}** - **{entryType.name}:** {entry.text}",options:MopBot.optAlwaysRetry);
			
			entry.messageId = message.Id;
			entry.channelId = channel.Id;
		}
		public async Task<SocketTextChannel> TryGetChangelogChannel(MessageContext context,bool showError = true)
		{
			var result = changelogChannel!=0 ? (SocketTextChannel)MopBot.client.GetChannel(changelogChannel) : null;

			if(result==null && showError) {
				throw new BotError("Changelog channel has not been set. Set it with `!cl setchannel <channel>` first.");
			}

			return result;
		}
	}
}