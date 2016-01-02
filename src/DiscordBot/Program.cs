using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Commands.Permissions.Userlist;
using Discord.Modules;
using DiscordBot.Modules;
using DiscordBot.Services;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Compilation;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Program
    {
        private DiscordClient _client;

        public Program(IApplicationEnvironment env, ILibraryExporter exporter)
        {
            GlobalSettings.Load();

            //Set up the base client itself with no voice and small message queues
            _client = new DiscordClient(new DiscordConfig
            {
                AppName = "VoltBot",
                AppUrl = "https://github.com/RogueException/DiscordBot",
                AppVersion = DiscordConfig.LibVersion,
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 0,
                UsePermissionsCache = false
            });
#if !DNXCORE50
            Console.Title = $"{_client.Config.AppName} v{_client.Config.AppVersion} (Discord.Net v{DiscordConfig.LibVersion})";
#endif
            _client.Log.Message += (s, e) => WriteLog(e);

            //Add a whitelist service so the bot only responds to commands from us or the people we choose
            //_client.AddService(new WhitelistService(new string[] { GlobalSettings.Users.DevId }));

            //Add a blacklist service so we can add people that can't run any commands
            _client.Services.Add(new BlacklistService());

            //Add a permission level service so we can divide people up into multiple roles
            //(in this case, base on their permissions in a given server or channel)
            _client.Services.Add(new PermissionLevelService((u, c) =>
            {
                if (u.Id == GlobalSettings.Users.DevId)
                    return (int)PermissionLevel.BotOwner;
                if (u.Server != null)
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
            var commands = _client.Services.Add(new CommandService(new CommandServiceConfig
            {
                CommandChar = '~',
                HelpMode = HelpMode.Public
            }));

            //Display errors that occur when a user tries to run a command
            //(In this case, we hide argcount, parsing and unknown command errors to reduce spam in servers with multiple bots)
            commands.CommandError += (s, e) =>
            {
                string msg = e.Exception?.GetBaseException().Message;
                if (msg == null) //No exception - show a generic message
                {
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
                    _client.Log.Error("Command", msg);
                }
            };

            //Log to the console whenever someone uses a command
            commands.Command += (s, e) => _client.Log.Info("Command", $"{e.User.Name}: {e.Command.Text}");

            _client.Services.Add(new AudioService(new AudioServiceConfig
            {
                Mode = AudioMode.Outgoing,
                EnableMultiserver = false,
                EnableEncryption = true,
                Bitrate = 512,
            }));

            //Add a module service to use Discord.Modules, and add the different modules we want in this bot
            //(Modules are an isolation of functionality where they can be enabled only for certain channel/servers, and are grouped in the built-in help)
            var modules = _client.Services.Add(new ModuleService());
            _client.Services.Add(new SettingsService());
            _client.Services.Add(new HttpService());
            modules.Install(new Modules.Admin.AdminModule(), "Admin", FilterType.ServerWhitelist);
            modules.Install(new Modules.Colors.ColorsModule(), "Colors", FilterType.ServerWhitelist);
            modules.Install(new Modules.Execute.ExecuteModule(env, exporter), "Execute", FilterType.ServerWhitelist);
            modules.Install(new Modules.Feeds.FeedModule(), "Feeds", FilterType.ServerWhitelist);
            modules.Install(new Modules.Github.GithubModule(), "Repos", FilterType.ServerWhitelist);
            modules.Install(new Modules.Modules.ModulesModule(), "Modules", FilterType.Unrestricted);
            modules.Install(new Modules.Public.PublicModule(), "Public", FilterType.Unrestricted);
            modules.Install(new Modules.Twitch.TwitchModule(), "Twitch", FilterType.ServerWhitelist);
            
#if PRIVATE
            PrivateModules.Install(_client);
#endif

            //Convert this method to an async function and connect to the server
            //DiscordClient will automatically reconnect once we've established a connection, until then we loop on our end
            _client.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await _client.Connect(GlobalSettings.Discord.Email, GlobalSettings.Discord.Password);
                        _client.SetGame("Discord.Net");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _client.Log.Error($"Login Failed", ex);
                        await Task.Delay(_client.Config.FailedReconnectDelay);
                    }
                }
            });
        }

        public static void Main(string[] args) => WebApplication.Run<Program>(args);

        private static void WriteLog(LogMessageEventArgs e)
        {
            //Color
            ConsoleColor color;
            switch (e.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = e.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = ex.Message;
            }
            else
                exMessage = null;

            //Source
            string sourceName = e.Source?.ToString();

            //Text
            string text;
            if (e.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = e.Message;

            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (!char.IsControl(c))
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            //if (e.Severity <= LogSeverity.Info)
            //{
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            //}
/*#if DEBUG
            System.Diagnostics.Debug.WriteLine(text);
#endif*/
        }
    }
}
