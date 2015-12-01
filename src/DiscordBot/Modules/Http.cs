using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Reflection;
using System.Net;
using System.Net.Http.Headers;
using Discord;

namespace DiscordBot
{
	public static class Http
	{
		private static readonly HttpClient _client;

		static Http()
		{
			_client = new HttpClient(new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				UseCookies = false,
				PreAuthenticate = false //We do auth ourselves
			});
			_client.DefaultRequestHeaders.Add("accept", "*/*");
			_client.DefaultRequestHeaders.Add("accept-encoding", "gzip, deflate");			
			_client.DefaultRequestHeaders.Add("user-agent", $"DiscordBot/{DiscordClient.Version} (https://github.com/RogueException/Discord.Net)");
		}

		public static Task<HttpContent> Send(HttpMethod method, string path, string authToken = null)
			=> Send<object>(method, path, null, authToken);
        public static async Task<HttpContent> Send<T>(HttpMethod method, string path, T payload, string authToken = null)
			where T : class
		{
			HttpRequestMessage msg = new HttpRequestMessage(method, path);

			if (authToken != null)
				msg.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);
			if (payload != null)
			{
				string json = JsonConvert.SerializeObject(payload);
				msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
			}
			
			var response = await _client.SendAsync(msg, HttpCompletionOption.ResponseContentRead);
			if (!response.IsSuccessStatusCode)
				throw new HttpException(response.StatusCode);
			return response.Content;
        }
	}
}
