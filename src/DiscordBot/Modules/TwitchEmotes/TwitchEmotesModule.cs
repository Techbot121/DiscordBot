using Discord;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System.IO;

namespace DiscordBot.Modules.TwitchEmotes
{
    internal class TwitchEmotesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private const string path = "./config/emotes/";

        private string[] Emotes = Directory.GetFiles(path, "*.png");

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            manager.CreateCommands("", group =>
            {
                group.MinPermissions((int)PermissionLevel.User);

                try
                {
                    foreach (var i in Emotes)
                    {
                        group.CreateCommand(Path.GetFileNameWithoutExtension(i))
                             .Do(async e =>
                             {
                                 await e.Channel.SendFile(i);
                             });
                    }
                }
                catch (IOException e)
                {
                    _client.Log.Error("TwitchEmotes", e);
                }
            });
        }
    }
}