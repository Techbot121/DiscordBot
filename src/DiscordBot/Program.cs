using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Commands.Permissions.Userlist;
using Discord.Modules;
using DiscordBot.Modules;
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Compilation;
using Microsoft.Extensions.PlatformAbstractions;
using DiscordBot.Services;

namespace DiscordBot
{
	public class Program
	{
		private DiscordClient _client;

        public Program(IApplicationEnvironment env, ILibraryExporter exporter)
		{
			GlobalSettings.Load();

			//Set up the base client itself with no voice and small message queues
			_client = new DiscordClient(new DiscordClientConfig
			{
				AckMessages = true,
				LogLevel = LogMessageSeverity.Verbose,
				TrackActivity = true,
				UseMessageQueue = false,
				VoiceMode = DiscordVoiceMode.Both,
				EnableVoiceMultiserver = true,
				EnableVoiceEncryption = true,
				VoiceBitrate = 512,
				MessageCacheLength = 10,
				UseLargeThreshold = true
			});
			_client.LogMessage += (s, e) => _client.Log(e);

			//Add ASP.Net resources so we can access them elsewhere
			_client.AddSingleton(env);
			_client.AddSingleton(exporter);

			//Add a whitelist service so the bot only responds to commands from us or the people we choose
			//_client.AddService(new WhitelistService(new string[] { GlobalSettings.Users.DevId }));

			//Add a blacklist service so we can add people that can't run any commands
			_client.AddService(new BlacklistService());

			//Add a permission level service so we can divide people up into multiple roles
			//(in this case, base on their permissions in a given server or channel)
			_client.AddService(new PermissionLevelService((u, c) =>
			{
				if (u.Id == GlobalSettings.Users.DevId)
					return (int)PermissionLevel.BotOwner;
				if (!u.IsPrivate)
				{
					if (u == c.Server.Owner)
						return (int)PermissionLevel.ServerOwner;

					var serverPerms = u.ServerPermissions;
					if (serverPerms.ManageRoles)
						return (int)PermissionLevel.ServerAdmin;
					if (serverPerms.ManageMessages && serverPerms.KickMembers && serverPerms.BanMembers)
						return (int)PermissionLevel.ServerModerator;

					var channelPerms = u.GetPermissions(c);
					if (channelPerms.ManagePermissions)
						return (int)PermissionLevel.ChannelAdmin;
					if (channelPerms.ManageMessages)
						return (int)PermissionLevel.ChannelModerator;
				}
				return (int)PermissionLevel.User;
			}));

			//Adds a command service to use Discord.Commands, and activate the built-in help function
			var commands = _client.AddService(new CommandService(new CommandServiceConfig
			{
				CommandChar = '~',
				HelpMode = HelpMode.Public
			}));

			//Display errors that occur when a user tries to run a command
			//(In this case, we hide argcount, parsing and unknown command errors to reduce spam in servers with multiple bots)
			commands.CommandError += (s, e) =>
			{
				string msg = e.Exception?.GetBaseException().Message;
				if (msg == null) //No mxception - show a generic message
				{
					//A lot of these messages are disabled to not spam public servers. In private ones, you may want to enable them.
					switch (e.ErrorType)
					{
						case CommandErrorType.Exception:
							//msg = "Unknown error.";
							break;
						case CommandErrorType.BadPermissions:
							msg = "You do not have permission to run this command.";
							break;
						case CommandErrorType.BadArgCount:
							//msg = "You provided the incorrect number of arguments for this command.";
							break;
						case CommandErrorType.InvalidInput:
							//msg = "Unable to parse your command, please check your input.";
							break;
						case CommandErrorType.UnknownCommand:
							//msg = "Unknown command.";
							break;
					}
				}
				if (msg != null)
				{
					_client.ReplyError(e, msg);
					_client.Log(LogMessageSeverity.Error, "Command", msg);
				}
			};

			//Log to the console whenever someone uses a command
			commands.RanCommand += (s, e) => _client.Log(LogMessageSeverity.Info, "Command", $"{e.User.Name}: {e.Command.Text}");

			//Add a module service to use Discord.Modules, and add the different modules we want in this bot
			//(Modules are an isolation of functionality where they can be enabled only for certain channel/servers, and are grouped in the built-in help)
			var modules = _client.AddService(new ModuleService());
			_client.AddService(new SettingsService());
			_client.AddService(new HttpService());
			modules.Install(new Modules.Admin.AdminModule(), "Admin", FilterType.ServerWhitelist);
			modules.Install(new Modules.Colors.ColorsModule(), "Colors", FilterType.ServerWhitelist);
			modules.Install(new Modules.Execute.ExecuteModule(), "Execute", FilterType.ServerWhitelist);
			modules.Install(new Modules.Feeds.FeedModule(), "Feeds", FilterType.ServerWhitelist);
			modules.Install(new Modules.Github.GithubModule(), "Repos", FilterType.ServerWhitelist);
			modules.Install(new Modules.Modules.ModulesModule(), "Modules", FilterType.Unrestricted);
			modules.Install(new Modules.Public.PublicModule(), "Public", FilterType.Unrestricted);
			modules.Install(new Modules.Twitch.TwitchModule(), "Twitch", FilterType.ServerWhitelist);

#if PRIVATE
			PrivateModules.Install(_client);
#endif

			//Convert this method to an async function and connect to the server
			_client.Run(async () =>
			{
				while (true)
				{
					try
					{
						await _client.Connect(GlobalSettings.Discord.Email, GlobalSettings.Discord.Password);
						break;
					}
					catch (Exception ex)
					{
						string msg = ex.GetBaseException().Message;
						_client.Log(LogMessageSeverity.Error, $"Login Failed: {msg}");
						await Task.Delay(1000);
					}
				}
			});
		}

		public static void Main(string[] args) => WebApplication.Run<Program>(args);
	}
}
