using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Colors
{
	/// <summary> Creates a role for each built-in color and allows users to freely select them. </summary>
	internal class ColorsModule : IModule
	{
		private class ColorDefinition
		{
			public string Id;
			public string Name;
			public Color Color;
			public ColorDefinition(string name, Color color)
			{
				Name = name;
				Id = name.ToLowerInvariant();
				Color = color;
			}
		}
		private readonly List<ColorDefinition> _colors;
		private readonly Dictionary<string, ColorDefinition> _colorMap;
		private ModuleManager _manager;
		private DiscordClient _client;

		public ColorsModule()
		{
			_colors = new List<ColorDefinition>()
			{
				new ColorDefinition("Blue", Color.Blue),
				new ColorDefinition("Teal", Color.Teal),
				new ColorDefinition("Gold", Color.Gold),
				new ColorDefinition("Green", Color.Green),
				new ColorDefinition("Purple", Color.Purple),
				new ColorDefinition("Orange", Color.Orange),
				new ColorDefinition("Magenta", Color.Magenta),
				new ColorDefinition("Red", Color.Red),
				new ColorDefinition("DarkBlue", Color.DarkBlue),
				new ColorDefinition("DarkTeal", Color.DarkTeal),
				new ColorDefinition("DarkGold", Color.DarkGold),
				new ColorDefinition("DarkGreen", Color.DarkGreen),
				new ColorDefinition("DarkMagenta", Color.DarkMagenta),
				new ColorDefinition("DarkOrange", Color.DarkOrange),
				new ColorDefinition("DarkPurple", Color.DarkPurple),
				new ColorDefinition("DarkRed", Color.DarkRed),
			};
			_colorMap = _colors.ToDictionary(x => x.Id);
		}

		void IModule.Install(ModuleManager manager)
		{
			_manager = manager;
			_client = _manager.Client;

            manager.CreateCommands("colors", group =>
			{
				//group.SetAlias("colours"); //TODO: add group alias and absolute vs relative alias
				group.CreateCommand("list")
					.Description("Gives a list of all available username colors.")
					.Do(async e =>
					{
						string text = $"{Format.Bold("Available Colors:")}\n" + string.Join(", ", _colors.Select(x => '`' + x.Name + '`'));
						await _client.Reply(e, text);
					});
				group.CreateCommand("set")
					.Parameter("color")
					.Description("Sets your username to a custom color.")
					.Do(e => SetColor(e, e.User, e.Args[0]));
                group.CreateCommand("set")
					.Parameter("user")
					.Parameter("color")
					.MinPermissions((int)PermissionLevel.BotOwner)
					.Description("Sets another user's name to a custom color.")
					.Do(e =>
					{
						User user = e.Server.FindUsers(e.Args[0]).FirstOrDefault();
						if (user == null)
							return _client.ReplyError(e, "Unknown user");
						return SetColor(e, user, e.Args[1]);
					});
				group.CreateCommand("clear")
					.Description("Removes your username color, returning it to default.")
					.Do(async e =>
					{
						if (!e.Server.CurrentUser.ServerPermissions.ManageRoles)
						{
							await _client.ReplyError(e, "This command requires the bot have Manage Roles permission.");
							return;
						}
						var otherRoles = GetOtherRoles(e.User);
						await e.User.Edit(roles: otherRoles);
						await _client.Reply(e, $"Reset username color.");
					});
            });
		}

		private IEnumerable<Role> GetOtherRoles(User user)
            => user.Roles.Where(x => !_colorMap.ContainsKey(x.Name.ToLowerInvariant()));

		private async Task SetColor(CommandEventArgs e, User user, string colorName)
		{
			ColorDefinition color;
			if (!_colorMap.TryGetValue(colorName.ToLowerInvariant(), out color))
			{
				await _client.ReplyError(e, "Unknown color");
				return;
			}
			if (!e.Server.CurrentUser.ServerPermissions.ManageRoles)
			{
				await _client.ReplyError(e, "This command requires the bot have Manage Roles permission.");
				return;
			}
			Role role = e.Server.Roles.Where(x => x.Name == color.Name).FirstOrDefault();
			if (role == null)
			{
				role = await e.Server.CreateRole(color.Name);
				await role.Edit(permissions: ServerPermissions.None, color: color.Color);
			}
			var otherRoles = GetOtherRoles(user);
			await user.Edit(roles: otherRoles.Concat(new Role[] { role }));
			await _client.Reply(e, $"Set {user.Name}'s color to {color.Name}");
		}
	}
}
