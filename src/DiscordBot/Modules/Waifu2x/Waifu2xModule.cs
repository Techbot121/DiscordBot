using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Waifu2x
{
    internal class Waifu2xModule : IModule
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
                group.CreateCommand("waifu2x")
                .Alias("w2x")
                .Description("Uploads image to waifu2x and returns it.\nIf no additional Parameters are specified, the default Values will be used `Noise: None` and `Amount 1x`")
                .Parameter("image url", ParameterType.Required)
                .Parameter("amount", ParameterType.Optional)
                .Parameter("noise", ParameterType.Optional)
                .Do(async e =>
                {
                    if (e.Args.Any())
                    {
                        var uri = e.Args[0];
                        int scale = 2;
                        int amount = 1;
                        Noise noise = Noise.None;

                        if (e.Args[1] != "")
                        {
                            int.TryParse(e.Args[1], out amount);
                        }

                        if (amount > 4)
                        {
                            await _client.ReplyError(e, "Maximum allowed amount is currently 4... Aborting.");
                            return;
                        }
                        if (amount <= 0)
                        {
                            await _client.ReplyError(e, $"So you want to upscale `{amount}` times huh?");
                            return;
                        }

                        if (e.Args[2] != "")
                        {
                            noise = ParseEnum<Noise>(e.Args[2]);
                        }

                        if (uri.Any() && await isImage(e.Args[0]))
                        {
                            var ext = await getImageExtension(e.Args[0]);

                            string guid = Guid.NewGuid().ToString();

                            string file = "temp" + guid + ext;

                            try
                            {
                                await DownloadImage(uri, file);
                            }
                            catch (WebException ex)
                            {
                                await _client.ReplyError(e, ex.Message);
                                _client.Log.Error("w2x", ex);
                                return;
                            }

                            NameValueCollection param = new NameValueCollection();

                            param.Add("scale", $"{scale}");
                            param.Add("noise", $"{(int)noise}");

                            int ih = 0;
                            int iw = 0;

                            await _client.Reply(e, $"Trying to Upscale image {(amount == 1 ? "once..." : "`" + amount + "' times...")}");

                            try
                            {
                                for (int i = 0; i < amount; i++)
                                {
                                    if (File.Exists(file))
                                    {
                                        using (Image image = Image.FromFile(file))
                                        {
                                            iw = image.Width;
                                            ih = image.Height;
                                        }

                                        if (ih >= 1500 || iw >= 1500) // need to check the actual values
                                        {
                                            await _client.ReplyError(e, $"File Dimensions are now {iw}x{ih}. This will probably not work... Aborting.\nLast successful Image:");
                                            await e.Channel.SendFile(file);
                                            return;
                                        }

                                        FileInfo fi = new FileInfo(file);
                                        if (fi.Length >= 3e+6)
                                        {
                                            await _client.ReplyError(e, "File exceeded 3Mb... aborting.");
                                            return;
                                        }
                                    }
                                    using (WebClient cli = new WebClient())
                                    {
                                        cli.QueryString = param;
                                        var rb = cli.UploadFile(new Uri("http://waifu2x.udp.jp/api"), file);

                                        string ft = cli.ResponseHeaders[HttpResponseHeader.ContentType];

                                        if (ft != null)
                                        {
                                            try
                                            {
                                                File.WriteAllBytes(file, rb);
                                            }
                                            catch (IOException ex)
                                            {
                                                _client.Log.Error("w2x", ex);
                                                await _client.ReplyError(e, ex.Message);
                                                return;
                                            }

                                            await Task.Delay(1000); // lets not rape their servers
                                        }
                                        else
                                        {
                                            await _client.ReplyError(e, "Got an empty reponse from waifu2x, aborting... Please try again later (or now, I'm a bot not a cop).");
                                            return;
                                        }
                                    }
                                }
                                await e.Channel.SendFile(file);
                                await e.Channel.SendMessage($"New Resolution is: {iw}x{ih}");
                                File.Delete(file);
                            }
                            catch (WebException ex)
                            {
                                await _client.ReplyError(e, ex.Message);
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
                        await _client.Reply(e, "Usage: `w2x <link> [amount (max is 4)] [noiselevel 0-3]`");
                    }
                });
            });
        }

        private enum Noise
        {
            None,
            Medium,
            High,
            Highest
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
                        try
                        {
                            byte[] buffer = new byte[4096];
                            int br;
                            do
                            {
                                br = await inp.ReadAsync(buffer, 0, buffer.Length);
                                await outp.WriteAsync(buffer, 0, br);
                            } while (br != 0);
                        }
                        catch (IOException ex)
                        {
                            _client.Log.Error("w2x", ex);
                        }
                    }
                }
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

        private static byte[] GetUploadedFile(object s, UploadFileCompletedEventArgs e) => e.Result; // todo: check for errors if upload fails or something

        public static T ParseEnum<T>(string value) => (T)Enum.Parse(typeof(T), value, true);
    }
}