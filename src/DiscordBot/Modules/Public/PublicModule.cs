﻿using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace DiscordBot.Modules.Public
{
    internal class PublicModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            manager.CreateCommands("", group =>
            {
                group.MinPermissions((int)PermissionLevel.User);

                group.CreateCommand("join")
                    .Description("Requests the bot to join another server.")
                    .Parameter("invite url")
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        var invite = await _client.GetInvite(e.Args[0]);
                        if (invite == null)
                        {
                            await _client.Reply(e, $"Invite not found.");
                            return;
                        }
                        else if (invite.IsRevoked)
                        {
                            await _client.Reply(e, $"This invite has expired or the bot is banned from that server.");
                            return;
                        }

                        await invite.Accept();
                        await _client.Reply(e, $"Joined server.");
                    });
                group.CreateCommand("leave")
                    .Description("Instructs the bot to leave this server.")
                    .MinPermissions((int)PermissionLevel.ServerModerator)
                    .Do(async e =>
                    {
                        await _client.Reply(e, $"Leaving~");
                        await e.Server.Leave();
                    });

                group.CreateCommand("say")
                    .Parameter("Text", ParameterType.Unparsed)
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(e.Message.Resolve(Format.Escape(e.Args[0])));
                    });
                group.CreateCommand("sayraw")
                    .Parameter("Text", ParameterType.Unparsed)
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(e.Args[0]);
                    });

                group.CreateCommand("info")
                    .Alias("about")
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(
                            $"{Format.Bold("Info")}\n" +
                            $"- Author: Voltana (ID 53905483156684800)\n" +
                            $"- Modified by: Techbot (ID 94829082360942592)\n" +
                            $"- Library: {DiscordConfig.LibName} ({DiscordConfig.LibVersion})\n" +
                            $"- Runtime: {GetRuntime()} {GetBitness()}\n" +
                            $"- Uptime: {GetUptime()}\n\n" +

                            $"{Format.Bold("Stats")}\n" +
                            $"- Heap Size: {GetHeapSize()} MB\n" +
                            $"- Servers: {_client.Servers.Count()}\n" +
                            $"- Channels: {_client.Servers.Sum(x => x.AllChannels.Count())}\n" +
                            $"- Users: {_client.Servers.Sum(x => x.Users.Count())}"
                        );
                    });

                group.CreateCommand("avatar")
                    .Parameter("user")
                    .Parameter("discriminator", ParameterType.Optional)
                    .Description("Displays the full resolution Avatar of the user specified.")
                    .Alias("av")
                    .Do(async e =>
                    {
                        var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
                        if (user == null)
                        {
                            await _client.Reply(e, $"Could not find that user.");
                            return;
                        }
                        string avurl = user.AvatarUrl;
                        await e.Channel.SendMessage(avurl);
                    });

                group.CreateCommand("pp")
                    .Description("PEPEEEEEEEEEEEEEEEEEEEEEEEEEEEE")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage("http://www.myinstants.com/instant/pp/");
                    });

                group.CreateCommand("setAvatar")
                .Parameter("uri", ParameterType.Required)
                .MinPermissions((int)PermissionLevel.BotOwner)
                .Do(async e =>
                {
                    if (File.Exists("av.png"))
                    {
                        File.Delete("av.png");
                    }
                    using (WebClient w = new WebClient())
                    {
                        try
                        {
                            w.DownloadFile(new Uri(e.Args[0]), "av.png");
                        }
                        catch (WebException ex)
                        {
                            _client.Log.Error("Avatar", ex);
                            throw;
                        }
                        finally
                        {
                            w.Dispose();
                        }
                    }
                    if (!File.Exists("av.png"))
                    {
                        return;
                    }
                    var asd = File.OpenRead("av.png");
                    await _client.CurrentUser.Edit("", null, null, null, asd);
                    asd.Close();
                });
            });
        }

        private static string GetRuntime()
#if NET11
            => ".Net Framework 1.1";
#elif NET20
            => ".Net Framework 2.0";
#elif NET35
            => ".Net Framework 3.5";
#elif NET40
            => ".Net Framework 4.0";
#elif NET45
            => ".Net Framework 4.5";
#elif NET451
            => ".Net Framework 4.5.1";
#elif NET452
            => ".Net Framework 4.5.2";
#elif NET46
            => ".Net Framework 4.6";
#elif NET461
            => ".Net Framework 4.6.1";
#elif NETCORE50
            => ".Net Core 5.0";
#elif DNX451
            => "DNX (.Net Framework 4.5.1)";

#elif DNX452
            => "DNX (.Net Framework 4.5.2)";
#elif DNX46
            => "DNX (.Net Framework 4.6)";
#elif DNX461
            => "DNX (.Net Framework 4.6.1)";
#elif DNXCORE50
            => "DNX (.Net Core 5.0)";
#elif DOTNET50 || NETPLATFORM10
            => ".Net Platform Standard 1.0";
#elif DOTNET51 || NETPLATFORM11
            => ".Net Platform Standard 1.1";
#elif DOTNET52 || NETPLATFORM12
            => ".Net Platform Standard 1.2";
#elif DOTNET53 || NETPLATFORM13
            => ".Net Platform Standard 1.3";
#elif DOTNET54 || NETPLATFORM14
            => ".Net Platform Standard 1.4";
#else
            => "Unknown";
#endif

        private static string GetBitness() => $"{IntPtr.Size * 8}-bit";

        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");

        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();
    }
}