using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Net;
using DiscordBot.Services;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using Discord.API.Client.Rest;

namespace DiscordBot.Modules.Twitch
{
    public class TwitchModule : IModule
    {
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
                .AddModule<TwitchModule, Settings>(manager);

            manager.CreateCommands("streams", group =>
            {
                group.MinPermissions((int)PermissionLevel.BotOwner);

                group.CreateCommand("list")
                    .Do(async e =>
                    {
                        StringBuilder builder = new StringBuilder();
                        var settings = _settings.Load(e.Server);
                        foreach (var channel in settings.Channels)
                            builder.AppendLine($"{_client.GetChannel(channel.Key)}:\n   {string.Join(", ", channel.Value.Streams.Select(x => x.Key))}");
                        await _client.Reply(e, "Linked Streams", builder.ToString());
                    });

                group.CreateCommand("add")
                    .Parameter("twitchuser")
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

                        var channelSettings = settings.GetOrAddChannel(channel.Id);
                        if (channelSettings.AddStream(e.Args[0]))
                        {
                            await _settings.Save(e.Server, settings);
                            await _client.Reply(e, $"Linked stream {e.Args[0]} to {channel.Name}.");
                        }
                        else
                            await _client.Reply(e, $"Stream {e.Args[0]} is already linked to {channel.Name}.");
                    });

                group.CreateCommand("remove")
                    .Parameter("twitchuser")
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

                        var channelSettings = settings.GetOrAddChannel(channel.Id);
                        if (channelSettings.RemoveStream(e.Args[0]))
                        {
                            await _settings.Save(e.Server.Id, settings);
                            await _client.Reply(e, $"Unlinked stream {e.Args[0]} from {channel.Name}.");
                        }
                        else
                            await _client.Reply(e, $"Stream {e.Args[0]} is not currently linked to {channel.Name}.");
                    });

                group.CreateGroup("set", set =>
                {
                    set.CreateCommand("sticky")
                        .Parameter("value")
                        .Parameter("channel", ParameterType.Optional)
                        .Do(async e =>
                        {
                            bool value = false;
                            bool.TryParse(e.Args[0], out value);

                            Channel channel;
                            if (e.Args[1] != "")
                                channel = await _client.FindChannel(e, e.Args[1], ChannelType.Text);
                            else
                                channel = e.Channel;
                            if (channel == null) return;

                            var settings = _settings.Load(e.Server);
                            var channelSettings = settings.GetOrAddChannel(channel.Id);
                            if (channelSettings.UseSticky && !value && channelSettings.StickyMessageId != null)
                            {
                                var msg = channel.GetMessage(channelSettings.StickyMessageId.Value);
                                try { await msg.Delete(); }
                                catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                            }
                            channelSettings.UseSticky = value;
                            await _settings.Save(e.Server, settings);
                            await _client.Reply(e, $"Stream sticky for {channel.Name} set to {value}.");
                        });
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
            StringBuilder builder = new StringBuilder();

            try
            {
                while (!_client.CancelToken.IsCancellationRequested)
                {
                    foreach (var settings in _settings.AllServers)
                    {
                        bool isServerUpdated = false;
                        foreach (var channelSettings in settings.Value.Channels)
                        {
                            bool isChannelUpdated = false;
                            var channel = _client.GetChannel(channelSettings.Key);
                            if (channel != null && channel.Server.CurrentUser.GetPermissions(channel).SendMessages)
                            {
                                foreach (var twitchStream in channelSettings.Value.Streams)
                                {
                                    try
                                    {
                                        var content = await _http.Send(HttpMethod.Get, $"https://api.twitch.tv/kraken/streams/{twitchStream.Key}");
                                        var response = await content.ReadAsStringAsync();
                                        JToken json = JsonConvert.DeserializeObject(response) as JToken;

                                        bool wasStreaming = twitchStream.Value.IsStreaming;
                                        string lastSeenGame = twitchStream.Value.CurrentGame;

                                        var streamJson = json["stream"];
                                        bool isStreaming = streamJson.HasValues;
                                        string currentGame = streamJson.HasValues ? streamJson.Value<string>("game") : null;

                                        if (wasStreaming) //Online
                                        {
                                            if (!isStreaming) //Now offline
                                            {
                                                _client.Log.Info("Twitch", $"{twitchStream.Key} is no longer streaming.");
                                                twitchStream.Value.IsStreaming = false;
                                                twitchStream.Value.CurrentGame = null;
                                                isChannelUpdated = true;
                                            }
                                            else if (lastSeenGame != currentGame) //Switched game
                                            {
                                                _client.Log.Info("Twitch", $"{twitchStream.Key} is now streaming {currentGame}.");
                                                twitchStream.Value.IsStreaming = true;
                                                twitchStream.Value.CurrentGame = currentGame;
                                                isChannelUpdated = true;
                                            }
                                        }
                                        else //Offline
                                        {
                                            if (isStreaming) //Now online
                                            {
                                                if (currentGame != null)
                                                    _client.Log.Info("Twitch", $"{twitchStream.Key} has started streaming {currentGame}.");
                                                else
                                                    _client.Log.Info("Twitch", $"{twitchStream.Key} has started streaming.");
                                                await channel.SendMessage(Format.Escape($"{twitchStream.Key} is now live (http://www.twitch.tv/{twitchStream.Key})."));
                                                twitchStream.Value.IsStreaming = true;
                                                twitchStream.Value.CurrentGame = currentGame;
                                                isChannelUpdated = true;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _client.Log.Error("Twitch", ex);
                                        await Task.Delay(5000);
                                        continue;
                                    }
                                }
                            } //Stream Loop

                            /*if (channelSettings.Value.UseSticky && (isChannelUpdated || channelSettings.Value.StickyMessageId == null))
                            {
                                //Build the sticky post
                                builder.Clear();
                                builder.AppendLine(Format.Bold("Current Streams:"));
                                foreach (var stream in channelSettings.Value.Streams)
                                {
                                    var streamData = stream.Value;
                                    if (streamData.IsStreaming)
                                    {
                                        if (streamData.CurrentGame != null)
                                            builder.AppendLine(Format.Escape($"{stream.Key} - {streamData.CurrentGame} (http://www.twitch.tv/{stream.Key})"));
                                        else
                                            builder.AppendLine(Format.Escape($"{stream.Key} (http://www.twitch.tv/{stream.Key}))"));
                                    }
                                }

                                //Edit the old message or make a new one
                                string text = builder.ToString();
                                if (channelSettings.Value.StickyMessageId != null)
                                {
                                    try
                                    {
                                        await _client.StatusAPI.Send(
                                            new UpdateMessageRequest(channelSettings.Key, channelSettings.Value.StickyMessageId.Value) { Content = text });
                                    }
                                    catch (HttpException)
                                    {
                                        _client.Log.Error("Twitch", "Failed to edit message.");
                                        channelSettings.Value.StickyMessageId = null;
                                    }
                                }
                                if (channelSettings.Value.StickyMessageId == null)
                                {
                                    channelSettings.Value.StickyMessageId = (await _client.SendMessage(_client.GetChannel(channelSettings.Key), text)).Id;
                                    isChannelUpdated = true;
                                }

                                //Delete all old messages in the sticky'd channel to keep our message at the top
                                try
                                {
                                    var msgs = await _client.DownloadMessages(channel, 50);
                                    foreach (var message in msgs
                                            .OrderByDescending(x => x.Timestamp)
                                            .Where(x => x.Id != channelSettings.Value.StickyMessageId)
                                            .Skip(3))
                                        await _client.DeleteMessage(message);
                                }
                                catch (HttpException) { }
                            }*/
                            isServerUpdated |= isChannelUpdated;
                        } //Channel Loop
                        if (isServerUpdated)
                            await _settings.Save(settings);
                    } //Server Loop
                    await Task.Delay(1000 * 60, cancelToken); //Wait 60 seconds between full updates
                }
            }
            catch (TaskCanceledException) { }
        }
    }
}
