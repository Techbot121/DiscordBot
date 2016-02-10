using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Net;
using DiscordBot.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Github
{
	internal class GithubModule : IModule
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
				.AddModule<GithubModule, Settings>(manager);

			manager.CreateCommands("repos", group =>
			{
				group.MinPermissions((int)PermissionLevel.BotOwner);

				group.CreateCommand("list")
					.Do(async e =>
					{
						StringBuilder builder = new StringBuilder();
						var settings = _settings.Load(e.Server);
						foreach (var repo in settings.Repos.OrderBy(x => x.Key))
							builder.AppendLine($"{repo.Key} [{string.Join(",", repo.Value.Branches)}] => {_client.GetChannel(repo.Value.ChannelId)?.Name ?? "Unknown"}");
						await _client.Reply(e, "Linked Repos", builder.ToString());
					});

				group.CreateCommand("add")
					.Parameter("githubrepo")
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

						string url = FilterUrl(e.Args[0]);
						if (settings.AddRepo(url, channel.Id))
						{
							await _settings.Save(e.Server.Id, settings);
							await _client.Reply(e, $"Linked repo {url} to {channel.Name}.");
						}
						else
							await _client.Reply(e, $"Error: Repo {url} is already being watched.");
					});

				group.CreateCommand("remove")
					.Parameter("githubrepo")
					.Do(async e =>
					{
						var settings = _settings.Load(e.Server);
						string url = FilterUrl(e.Args[0]);
						if (settings.RemoveRepo(url))
						{
							await _settings.Save(e.Server.Id, settings);
							await _client.Reply(e, $"Unlinked repo {url}.");
						}
						else
							await _client.Reply(e, $"Error: Repo {url} is not currently being watched.");
					});

				group.CreateCommand("addbranch")
					.Parameter("githubrepo")
					.Parameter("branch")
					.Do(async e =>
					{
						var settings = _settings.Load(e.Server);
						string url = FilterUrl(e.Args[0]);
						var repo = settings.Repos[url];
						if (repo != null)
						{
							if (repo.AddBranch(e.Args[1]))
							{
								await _settings.Save(e.Server.Id, settings);
								await _client.Reply(e, $"Added branch {url}/{e.Args[1]}.");
							}
							else
								await _client.Reply(e, $"Error: Branch {url}/{e.Args[1]} is already being watched.");
						}
						else
							await _client.Reply(e, $"Error: Repo {url} is not currently being watched.");
					});

				group.CreateCommand("removebranch")
					.Parameter("githubrepo")
					.Parameter("branch")
					.Do(async e =>
					{
						var settings = _settings.Load(e.Server);
						string url = FilterUrl(e.Args[0]);
						var repo = settings.Repos[url];
						if (repo != null)
						{
							if (repo.RemoveBranch(e.Args[1]))
							{
								await _settings.Save(e.Server.Id, settings);
								await _client.Reply(e, $"Removed branch {url}/{e.Args[1]}.");
							}
							else
								await _client.Reply(e, $"Error: Branch {url}/{e.Args[1]} is not being watched.");
						}
						else
							await _client.Reply(e, $"Error: Repo {url} is not currently being watched.");
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
                while (!_client.CancelToken.IsCancellationRequested)
                {
                    foreach (var settings in _settings.AllServers)
                    {
                        foreach (var repo in settings.Value.Repos)
                        {
                            try
                            {
                                var channel = _client.GetChannel(repo.Value.ChannelId);
                                if (channel != null && channel.Server.CurrentUser.GetPermissions(channel).SendMessages)
                                {
                                    bool dateChanged = false;
                                    var since = repo.Value.LastUpdate.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                                    DateTimeOffset newDate = repo.Value.LastUpdate;
                                    string repoAuthor = repo.Key.Split('/')[0];
                                    HttpContent content;
                                    string response;
                                    JToken json;

                                    foreach (var branch in repo.Value.Branches)
                                    {
                                        try
                                        {
                                            content = await _http.Send(
                                                HttpMethod.Get,
                                                $"https://api.github.com/repos/{repo.Key}/commits?sha={branch}&since={since}",
                                                authToken: GlobalSettings.Github.Token);
                                        }
                                        catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden || ex.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            _client.Log.Warning("Github", $"Unable to access {repo.Key}'s {branch} branch.");
                                            continue;
                                        }
                                        response = await content.ReadAsStringAsync();
                                        json = JsonConvert.DeserializeObject(response) as JToken;

                                        if (json.Children().Any())
                                        {
                                            builder.Clear();
                                            builder.Append($"{Format.Bold(repo.Key)}");
                                            if (branch != "master")
                                                builder.Append($" ({Format.Bold(branch)})");

                                            foreach (var commit in json.Children().Reverse())
                                            {
                                                var sha = commit.Value<string>("sha")?.Substring(0, 7);
                                                var msg = commit["commit"].Value<string>("message");
                                                var date = new DateTimeOffset(commit["commit"]["committer"].Value<DateTime>("date").AddSeconds(1.0), TimeSpan.Zero);
                                                var author = commit["commit"]["committer"].Value<string>("name");
                                                //var url = commit.Value<string>("html_url");

                                                _client.Log.Info("Github", $"{repo.Key} {branch} #{sha}");

                                                string prefix = $"\n{Format.Code(sha)} ";
                                                builder.Append($"{prefix}{Format.Escape(msg.Split('\n')[0])} [{author}]");
                                                //builder.Append($"{prefix}{url}");
                                                if (date > newDate)
                                                {
                                                    newDate = date;
                                                    dateChanged = true;
                                                }
                                            }
                                            try
                                            {
                                                await channel.SendMessage(builder.ToString());
                                            }
                                            catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden || ex.StatusCode == HttpStatusCode.NotFound) { }
                                        }

                                        await Task.Delay(1000, cancelToken);
                                    }

                                    try
                                    {
                                        content = await _http.Send(
                                            HttpMethod.Get,
                                            $"https://api.github.com/repos/{repo.Key}/issues?state=all&sort=updated&since={since}",
                                            authToken: GlobalSettings.Github.Token);
                                    }
                                    catch
                                    {
                                        _client.Log.Warning("Github", $"Unable to access {repo.Key}'s issues.");
                                        continue;
                                    }
                                    response = await content.ReadAsStringAsync();
                                    json = JsonConvert.DeserializeObject(response) as JToken;

                                    foreach (var issue in json.Children().Reverse())
                                    {
                                        var author = issue["user"].Value<string>("login");
                                        var id = issue.Value<string>("number");
                                        var url = issue.Value<string>("html_url");
                                        var createdAt = issue.Value<DateTime>("created_at");
                                        var updatedAt = issue.Value<DateTime>("updated_at");
                                        var closedAt = issue.Value<DateTime?>("closed_at");
                                        var title = issue.Value<string>("title");

                                        string text;
                                        bool skip = false;
                                        string type = (issue.Value<JToken>("pull_request") != null) ? "Pull Request" : "Issue";

                                        if (author.Equals(repoAuthor, StringComparison.OrdinalIgnoreCase))
                                            skip = true;
                                        if (updatedAt == closedAt)
                                            text = $"Closed {type} #{id}";
                                        else if (createdAt == updatedAt)
                                            text = $"New {type} #{id}";
                                        else
                                        {
                                            skip = true;
                                            text = $"Updated {type} #{id}";
                                        }
                                        _client.Log.Info("Github", $"{repo.Key} {text}");
                                        if (!string.IsNullOrEmpty(title))
                                            text += '\n' + title;

                                        if (!skip)
                                        {
                                            try
                                            {
                                                await channel.SendMessage($"{Format.Bold(repo.Key)} {text}\n{Format.Escape(url)}");
                                            }
                                            catch (HttpException ex) when (ex.StatusCode == HttpStatusCode.Forbidden || ex.StatusCode == HttpStatusCode.NotFound) { }
                                        }

                                        var date = new DateTimeOffset(updatedAt.AddSeconds(1.0), TimeSpan.Zero);
                                        if (date > newDate)
                                        {
                                            newDate = date;
                                            dateChanged = true;
                                        }
                                    }

                                    if (dateChanged)
                                    {
                                        repo.Value.LastUpdate = newDate;
                                        await _settings.Save(settings);
                                    }
                                }
                            }
                            catch (Exception ex) when (!(ex is TaskCanceledException))
                            {
                                _client.Log.Error("Github", ex);
                                await Task.Delay(5000);
                                continue;
                            }

                            await Task.Delay(1000, cancelToken); //Wait 1 second between individual requests
                        }
                    }
                    await Task.Delay(60000, cancelToken); //Wait 1 minute between full updates
                }
            }
            catch (TaskCanceledException) { }
        }

		private static string FilterUrl(string url)
		{
			if (url.StartsWith("http://github.com/"))
				url = url.Substring("http://github.com/".Length);
			else if (url.StartsWith("https://github.com/"))
				url = url.Substring("https://github.com/".Length);
			if (url.EndsWith("/"))
				url = url.Substring(0, url.Length - 1);
			return url.Trim();
		}
	}
}
