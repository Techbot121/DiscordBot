using Discord;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules.GImages
{
    internal class GImagesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private HttpService _http;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            manager.CreateCommands("", group =>
            {
                group.CreateCommand("gimage")
                .Parameter("query", ParameterType.Unparsed)
                .Description("Queries Google for an Image.")
                .Alias("gi")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var url = await GetImage(e.Args[0]);
                        await e.Channel.SendMessage(url);
                    }
                    else
                    {
                        await _client.ReplyError(e, "You need to specify what you want to search for.");
                    }
                });
            });
        }

        private async Task<string> GetImage(string txt)
        {
            var response = await Query(txt);
            var json = JsonConvert.DeserializeObject(response.ToString()) as JObject;
            var icount = json["items"].Count();
            Random rnd = new Random();
            var randin = rnd.Next(0, icount);
            return (string)json["items"][randin]["link"];
        }

        private async Task<string> Query(string txt)
        {
            string query = WebUtility.HtmlEncode(txt = Encoding.UTF8.GetString(Encoding.Default.GetBytes(txt)));
            HttpContent content = null;
            try
            {
                content = await _http.Send(
                    HttpMethod.Get,
                    $"https://www.googleapis.com/customsearch/v1?key={GlobalSettings.Google.ApiKey}&cx=000079114482041444970:3mn1iffeztu&searchType=image&q={query}"
                    );
            }
            catch (WebException ex)
            {
                _client.Log.Error("GImage", "Couldn't Query Google " + ex);
            }
            return await content.ReadAsStringAsync();
        }
    }
}