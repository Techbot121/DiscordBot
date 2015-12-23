using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Legacy;
using Discord.Modules;
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
						if (invite.IsRevoked)
						{
							await _client.Reply(e, $"This invite has expired or the bot is banned from that server.");
							return;
						}

						await _client.AcceptInvite(invite);
						await _client.Reply(e, $"Joined server.");
					});
				group.CreateCommand("leave")
					.Description("Instructs the bot to leave this server.")
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.MinPermissions((int)PermissionLevel.BotOwner)
					.Do(async e =>
					{
						await _client.Reply(e, $"Leaving~");
						await _client.LeaveServer(e.Server);
					});

				group.CreateCommand("say")
					.Parameter("Text", ParameterType.Unparsed)
					.Do(async e =>
					{
						await _client.SendMessage(e.Channel, e.Message.Resolve(Format.Escape(e.Args[0])));
					});
				group.CreateCommand("sayraw")
					.Parameter("Text", ParameterType.Unparsed)
					.Do(async e =>
					{
						await _client.SendMessage(e.Channel, e.Args[0]);
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
						User user = _client.FindUsers(e.Server, e.Args[0]).FirstOrDefault();
						await Whois(e, user);
					});

				group.CreateCommand("about")
					.Alias("info")
					.Do(async e =>
					{
						//int serverCount, channelCount, userCount, uniqueUserCount, messageCount, roleCount;
						//_client.GetCacheStats(out serverCount, out channelCount, out userCount, out uniqueUserCount, out messageCount, out roleCount);
						await _client.Reply(e,
							$"{Format.Bold("Basic Info")}\n" +
							//"I'm a basic bot used to test Discord.Net and manage the Discord API server",
							"- Author: Voltana (ID 53905483156684800)\n" +
							$"- Library: {DiscordConfig.LibName}({DiscordConfig.LibVersion})"/*\n" +
							$"{Format.Bold("Cache Counts")}\n" +
							$"- Channels: {channelCount}\n" +
							$"- Messages: {messageCount}\n" +
							$"- Roles: {roleCount}\n" +
							$"- Servers: {serverCount}\n" +
							$"- Users: {userCount} ({uniqueUserCount} unique)\n"*/
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
