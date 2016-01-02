using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System;
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

                group.CreateCommand("about")
                    .Alias("info")
                    .Do(async e =>
                    {
                        await _client.Reply(e,
                            $"{Format.Bold("Info")}\n" +
                            $"- Author: Voltana (ID 53905483156684800)\n" +
                            $"- Library: {DiscordConfig.LibName}({DiscordConfig.LibVersion})\n" +
                            $"- Memory Usage: {Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB\n" +

                            $"{Format.Bold("Cache")}\n" +
                            $" - Servers: {_client.Servers.Count()}\n" +
                            $" - Channels: {_client.Servers.Sum(x => x.AllChannels.Count())}\n" +
                            $" - Users: {_client.Servers.Sum(x => x.Users.Count())}"
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
	}
}
