using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using System;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace DiscordBot.Modules.TwitchEmotes
{
    internal class TwitchEmotesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private const string path = "./config/emotes/";

        string[] Emotes = Directory.GetFiles(path,"*.png");

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            manager.CreateCommands("", group =>
            {
                group.MinPermissions((int)PermissionLevel.User);

                foreach (var i in Emotes)
                {
                    group.CreateCommand(Path.GetFileNameWithoutExtension(i))
                        .Do(async e =>
                        {
                            await e.Channel.SendFile(i);
                        });
                }
               

            });
        }
    }
}
