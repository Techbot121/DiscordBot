using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
                    .MinPermissions((int)PermissionLevel.User)
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
                    .MinPermissions((int)PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        await _client.Reply(e, $"Leaving~");
                        await e.Server.Leave();
                    });

                group.CreateCommand("say")
                    .Parameter("Text", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(e.Message.Resolve(Format.Escape(e.Args[0])));
                    });
                group.CreateCommand("sayraw")
                    .Parameter("Text", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(e.Args[0]);
                    });

                group.CreateCommand("whoami")
                    .Do(async e =>
                    {
                        await Whois(e, e.User);
                    });
                group.CreateCommand("whois")
                    .Parameter("User name")
                    .Do(async e =>
                    {
                        User user = e.Server.FindUsers(e.Args[0]).FirstOrDefault();
                        await Whois(e, user);
                    });

                group.CreateCommand("info")
                    .Alias("about")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage(
                            $"{Format.Bold("Info")}\n" +
                            $"- Author: Voltana (ID 53905483156684800)\n" +
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
            });
        }

        private async Task Whois(CommandEventArgs e, User user)
        {
            if (user != null)
            {
                var response = new
                {
                    Id = user.Id,
                    Name = user.Name,
                    Discriminator = user.Discriminator
                };
                await _client.Reply(e, "User Info", response);
            }
            else
                await _client.Reply(e, "Unknown User");
        }

        private string GetRuntime()
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
#elif DNX11
            => "DNX (.Net Framework 1.1)";
#elif DNX20
            => "DNX (.Net Framework 2.0)";
#elif DNX35
            => "DNX (.Net Framework 3.5)";
#elif DNX40
            => "DNX (.Net Framework 4.0)";
#elif DNX45
            => "DNX (.Net Framework 4.5)";
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
            => "Unknown"
#endif

        private static string GetBitness()=> $"{IntPtr.Size * 8}-bit";
        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString();
    }
}
