using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Commands.Permissions.Visibility;
using Discord.Modules;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot.Modules.Admin
{
	/// <summary> Provides easy access to manage users from chat. </summary>
	internal class AdminModule : IModule
	{
		private ModuleManager _manager;
		private DiscordClient _client;

		void IModule.Install(ModuleManager manager)
		{
			_manager = manager;
			_client = manager.Client;

			manager.CreateCommands("", group =>
			{
				group.PublicOnly();

				group.CreateCommand("kick")
					.Description("Kicks a user from this server.")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

                        await user.Kick();
						await _client.Reply(e, $"Kicked user {user.Name}.");
					});
				group.CreateCommand("ban")
					.Description("Bans a user from this server.")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

                        await user.Server.Ban(user);
						await _client.Reply(e, $"Banned user {user.Name}.");
					});

				group.CreateCommand("mute")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

						await user.Edit(isMuted: true);
						await _client.Reply(e, $"Muted user {user.Name}.");
					});
				group.CreateCommand("unmute")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

						await user.Edit(isMuted: false);
						await _client.Reply(e, $"Unmuted user {user.Name}.");
					});
				group.CreateCommand("deafen")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

						await user.Edit(isDeafened: true);
						await _client.Reply(e, $"Deafened user {user.Name}.");
					});
				group.CreateCommand("undeafen")
					.Parameter("user")
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ServerModerator)
					.Do(async e =>
					{
						var user = await _client.FindUser(e, e.Args[0], e.Args[1]);
						if (user == null) return;

						await user.Edit(isDeafened: false);
						await _client.Reply(e, $"Undeafened user {user.Name}.");
					});

				group.CreateCommand("cleanup")
					.Parameter("count")
					.Parameter("user", ParameterType.Optional)
					.Parameter("discriminator", ParameterType.Optional)
					.MinPermissions((int)PermissionLevel.ChannelModerator)
					.Do(async e =>
					{
						int count = int.Parse(e.Args[0]);
						string username = e.Args[1];
						string discriminator = e.Args[2];
						User[] users = null;

						if (username != "")
						{
							users = await _client.FindUsers(e, username, discriminator);
							if (users == null) return;
						}

						IEnumerable<Message> msgs;
						var cachedMsgs = e.Channel.Messages;
						if (cachedMsgs.Count() < count)
							msgs = (await e.Channel.DownloadMessages(count));
						else
							msgs = e.Channel.Messages.OrderByDescending(x => x.Timestamp).Take(count);

						if (username != "")
							msgs = msgs.Where(x => users.Contains(x.User));

						if (msgs.Any())
						{
                            foreach (var msg in msgs)
                                await msg.Delete();
							await _client.Reply(e, $"Cleaned up {msgs.Count()} messages.");
						}
						else
							await _client.ReplyError(e, $"No messages found.");
					});
			});
		}
	}
}