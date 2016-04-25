using Discord;
using Discord.Commands;
using Discord.Modules;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Images
{
    internal class ImagesModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private HttpService _http;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            manager.CreateCommands("image", group =>
            {
                group.CreateCommand("caption")
                .Parameter("uri", ParameterType.Required)
                .Parameter("text", ParameterType.Unparsed)
                .Description("Adds text to an Image.\n<`uri`> ⦗`color`⦘ ⦗`alpha`⦘ ⦗`position`⦘ ⦗`fontsize`⦘ ⦗`dropshadow`⦘\nExample usage:\n⦗`cap http://myimage.png red 0.5 center arial 12 1 hello world`⦘\n⦗`cap http://myimage.png hello world`⦘")
                .Alias("cap")
                .Do(async e =>
                {
                    MatchCollection match = Regex.Matches(e.Args[1], "(?<=^|\\ )([^\\s\\\"=]+)=(\\\"[^\\\"]*\\\"|[^\\s\\\"]+)(?=$|\\ )");

                    Dictionary<string, string> args = new Dictionary<string, string>();

                    if (match.Count > 0)
                    {
                        foreach (Match m in match)
                        {
                            args.Add(m.Groups[1]?.Value.Replace("\"", ""), m.Groups[2]?.Value.Replace("\"", ""));
                        }
                    }

                    if (e.Args.Any())
                    {
                        string uri = e.Args[0];

                        if (args.Any())
                        {
                            if (await isImage(uri))
                            {
                                string file = "cap_tmp" + Guid.NewGuid().ToString() + await getImageExtension(uri);

                                try
                                {
                                    await DownloadImage(uri, file);
                                }
                                catch (WebException ex)
                                {
                                    await _client.ReplyError(e, ex.Message);
                                    _client.Log.Error("captions", ex);

                                    if (File.Exists(file))
                                    {
                                        File.Delete(file);
                                    }
                                    return;
                                }

                                if (File.Exists(file))
                                {
                                    byte[] pb = File.ReadAllBytes(file);

                                    var asd = new TextLayer();
                                    asd.Text = args?["text"];
                                    asd.FontColor = args.ContainsKey("color") ? System.Drawing.Color.FromName(args["color"]) : System.Drawing.Color.White;
                                    asd.FontFamily = args.ContainsKey("font") ? FontFamily.Families.Where(x => x.Name == args["font"]).FirstOrDefault() : FontFamily.GenericSerif;
                                    asd.DropShadow = args.ContainsKey("dropshadow") ? bool.Parse(args["dropshadow"]) : false;
                                    // asd.Position = Point.Empty;
                                    asd.Opacity = args.ContainsKey("opacity") ? int.Parse(args["opacity"]) : 100;
                                    asd.Style = args.ContainsKey("style") ? (FontStyle)Enum.Parse(typeof(FontStyle), args["style"], true) : FontStyle.Regular;
                                    asd.FontSize = args.ContainsKey("size") ? int.Parse(args["size"]) : 20;

                                    ISupportedImageFormat format = new PngFormat { Quality = 100 };

                                    using (MemoryStream ins = new MemoryStream(pb))
                                    using (MemoryStream outs = new MemoryStream())
                                    using (ImageFactory iff = new ImageFactory())
                                    {
                                        iff.Load(ins)
                                        .Watermark(asd)
                                        .Format(format)
                                        .Save(outs);
                                        await e.Channel.SendFile("output.png", outs);
                                    }
                                    File.Delete(file);
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
                            await _client.ReplyError(e, "No Parameters provided, aborting...");
                            return;
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