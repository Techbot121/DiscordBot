using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Net;
using DiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DiscordBot.Modules.Feeds
{
    internal class FeedModule : IModule
    {
        public class Article
        {
            public string Title;
            public string Link;
            public DateTimeOffset PublishedAt;
        }

        private ModuleManager _manager;
        private DiscordClient _client;
        private bool _isRunning;
        private HttpService _http;
        private SettingsManager<Settings> _settings;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();
            _settings = _client.GetService<SettingsService>()
                .AddModule<FeedModule, Settings>(manager);

            manager.CreateCommands("feeds", group =>
            {
                group.MinPermissions((int)PermissionLevel.BotOwner);

                group.CreateCommand("list")
                    .Do(async e =>
                    {
                        var settings = _settings.Load(e.Server);
                        var response = settings.Feeds
                            .OrderBy(x => x.Key)
                            .Select(x => $"{x.Key} => {_client.GetChannel(x.Value.ChannelId)?.Name ?? "Unknown"}");
                        await _client.Reply(e, "Linked Feeds", response);
                    });

                group.CreateCommand("add")
                    .Parameter("url")
                    .Parameter("channel", ParameterType.Optional)
                    .Do(async e =>
                    {
                        var settings = _settings.Load(e.Server);
                        Channel channel;
                        if (e.Args[1] != "")
                            channel = await _client.FindChannel(e, e.Args[1], ChannelType.Text);
                        else
                            channel = e.Channel;
                        if (channel == null) return;

                        if (settings.AddFeed(e.Args[0], channel.Id))
                        {
                            await _settings.Save(e.Server, settings);
                            await _client.Reply(e, $"Linked feed {e.Args[0]} to {channel.Name}");
                        }
                        else
                            await _client.Reply(e, $"Feed {e.Args[0]} is already linked to a channel.");
                    });

                group.CreateCommand("remove")
                    .Parameter("url")
                    .Do(async e =>
                    {
                        var settings = _settings.Load(e.Server);
                        if (settings.RemoveFeed(e.Args[0]))
                        {
                            await _settings.Save(e.Server, settings);
                            await _client.Reply(e, $"Unlinked feed {e.Args[0]}.");
                        }
                        else
                            await _client.Reply(e, $"Feed {e.Args[0]} is not currently linked to a channel.");
                    });
            });

            _client.Ready += (s, e) =>
            {
                if (!_isRunning)
                {
                    Task.Run(Run);
                    _isRunning = true;
                }
            };
        }

        public async Task Run()
        {
            var cancelToken = _client.CancelToken;
            try
            {
                while (!_client.CancelToken.IsCancellationRequested)
                {
                    foreach (var settings in _settings.AllServers)
                    {
                        foreach (var feed in settings.Value.Feeds)
                        {
                            try
                            {
                                var channel = _client.GetChannel(feed.Value.ChannelId);
                                if (channel != null && channel.Server.CurrentUser.GetPermissions(channel).SendMessages)
                                {
                                    var content = await _http.Send(HttpMethod.Get, feed.Key);
                                    var doc = XDocument.Load(await content.ReadAsStreamAsync());
                                    var rssNode = doc.Element("rss");
                                    var atomNode = doc.Element("{http://www.w3.org/2005/Atom}feed");

                                    IEnumerable<Article> articles;
                                    if (rssNode != null)
                                    {
                                        articles = rssNode
                                            .Element("channel")
                                            .Elements("item")
                                            .Select(x => new Article
                                            {
                                                Title = x.Element("title")?.Value,
                                                Link = x.Element("link")?.Value,
                                                PublishedAt = DateTimeOffset.Parse(x.Element("pubDate").Value)
                                            });
                                    }
                                    else if (atomNode != null)
                                    {
                                        articles = atomNode
                                            .Elements("{http://www.w3.org/2005/Atom}entry")
                                            .Select(x => new Article
                                            {
                                                Title = x.Element("{http://www.w3.org/2005/Atom}title")?.Value,
                                                Link = x.Element("{http://www.w3.org/2005/Atom}link")?.Attribute("href")?.Value,
                                                PublishedAt = DateTimeOffset.Parse(x.Element("{http://www.w3.org/2005/Atom}published").Value)
                                            });
                                    }
                                    else
                                        throw new InvalidOperationException("Unknown feed type.");

                                    articles = articles
                                        .Where(x => x.PublishedAt > feed.Value.LastUpdate)
                                        .OrderBy(x => x.PublishedAt)
                                        .ToArray();

                                    foreach (var article in articles)
                                    {
                                        _client.Log.Info("Feed", $"New article: {article.Title}");
                                        if (article.Link != null)
                                        {
                                            try
                                            {
                                                await channel.SendMessage(Format.Escape(article.Link));
                                            }
                                            catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden) { }
                                        }

                                        if (article.PublishedAt > feed.Value.LastUpdate)
                                        {
                                            feed.Value.LastUpdate = article.PublishedAt;
                                            await _settings.Save(settings.Key, settings.Value);
                                        }
                                    };
                                }
                            }
                            catch (Exception ex) when (!(ex is TaskCanceledException))
                            {
                                _client.Log.Error("Feed", ex);
                            }
                        }
                    }
                    await Task.Delay(1000 * 300, cancelToken); //Wait 5 minutes between updates
                }
            }
            catch (TaskCanceledException) { }
        }
    }
}
