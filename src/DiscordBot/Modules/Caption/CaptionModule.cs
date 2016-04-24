using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Caption
{
    internal class CaptionModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private HttpService _http;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            manager.CreateCommands("", group =>
            {
                group.CreateCommand("caption")
                .Parameter("url to image", ParameterType.Required)
                .Parameter("text", ParameterType.Unparsed)
                .Description("Adds text to an Image.")
                .Alias("cap")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        string uri = e.Args[0];

                        if (e.Args[1].Any())
                        {
                            string text = e.Args[1];
                            if (await isImage(uri))
                            {
                                string file = "cap_tmp" + Guid.NewGuid().ToString() + await getImageExtension(uri);

                                try
                                {
                                    await DownloadImage(uri, file);
                                }
                                catch (WebException ex)
                                {
                                    _client.Log.Error("captions", ex);
                                    return;
                                }

                                if (File.Exists(file))
                                {
                                    Bitmap bmp = (Bitmap)Image.FromFile(file);

                                    using (Graphics g = Graphics.FromImage(bmp))
                                    {
                                        using (Font f = new Font("Arial", 20))
                                        {
                                            float w = bmp.Width;
                                            float h = bmp.Height;

                                            StringFormat sf = new StringFormat();
                                            sf.Alignment = StringAlignment.Center;
                                            sf.LineAlignment = StringAlignment.Center;

                                            SizeF s = g.MeasureString(text, f);

                                            g.DrawString(text, f, Brushes.Red, new PointF(w/2f, h - s.Height - 2f),sf);
                                        }
                                    }
                                    bmp.Save("fuckio"+file, ImageFormat.Png);
                                    bmp.Dispose();

                                    await e.Channel.SendFile("fuckio" + file);
                                    File.Delete(file);
                                    File.Delete("fuckio" + file);
                                }
                                else
                                {
                                    await _client.ReplyError(e, "Couldn't find your image on my end. Bug @Techbot about it.");
                                    return;
                                }
                            }
                            else
                            {
                                await _client.ReplyError(e, "That doesn't seem to be an image...");
                            }
                        }
                        else
                        {
                            await _client.ReplyError(e, "No Text provided, aborting...");
                        }
                    }
                    else
                    {
                        await _client.Reply(e, "Usage: `cap [link] <text>`");
                    }
                });
            });
        }

        private async Task<bool> isImage(string uri)
        {
            var r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = "HEAD";
            using (var res = await r.GetResponseAsync())
            {
                return res.ContentType.ToLower().StartsWith("image/");
            }
        }

        private async Task DownloadImage(string uri, string file)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            using (HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsync())

                if ((resp.StatusCode == HttpStatusCode.OK ||
                        resp.StatusCode == HttpStatusCode.Moved ||
                        resp.StatusCode == HttpStatusCode.Redirect) &&
                        resp.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    using (Stream inp = resp.GetResponseStream())
                    using (Stream outp = File.OpenWrite(file))
                    {
                        byte[] buffer = new byte[4096];
                        int br;
                        do
                        {
                            br = await inp.ReadAsync(buffer, 0, buffer.Length);
                            await outp.WriteAsync(buffer, 0, br);
                        } while (br != 0);
                    }
                }
        }

        private async Task<string> getImageExtension(string uri)
        {
            var r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = "HEAD";
            using (var res = await r.GetResponseAsync())
            {
                switch (res.ContentType)
                {
                    case "image/jpeg":
                        return ".jpg";

                    case "image/png":
                        return ".png";

                    case "image/gif":
                        return ".gif";

                    default:
                        return ""; // idk what happens when this happens
                }
            }
        }
    }
}