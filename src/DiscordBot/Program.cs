using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Commands.Permissions.Userlist;
using Discord.Modules;
using DiscordBot.Modules.Admin;
using DiscordBot.Modules.Colors;
using DiscordBot.Modules.Feeds;
using DiscordBot.Modules.Github;
using DiscordBot.Modules.Modules;
using DiscordBot.Modules.Public;
using DiscordBot.Modules.Status;
using DiscordBot.Modules.Twitch;
using DiscordBot.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Program
    {
        private DiscordClient _client;

        private void Start(string[] args)
        {
            //Discord.ETF.ETFWriter.Test();
            GlobalSettings.Load();

            //Set up the base client itself with no voice and small message queues
            _client = new DiscordClient(x =>
            {
                x.AppName = "VoltBot";
                x.AppUrl = "https://github.com/RogueException/DiscordBot";
                x.AppVersion = DiscordConfig.LibVersion;
                x.LogLevel = LogSeverity.Info;
                x.MessageCacheSize = 0;
                x.UsePermissionsCache = false;
                x.EnablePreUpdateEvents = true;
            })

            //** Core Services **//
            //These are services adding functionality from other Discord.Net.XXX packages

            //Enable commands on this bot and activate the built-in help command
            .UsingCommands(x =>
            {
                x.CommandChar = '~';
                x.HelpMode = HelpMode.Public;
            })

            //Enable command modules
            .UsingModules()

            //Enable audio support
            .UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
                x.EnableMultiserver = true;
                x.EnableEncryption = true;
                x.Bitrate = AudioServiceConfig.MaxBitrate;
                x.BufferLength = 10000;
            })

            //** Command Permission Services **//
            // These allow you to use permission checks on commands or command groups, or apply a permission globally (such as a blacklist)

            //Add a blacklist service so we can add people that can't run any commands. We have used a whitelist instead to restrict it to just us.
            .UsingGlobalBlacklist()
            //.EnableGlobalWhitelist(GlobalSettings.Users.DevId))

            //Assign users to our own role system based on their permissions in the server/channel a command is run in.
            .UsingPermissionLevels((u, c) =>
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
            })

            //** Helper Services**//
            //These are used by the modules below, and will likely be removed in the future

            .AddService<SettingsService>()
            .AddService<HttpService>()

            //** Command Modules **//
            //Modules allow for events such as commands run or user joins to be filtered to certain servers/channels, as well as provide a grouping mechanism for commands
            
            .AddModule<AdminModule>("Admin", ModuleFilter.ServerWhitelist)
            .AddModule<ColorsModule>("Colors", ModuleFilter.ServerWhitelist)
            .AddModule<FeedModule>("Feeds", ModuleFilter.ServerWhitelist)
            .AddModule<GithubModule>("Repos", ModuleFilter.ServerWhitelist)
            .AddModule<ModulesModule>("Modules", ModuleFilter.None)
            .AddModule<PublicModule>("Public", ModuleFilter.None)
            .AddModule<TwitchModule>("Twitch", ModuleFilter.ServerWhitelist)
            .AddModule<StatusModule>("Status", ModuleFilter.ServerWhitelist);
            //.AddModule(new ExecuteModule(env, exporter), "Execute", ModuleFilter.ServerWhitelist);

            //** Events **//
            
            _client.Log.Message += (s, e) => WriteLog(e);

            //Display errors that occur when a user tries to run a command
            //(In this case, we hide argcount, parsing and unknown command errors to reduce spam in servers with multiple bots)
            _client.Commands().CommandErrored += (s, e) =>
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
            _client.Commands().CommandExecuted += (s, e) => _client.Log.Info("Command", $"{e.Command.Text} ({e.User.Name})");
            
            //Used to load private modules outside of this repo
#if PRIVATE
            PrivateModules.Install(_client);
#endif

            //** Run **//

#if !DNXCORE50
            Console.Title = $"{_client.Config.AppName} v{_client.Config.AppVersion} (Discord.Net v{DiscordConfig.LibVersion})";
#endif

            //Convert this method to an async function and connect to the server
            //DiscordClient will automatically reconnect once we've established a connection, until then we loop on our end
            //Note: ExecuteAndWait is only needed for Console projects as Main can't be declared as async. UI/Web applications should *not* use this function.
            _client.ExecuteAndWait(async () =>
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

        //TODO: Remove this hack
        //public static void Main(string[] args) => WebApplication.Run<Program>(args);
        public static void Main(string[] args) => new Program().Start(args);

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
