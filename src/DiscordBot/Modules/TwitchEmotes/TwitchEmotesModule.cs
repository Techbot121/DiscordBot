using Discord;
using Discord.Modules;
using System.Collections.Generic;
using System.Drawing;
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
                        if (ma.Where(Emotes.Contains).Count() > 1)
                        {
                            var emotes = ma.Where(Emotes.Contains);
                            var images = new List<Image>();

                            foreach (string x in emotes)
                            {
                                using (Stream stream = File.OpenRead(path + x + ".png"))
                                {
                                    Image image = Image.FromStream(stream, false, false);
                                    images.Add(image);
                                }
                            }
                            // lol
                            MergeImages(images).Save("asd.png");
                            await e.Channel.SendFile("asd.png");
                            File.Delete("asd.png");
                            // lol
                        }
                        else
                        {
                            var emote = ma.Where(x => Emotes.Contains(x)).First();
                            await e.Channel.SendFile(path + emote + ".png");
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            };
        }

        private Bitmap MergeImages(IEnumerable<Image> images)
        {
            var enumerable = images;

            var width = 0;
            var height = 0;

            foreach (var image in enumerable)
            {
                width += image.Width + 4;
                height = image.Height > height
                    ? image.Height
                    : height;
            }

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                var localWidth = 0;
                foreach (var image in enumerable)
                {
                    g.DrawImage(image, localWidth, 0);
                    localWidth += image.Width + 2;
                }
            }
            return bitmap;
        }
    }
}