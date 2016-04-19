using Discord;
using Discord.Commands;
using Discord.Modules;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Youtube
{
    internal class YoutubeModule : IModule
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
                group.CreateCommand("youtube")
                .Parameter("query", ParameterType.Unparsed)
                .Description("Queries Youtube for a Video/Channel/Playlist.\nAFTER `|` SEPERATOR EITHER : \n`Amount in Numbers` specifies how many Results should be returned (5 max).\n`Type as Channel or Playlist` narrow down your search by filtering by Channels or Playlists")
                .Alias("yt")
                .Do(async e =>
                {
                    var query = e.Args[0];

                    if (query.Any())
                    {
                        if (query.Contains("|"))
                        {
                            var split = query.Split('|');
                            string arg = split[1];
                            int amount;

                            if (int.TryParse(arg, out amount))
                            {
                                if (amount <= 5)
                                {
                                    var vurls = await GetVideo(query, amount);
                                    await e.Channel.SendMessage(vurls.ToString());
                                    return;
                                }
                                else
                                {
                                    await _client.ReplyError(e, "Sorry, I can only return 5 results at once.");
                                    return;
                                }
                            }

                            if (string.Equals(arg, "playlist") || string.Equals(arg, "channel"))
                            {
                                //todo: Implement multiple playlist/channel replies?
                                var vurls = await GetVideo(query, 0, ParseEnum<vType>(arg));
                                await e.Channel.SendMessage(vurls.ToString());
                                return;
                            }
                            else
                            {
                                await _client.ReplyError(e, "Either you didn't enter a number to specify the amount, or your type is wrong! I can only search for `Channel` or `Playlist`.");
                                return;
                            }
                        }
                        else
                        {
                            var vurls = await GetVideo(query);
                            await e.Channel.SendMessage(vurls.ToString());
                        }
                    }
                    else
                    {
                        await _client.ReplyError(e, "You need to specify what you want to search for.");
                    }
                });
            });
        }

        private async Task<string> GetVideo(string txt, int amount = 0, vType type = vType.Video)
        {
            var ys = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = GlobalSettings.Youtube.ApiKey,
                ApplicationName = "Hal1320 Youtube Module"
            });

            string query = txt;

            var req = ys.Search.List("snippet");
            req.Q = query;
            req.MaxResults = 50;

            var resp = await req.ExecuteAsync();

            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            foreach (var searchResult in resp.Items)
            {
                switch (searchResult.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(searchResult.Id.VideoId);
                        break;

                    case "youtube#channel":
                        channels.Add(searchResult.Id.ChannelId);
                        break;

                    case "youtube#playlist":
                        playlists.Add(searchResult.Id.PlaylistId);
                        break;
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.Clear();

            if (amount > 0)
            {
                switch (type)
                {
                    case vType.Video:
                        if (videos.Count > 0)
                            for (int i = 0; i < amount; i++)
                            {
                                if (videos.Count >= i)
                                    sb.Append($"https://youtu.be/{videos[i]}");
                                    sb.AppendLine();
                            }
                        else
                            sb.Append($"No Videos called {query} found.");
                        break;

                    case vType.Channel:
                        break;

                    case vType.Playlist:
                        break;

                    default:
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case vType.Video:
                        if (videos.Count > 0)
                            sb.Append($"https://youtu.be/{videos[0]}");
                        else
                            sb.Append($"No Videos called {query} found.");
                        break;

                    case vType.Channel:
                        if (videos.Count > 0)
                            sb.Append($"https://www.youtube.com/channel/{channels[0]}");
                        else
                            sb.Append($"No Channels called {query} found.");
                        break;

                    case vType.Playlist:
                        if (videos.Count > 0)
                            sb.Append($"https://www.youtube.com/playlist?list={playlists[0]}");
                        else
                            sb.Append($"No Playlist called {query} found.");
                        break;

                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        private enum vType
        {
            Video,
            Channel,
            Playlist
        }

        public static T ParseEnum<T>(string value) => (T)Enum.Parse(typeof(T), value, true);
    }
}