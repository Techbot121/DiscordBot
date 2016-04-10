using Discord;
using Discord.Commands;
using Discord.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                .Parameter("method", ParameterType.Optional)
                .Parameter("amount", ParameterType.Optional)
                .Parameter("query", ParameterType.Unparsed)
                .Alias("gi")
                .Do(async e =>
                {
                    int amt = 0;
                    Methods res;

                    var hasAmount = int.TryParse(e.Args[1], out amt);

                    if (Enum.TryParse(e.Args[0], true, out res))
                    {
                        if (hasAmount)
                            await GetImage(e.Args[2], e, res, amt);
                        else
                            await GetImage(e.Args[1], e, res);
                    }
                    else
                    {
                        await GetImage(e.Args[0], e, Methods.First);
                        //todo how to avoid gimages [number] [method] [text] ?
                    }

                    //todo: add Syntax checking?
                });
            });
        }

        private async Task GetImage(string txt, CommandEventArgs e, Methods method, int amount = 0)
        {
            JObject json;
            string url = "";

            switch (method)
            {
                case Methods.First:
                    {
                        var response = await Query(txt);
                        json = JObject.Parse(response.ToString());
                        url = (string)json["items"][0]["link"];

                        break;
                    }
                case Methods.Exact:
                    {
                        var response = await Query(txt);
                        json = JsonConvert.DeserializeObject(response.ToString()) as JObject;
                        var icount = json["items"].Count();
                        var amt = (amount > icount) ? icount : amount;
                        url = (string)json["items"][amt]["link"];

                        break;
                    }
                case Methods.Random:
                    {
                        var response = await Query(txt);
                        json = JsonConvert.DeserializeObject(response.ToString()) as JObject;
                        var icount = json["items"].Count();
                        Random rnd = new Random();
                        var randin = rnd.Next(0, icount);
                        url = (string)json["items"][randin]["link"];

                        break;
                    }
            }
            await _client.Reply(e, url);
        }

        private enum Methods
        {
            First,
            Exact,
            Random
        }

        private async Task<string> Query(string txt)
        {
            string query = WebUtility.HtmlEncode(txt);
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