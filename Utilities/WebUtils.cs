using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MopBot.Utilities
{
	public static class WebUtils
	{
		public static async Task<string> DownloadString(string url)
		{
			if(!Uri.TryCreate(url,UriKind.Absolute,out var realUrl)) {
				throw new BotError("Invalid url.");
			}

			using var client = new WebClient();

			try {
				return await client.DownloadStringTaskAsync(realUrl);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}
		public static async Task DownloadFile(string url,string localPath,string[] validExtensions = null)
		{
			if(!Uri.TryCreate(url,UriKind.Absolute,out var realUrl)) {
				throw new BotError("Invalid url.");
			}

			if(validExtensions!=null && validExtensions.Any(ext => string.IsNullOrWhiteSpace(new FileInfo(realUrl.AbsolutePath).Extension))) {
				throw new BotError($"Url has no extension, or it's forbidden. The following extensions are allowed: {string.Join(",",validExtensions.Select(ext => ext.ToString()))}.");
			}

			using var client = new WebClient();

			try {
				await client.DownloadFileTaskAsync(realUrl,localPath);
			}
			catch(Exception e) {
				throw new BotError(e);
			}
		}
	}
}
