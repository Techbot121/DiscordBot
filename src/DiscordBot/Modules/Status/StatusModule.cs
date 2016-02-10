using Discord;
using Discord.API.Status;
using Discord.API.Status.Rest;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Net;
using DiscordBot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Status
{
	internal class StatusModule : IModule
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
			_http = _client.Services.Get<HttpService>();
			_settings = _client.Services.Get<SettingsService>()
				.AddModule<StatusModule, Settings>(manager);

			manager.CreateCommands("status", group =>
			{
				group.MinPermissions((int)PermissionLevel.BotOwner);

				group.CreateCommand("enable")
                    .Parameter("channel", ParameterType.Optional)
                    .Do(async e =>
                    {
                        var settings = _settings.Load(e.Server);

                        Channel channel;
                        if (e.Args[0] != "")
                            channel = await _client.FindChannel(e, e.Args[0], ChannelType.Text);
                        else
                            channel = e.Channel;
                        if (channel == null) return;

                        settings.Channel = channel.Id;
                        await _settings.Save(e.Server, settings);

						await _client.Reply(e, $"Enabled status reports in {channel.Name}");
					});
                group.CreateCommand("disable")
                    .Do(async e =>
                    {
                        var settings = _settings.Load(e.Server);

                        settings.Channel = null;
                        await _settings.Save(e.Server, settings);

                        await _client.Reply(e, "Disabled status reports on this server.");
                    });
			});

			_client.LoggedIn += (s, e) =>
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
            try
            {
                var cancelToken = _client.CancelToken;
                StringBuilder builder = new StringBuilder();
                StatusResult content;
                while (!_client.CancelToken.IsCancellationRequested)
                {
                    //Wait 1 minute between full updates
                    await Task.Delay(60000, cancelToken); 

                    //Get all current and recent incidents
                    try
                    {
                        content = await _client.StatusAPI.Send(new GetAllIncidentsRequest());
                    }
                    catch (Exception)
                    {
                        _client.Log.Warning("Status", $"Unable to get Discord's current status.");
                        continue;
                    }

                    foreach (var pair in _settings.AllServers)
                    {
                        DateTimeOffset? newDate = null;
                        builder.Clear();
                        var settings = pair.Value;
                        var channelId = pair.Value.Channel;

                        //Go through all incidents and see if any were updated since our last loop
                        foreach (var incident in content.Incidents)
                        {
                            var date = incident.UpdatedAt;
                            if (date > settings.LastUpdate)
                            {
                                if (channelId.HasValue)
                                {
                                    if (builder.Length != 0)
                                        builder.AppendLine();
                                    builder.AppendLine(Format.Bold(incident.Name));
                                    builder.AppendLine($"{incident.Status}: {incident.Updates.OrderByDescending(x => x.UpdatedAt).First().Body}");
                                }

                                if (newDate == null || date > newDate.Value)
                                    newDate = date;
                            }
                        }

                        if (newDate != null) //Was anything new found?
                        {
                            //Announce if a channel is registered
                            if (channelId.HasValue)
                            {
                                var channel = _client.GetChannel(channelId.Value);
                                if (channel != null)
                                {
                                    try
                                    {
                                        await channel.SendMessage(builder.ToString());
                                    }
                                    catch (Exception) { }
                                }
                            }

                            //Update LastUpdated
                            settings.LastUpdate = newDate.Value;
                            await _settings.Save(pair.Key, settings);
                        }
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
	}
}
