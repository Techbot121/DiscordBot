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
                    int scale = 2;
                    int amount = 1;
                    Noise noise = Noise.None;

                    bool _isRunning = false;

                    if (e.Args[1] != "")
                    {
                        int.TryParse(e.Args[1], out amount);
                    }

                    if (amount > 3)
                    {
                        await _client.ReplyError(e, "Max Amount is 3... Aborting.");
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

                    Uri uri;
                    var isUri = Uri.TryCreate(e.Args[0], UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                    if (e.Args[0].Any() && isUri)
                    {
                        var ext = Path.GetExtension(uri.AbsolutePath);

                        if (ext == ".png" || ext == ".jpg" || ext == ".jepg") // ?? who uses bmp and shit anyway
                        {

                            if(_isRunning)
                            {
                                await _client.ReplyError(e,"I'm running w2x somewhere already, please try again later.");
                                return;
                            }

                            _isRunning = true;
                            if (File.Exists("temp" + ext))
                            {
                                File.Delete("temp" + ext);
                            }

                            try
                            {
                                DownloadImage(uri.AbsoluteUri, "temp" + ext); // todo: Make this shit async
                            }
                            catch (WebException ex)
                            {
                                await _client.ReplyError(e, $"Something went wrong while downloading the Image.\n{ex}");
                                _client.Log.Error("w2x", ex);
                            }

                            NameValueCollection param = new NameValueCollection();

                            param.Add("scale", $"{scale}");
                            param.Add("noise", $"{(int)noise}");

                            await _client.Reply(e, $"Trying to Upscale image `{amount}` {(amount == 1 ? "time" : "times...")}");

                            
                            using (WebClient cli = new WebClient())
                            {
                                string file = "temp";
                                int ih = 0;
                                int iw = 0;
                                cli.QueryString = param;

                                for (int i = 0; i < amount; i++)
                                {
                                    try
                                    {
                                        Image image = Image.FromFile(file + ext);
                                        iw = image.Width;
                                        ih = image.Height;

                                        if (ih >= 1500 || iw >= 1500) // need to check the actual values
                                        {
                                            await _client.ReplyError(e, $"File Dimensions are now {iw}x{ih}. This will probably not work... Aborting.\nLast successful Image:");
                                            await e.Channel.SendFile(file + ext);
                                            _isRunning = false;
                                            return;
                                        }

                                        FileInfo fi = new FileInfo(file + ext);
                                        if (fi.Length >= 3e+6)
                                        {
                                            await _client.ReplyError(e, "File exceeded 3Mb... aborting.");
                                            _isRunning = false;
                                            return;
                                        }

                                        var rb = cli.UploadFile(new Uri("http://waifu2x.udp.jp/api"), "temp" + ext);
                                        string ft = cli.ResponseHeaders[HttpResponseHeader.ContentType];

                                        if (ft != null)
                                        {
                                            File.WriteAllBytes(file + ".png", rb);

                                            ext = ".png"; // hack

                                            await Task.Delay(1000);
                                        }
                                    }
                                    catch (FileLoadException)
                                    {
                                        throw;
                                    }
                                }

                                await e.Channel.SendFile(file + ext);
                                await e.Channel.SendMessage($"New Resolution is: {iw}x{ih}");
                                _isRunning = false;
                            }
                        }
                        else
                        {
                            await _client.ReplyError(e, "that file doesn't seem to be an image!");
                        }
                    }
                    else
                    {
                        await _client.ReplyError(e, "No ImageUrl specified");
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

        private static void DownloadImage(string uri, string file)  // todo: Make this shit async
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

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
                        br = inp.Read(buffer, 0, buffer.Length);
                        outp.Write(buffer, 0, br);
                    } while (br != 0);
                }
            }
        }

        public static T ParseEnum<T>(string value) => (T)Enum.Parse(typeof(T), value, true);
    }
}