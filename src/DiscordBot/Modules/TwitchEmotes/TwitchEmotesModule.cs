using Discord;
using Discord.Modules;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordBot.Modules.TwitchEmotes
{
    internal class TwitchEmotesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;

        private const string TwitchPath = "./config/emotes/twitch/";
        private const string FPPath = "./config/emotes/fp/";

        private string[] TwitchEmotePath = Directory.GetFiles(TwitchPath);
        private string[] FPEmotePath = Directory.GetFiles(FPPath);

        private string[] TwitchEmotes = Directory.EnumerateFiles(TwitchPath, "*", SearchOption.TopDirectoryOnly)
            .Where(x => x.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) || x.EndsWith(".gif", System.StringComparison.OrdinalIgnoreCase) || x.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();

        private string[] FPEmotes = Directory.EnumerateFiles(FPPath, "*", SearchOption.TopDirectoryOnly)
            .Where(x => x.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) || x.EndsWith(".gif", System.StringComparison.OrdinalIgnoreCase) || x.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;

            _client.MessageReceived += async (s, e) =>
            {
                try
                {
                    var ma = e.Message.Text.Split(null);

                    var fpreg = Regex.Matches(e.Message.Text, @"s?:([^:]*?):");

                    var fpemots = fpreg.Cast<Match>().SelectMany(x => x.Groups.Cast<Capture>().Skip(1).Select(y => y.Value)).ToList();

                    if (ma.Any(TwitchEmotes.Contains))
                    {
                        if (ma.Where(TwitchEmotes.Contains).Count() > 1 && ma.Where(TwitchEmotes.Contains).Count() < 10)
                        {
                            var emotes = ma.Where(TwitchEmotes.Contains);
                            var images = new List<Image>();

                            foreach (string emote in emotes)
                            {
                                using (Stream stream = File.OpenRead(TwitchEmotePath.Where(x => x.Contains(emote)).FirstOrDefault()))
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
                            var emote = ma.Where(x => TwitchEmotes.Contains(x)).First();
                            await e.Channel.SendFile(TwitchEmotePath.Where(x => x.Contains(emote)).FirstOrDefault());
                        }
                    }
                    if (fpemots.Any())
                    {
                        int fpcount = fpemots.Count();

                        if (fpcount > 1 && fpcount < 10)
                        {
                            var emotes = fpemots.Where(FPEmotes.Contains);
                            var images = new List<Image>();

                            foreach (string emote in emotes)
                            {
                                using (Stream stream = File.OpenRead(FPEmotePath.Where(x => x.Contains(emote)).FirstOrDefault()))
                                {
                                    Image image = Image.FromStream(stream, false, false);
                                    images.Add(image);
                                }
                            }
                            // lol
                            MergeImages(images).Save("fptmp.png");
                            await e.Channel.SendFile("fptmp.png");
                            File.Delete("fptmp.png");
                            // lol
                        }
                        else
                        {
                            var emote = fpemots[0];
                            await e.Channel.SendFile(FPEmotePath.Where(x => x.Contains(emote)).FirstOrDefault());
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