using System;
using System.Collections.Generic;

namespace MopBotTwo
{
	public static class StringUtils
	{
		public static string ChangeForm(string str,bool singular) => singular ? GetSingular(str) : GetPlural(str);
		public static string GetSingular(string str) => (str.Length<=1 || !str.EndsWith('s')) ? str : str.Substring(0,str.Length-1);
		public static string GetPlural(string str) => str.EndsWith('s') ? str : str+'s';

		public static void RemoveQuotemarks(ref string str)
		{
			int length = str.Length;
			if(length<2) {
				return;
			}

			const char Quotemark = '"';

			if(str[0]==Quotemark && str[length-1]==Quotemark) {
				str = length==2 ? "" : str.Substring(1,length-2);
			}
		}

		public static void CheckAndLowerStringId(ref string id)
		{
			int length = id.Length;
			char[] newChars = null;
			for(int i = 0;i<length;i++) {
				char c = id[i];
				if(!char.IsLetterOrDigit(c)) {
					throw new BotError($"Ids can only contain letters and digits, with no spaces.");
				}
				if(char.IsUpper(c)) {
					(newChars ?? (newChars = id.ToCharArray()))[i] = char.ToLower(c);
				}
			}
			if(newChars!=null) {
				id = new string(newChars);
			}
		}

		public static string SubstringSafe(string str,int startIndex,int length)
		{
			int end = Math.Min(startIndex+length,str.Length);
			string result = "";
			for(int i = startIndex;i<end;i++) {
				result += str[i];
			}
			return result;
		}
		public static string[] SplitMessageText(string allText)
		{
			//very old, very bad, does work.

			const int TrySplitAt = 1500;
			const int ForceSplitAt = 1700;
			const string Tilde = "`";
			const string TripleTilde = "```";
			const string LineBreak = "\r\n";
			const string LineBreak2 = "\n\r";

			bool codeLine = false;
			bool codeBlock = false;
			var result = new List<string>();

			for(int i = 0;i<allText.Length;i++) {
				if(allText.Length<=ForceSplitAt) {
					result.Add(allText);
					break;
				}

				if(i>=TrySplitAt) {
					string sub = SubstringSafe(allText,i,LineBreak.Length);
					if(i>=ForceSplitAt || sub==LineBreak || sub==LineBreak2) {
						if(codeBlock) {
							allText = allText.Insert(i,TripleTilde);
							i += TripleTilde.Length;
						}

						if(codeLine) {
							allText = allText.Insert(i,Tilde);
							i += Tilde.Length;
						}

						result.Add(allText.Substring(0,i));
						allText = allText.Substring(i);
						i = 0;

						if(codeBlock) {
							allText = allText.Insert(i,TripleTilde);
							i += TripleTilde.Length;
						}

						if(codeLine) {
							allText = allText.Insert(i,Tilde);
							i += Tilde.Length;
						}

						i--;
						continue;
					}
				}

				if(SubstringSafe(allText,i,TripleTilde.Length)==TripleTilde) {
					i += TripleTilde.Length;
					codeBlock = !codeBlock;
					codeLine = false;
					continue;
				} else if(!codeBlock && allText[i]==Tilde[0]) {
					codeLine = !codeLine;
				}
			}
			return result.ToArray();
		}
	}
}