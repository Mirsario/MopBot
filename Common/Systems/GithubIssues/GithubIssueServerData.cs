/*using System.Text.RegularExpressions;
using MopBotTwo.Core.Systems.Memory;
using Newtonsoft.Json;

namespace MopBotTwo.Common.Systems.Issues
{
	public class GithubIssueServerData : ServerData
	{
		private static readonly Regex RepositoryRegex = new Regex(@"([\w-.]+)\/([\w-.]+)",RegexOptions.Compiled);

		public string repository;

		[JsonIgnore]
		public bool IsRepositoryValid => !string.IsNullOrWhiteSpace(repository) && RepositoryRegex.IsMatch(repository);
	}
}*/