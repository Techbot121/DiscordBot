using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Commands.Permissions.Visibility;
using Discord.Modules;
using System.Linq;

namespace DiscordBot.Modules.Modules
{
	//TODO: Save what modules have been enabled on each server
    internal class ModulesModule : IModule
	{
		private ModuleManager _manager;
		private DiscordClient _client;
		private ModuleService _service;

		void IModule.Install(ModuleManager manager)
		{
			_manager = manager;
			_client = manager.Client;
			_service = manager.Client.GetService<ModuleService>();

            manager.CreateCommands("modules", group =>
			{
				group.MinPermissions((int)PermissionLevel.BotOwner);
                group.CreateCommand("list")
					.Description("Gives a list of all available modules.")
					.Do(async e =>
					{
						string text = "Available Modules: " + string.Join(", ", _service.Modules.Select(x => x.Id));
						await _client.Reply(e, text);
					});
				group.CreateCommand("enable")
					.Description("Enables a module for this server.")
					.Parameter("module")
					.PublicOnly()
					.Do(e =>
					{
						var module = GetModule(e.Args[0]);
						if (module == null)
						{
							_client.ReplyError(e, "Unknown module");
							return;
						}
						if (module.FilterType == ModuleFilter.None || module.FilterType == ModuleFilter.AlwaysAllowPrivate)
						{
							_client.ReplyError(e, "This module is global and cannot be enabled/disabled.");
							return;
						}
						if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
						{
							_client.ReplyError(e, "This module doesn't support being enabled for servers.");
							return;
						}
						var server = e.Server;
						if (!module.EnableServer(server))
						{
							_client.ReplyError(e, $"Module {module.Id} was already enabled for server {server.Name}.");
							return;
						}
						_client.Reply(e, $"Module {module.Id} was enabled for server {server.Name}.");
					});
				group.CreateCommand("disable")
					.Description("Disables a module for this server.")
					.Parameter("module")
					.PublicOnly()
					.Do(e =>
					{
						var module = GetModule(e.Args[0]);
						if (module == null)
						{
							_client.ReplyError(e, "Unknown module");
							return;
						}
						if (module.FilterType == ModuleFilter.None || module.FilterType == ModuleFilter.AlwaysAllowPrivate)
						{
							_client.ReplyError(e, "This module is global and cannot be enabled/disabled.");
							return;
						}
						if (!module.FilterType.HasFlag(ModuleFilter.ServerWhitelist))
						{
							_client.ReplyError(e, "This module doesn't support being enabled for servers.");
							return;
						}
						var server = e.Server;
                        if (!module.DisableServer(server))
                        {
							_client.ReplyError(e, $"Module {module.Id} was not enabled for server {server.Name}.");
							return;
						}
						_client.Reply(e, $"Module {module.Id} was disabled for server {server.Name}.");
					});
			});
		}

		private ModuleManager GetModule(string id)
		{
			id = id.ToLowerInvariant();
			return _service.Modules.Where(x => x.Id == id).FirstOrDefault();
		}
	}
}
