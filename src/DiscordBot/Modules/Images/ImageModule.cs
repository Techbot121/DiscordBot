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
                .Parameter("parameters", ParameterType.Unparsed)
                .Description("Adds text to an Image.\n<`uri`> ⦗`color`⦘ ⦗`alpha`⦘ ⦗`position`⦘ ⦗`fontsize`⦘ ⦗`dropshadow`⦘\nExample usage:\n⦗`cap http://myimage.png red 0.5 center arial 12 1 hello world`⦘\n⦗`cap http://myimage.png hello world`⦘")
                .Alias("cap")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var args = getArgs(e);
                        string uri = e.Args[0];

                        if (args.Any())
                        {
                            if (await isImage(uri))
                            {
                                string file = "img_cap_tmp" + Guid.NewGuid().ToString() + await getImageExtension(uri);

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
                                    using (ImageFactory iff = new ImageFactory(true))
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
                group.CreateCommand("edit")
                .Parameter("uri", ParameterType.Required)
                .Parameter("parameters", ParameterType.Unparsed)
                .Description("transforms an image.\nSupported Parameters:\n\n\nTint: `tint=red`\nCensor: `censor=x;y;w;h`\n`Scaling: `w=num or h=num`\n`blur=num`\n`rot=num`\n`filp=true/false`\n`crop=true + (top=num or bottom=num or left=num or right=num)`")
                .Alias("e")
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var args = getArgs(e);
                        string uri = e.Args[0];

                        if (await isImage(uri))
                        {
                            string file = "img_e_tmp" + Guid.NewGuid().ToString() + await getImageExtension(uri);
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

                                ISupportedImageFormat format = new PngFormat { Quality = 100 };

                                using (MemoryStream ins = new MemoryStream(pb))
                                using (MemoryStream outs = new MemoryStream())
                                using (ImageFactory iff = new ImageFactory(true))
                                {
                                    iff.Load(ins);
                                    if (args.ContainsKey("rot"))
                                    {
                                        iff.Rotate(int.Parse(args["rot"]));
                                    }
                                    if (args.ContainsKey("flip"))
                                    {
                                        iff.Flip(bool.Parse(args["flip"]));
                                    }
                                    if (args.ContainsKey("blur"))
                                    {
                                        iff.GaussianBlur(int.Parse(args["blur"]));
                                    }
                                    if (args.ContainsKey("ecrop"))
                                    {
                                        iff.EntropyCrop(byte.Parse(args["ecrop"]));
                                    }
                                    if (args.ContainsKey("pixelate"))
                                    {
                                        iff.Pixelate(int.Parse(args["pixelate"]));
                                    }
                                    if (args.ContainsKey("tint"))
                                    {
                                        iff.Tint(System.Drawing.Color.FromName(args["tint"]));
                                    }
                                    if (args.ContainsKey("replacecolor"))
                                    {
                                        string[] rcargs = args["replacecolor"].Split(';');
                                        System.Drawing.Color tar = System.Drawing.Color.FromName(rcargs[0]);
                                        System.Drawing.Color rep = System.Drawing.Color.FromName(rcargs[1]);

                                        if (rcargs.Length > 2 && int.Parse(rcargs[2]) >= 128 || int.Parse(rcargs[2]) < 0)
                                        {
                                            await _client.ReplyError(e, "Fuzzines mix and max values are 0 - 128");
                                            File.Delete(file);
                                            return;
                                        }
                                        int fuzz = rcargs.Length > 2 ? int.Parse(rcargs[2]) : 128;

                                        iff.ReplaceColor(tar, rep, fuzz);
                                    }
                                    if (args.ContainsKey("censor"))
                                    {
                                        string[] cargs = args["censor"].Split(';');
                                        var pixels = int.Parse(cargs[4]);
                                        var x = int.Parse(cargs[0]);
                                        var y = -int.Parse(cargs[1]);
                                        var w = int.Parse(cargs[2]);
                                        var h = int.Parse(cargs[3]);

                                        iff.Pixelate(pixels, new Rectangle(new Point(x, y), new Size(w, h)));
                                    }
                                    if (args.ContainsKey("w") || args.ContainsKey("h"))
                                    {
                                        int width = 0, height = 0;

                                        if (args.ContainsKey("w"))
                                        {
                                            width = int.Parse(args["w"]);
                                        }

                                        if (args.ContainsKey("h"))
                                        {
                                            height = int.Parse(args["h"]);
                                        }
                                        iff.Resize(new ResizeLayer(new Size(width, height), ResizeMode.Stretch, AnchorPosition.Center, true, null, new Size(5000, 5000)));
                                    }
                                    if (args.ContainsKey("crop"))
                                    {
                                        int top = 0, bottom = 0, left = 0, right = 0;

                                        // is there a better way to do this?

                                        if (args.ContainsKey("top"))
                                        {
                                            top = int.Parse(args["top"]);
                                        }
                                        if (args.ContainsKey("bottom"))
                                        {
                                            bottom = int.Parse(args["bottom"]);
                                        }
                                        if (args.ContainsKey("left"))
                                        {
                                            left = int.Parse(args["left"]);
                                        }
                                        if (args.ContainsKey("right"))
                                        {
                                            right = int.Parse(args["right"]);
                                        }

                                        iff.Crop(new CropLayer(
                                            top,
                                            bottom,
                                            left,
                                            right,
                                            CropMode.Percentage));
                                    }

                                    iff
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

        private Dictionary<string, string> getArgs(CommandEventArgs e)
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
            return args;
        }
    }
}