using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MopBot.Utilities
{
	public static class WildcardUtils
	{
		private static readonly Dictionary<string, Regex> wildcardToRegexCache = new Dictionary<string, Regex>();

		public static bool IsMatch(string pattern, string input)
		{
			return ToRegex(pattern).IsMatch(input);
		}

		public static Regex ToRegex(string pattern)
		{
			if (!wildcardToRegexCache.TryGetValue(pattern, out var regex)) {
				wildcardToRegexCache[pattern] = regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*?") + "$", RegexOptions.Compiled);
			}

			return regex;
		}
	}
}
