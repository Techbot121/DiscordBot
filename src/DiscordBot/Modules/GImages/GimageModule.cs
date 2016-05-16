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

            _client.GetService<CommandService>().CreateGroup("asd" );

            manager.CreateCommands("google", group =>
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
                group.CreateCommand("firstimage")
                .Parameter("query", ParameterType.Unparsed)
                .Description("Queries Google for the first Image.")
                .Alias("first")
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
                group.CreateCommand("")
                .Description("Queries Google.")
                .Do(async e =>
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("Example Usage:");
                    sb.AppendLine("`google [image/i] trees`");
                    sb.AppendLine("To only return the first found result use:");
                    sb.AppendLine("`google [firstimage/first/f] trees`");

                    await _client.Reply(e,sb.ToString());
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