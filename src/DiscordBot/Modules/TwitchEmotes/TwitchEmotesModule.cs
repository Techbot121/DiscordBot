using Discord;
using Discord.Modules;
using System.IO;
using System.Linq;

namespace DiscordBot.Modules.TwitchEmotes
{
    internal class TwitchEmotesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private const string path = "./config/emotes/";

        private string[] EmotePath = Directory.GetFiles(path, "*.png");

        private string[] Emotes = Directory
                    .GetFiles(path, "*.png")
                    .Select(Path.GetFileNameWithoutExtension).ToArray();

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            _client.MessageReceived += async (s, e) =>
            {
                try
                {
                    var ma = e.Message.Text.Split(null);

                    if (ma.Any(Emotes.Contains))
                    {
                        var emote = ma.Where(x => Emotes.Contains(x)).First();
                        await e.Channel.SendFile(path + emote + ".png");
                    }
                }
                catch
                {
                    // ignored
                }
            };
        }
    }
}

//        manager.CreateCommands("", group =>
//{
//    group.MinPermissions((int)PermissionLevel.User);

//    try
//    {
//        foreach (var i in Emotes)
//        {
//            group.CreateCommand(Path.GetFileNameWithoutExtension(i))
//                 .Do(async e =>
//                 {
//                     await e.Channel.SendFile(i);
//                 });
//        }
//    }
//    catch (IOException e)
//    {
//        _client.Log.Error("TwitchEmotes", e);
//    }
//});