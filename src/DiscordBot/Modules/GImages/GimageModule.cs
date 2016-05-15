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

        Random rnd = new Random();

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            manager.CreateCommands("g", group =>
            {
                group.CreateCommand("image")
                .Parameter("query", ParameterType.Unparsed)
                .Description("Queries Google for an Image.")
                .Alias("i")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var url = await GetImage(e.Args[0],true);
                        await e.Channel.SendMessage(url);
                    }
                    else
                    {
                        await _client.ReplyError(e, "You need to specify what you want to search for.");
                    }
                });
                group.CreateCommand("first")
                .Parameter("query", ParameterType.Unparsed)
                .Description("Queries Google for an Image.")
                .Alias("if")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var url = await GetImage(e.Args[0],false);
                        await e.Channel.SendMessage(url);
                    }
                    else
                    {
                        await _client.ReplyError(e, "You need to specify what you want to search for.");
                    }
                });
            });
        }

        private async Task<string> GetImage(string txt,bool random)
        {
            var response = await Query(txt);
            var json = JsonConvert.DeserializeObject(response.ToString()) as JObject;
            if (random)
            {
                var icount = json["items"].Count();
                var randin = rnd.Next(icount);
                return (string)json["items"][randin]["link"];
            }
            else
            {
                return (string)json["items"][0]["link"];
            }
        }

        private async Task<string> Query(string txt)
        {
            string query = Uri.EscapeDataString(txt);
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