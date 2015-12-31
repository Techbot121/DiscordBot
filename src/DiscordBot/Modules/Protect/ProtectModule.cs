using Discord;
using Discord.Modules;
using System.Linq;

namespace DiscordBot.Modules.Protect
{
	/// <summary> Provides easy access to manage users from chat. </summary>
	internal class ProtectModule : IModule
	{
		private ModuleManager _manager;
		private DiscordClient _client;

		void IModule.Install(ModuleManager manager)
		{
			_manager = manager;
			_client = manager.Client;
            
            manager.MessageReceived += (s, e) =>
            {
                if (e.Message.MentionedUsers.Count() >= 10)
                {
                    var user = e.User;
                    if (user != null)
                    {
                        e.Server.Ban(user, 1);
                        _client.Log.Warning("Protect", $"Banned {user} for excess mentions.");
                        e.Channel.SendMessage($"Banned {user} for excess mentions.");
                    }
                }
            };
		}
	}
}