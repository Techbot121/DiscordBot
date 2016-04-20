using Discord;
using Discord.Commands;
using Discord.Modules;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;

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
                .Description("Uploads image to waifu2x and returns it.\nIf no additional Parameters are specified, the default Values will be used `Noise: Medium` and `Scale 2x`")
                .Parameter("image url", ParameterType.Required)
                .Parameter("scale", ParameterType.Optional)
                .Parameter("noise", ParameterType.Optional)
                .Do(async e =>
                {
                    int scale = 2;
                    Noise noise = Noise.Medium;

                    if (e.Args[1] != "")
                    {
                        int.TryParse(e.Args[1], out scale);
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
                            if (File.Exists("temp" + ext))
                            {
                                File.Delete("temp" + ext);
                            }

                            DownloadImage(uri.AbsoluteUri, "temp" + ext); // todo: Make this shit async

                            NameValueCollection param = new NameValueCollection();

                            param.Add("scale", $"{scale}");
                            param.Add("noise", $"{(int)noise}");

                            using (WebClient cli = new WebClient())
                            {
                                cli.QueryString = param;
                                var rb = cli.UploadFile(new Uri("http://waifu2x.udp.jp/api"), "temp" + ext);
                                string ft = cli.ResponseHeaders[HttpResponseHeader.ContentType];
                                string file = "temp";
                                if (ft != null)
                                {
                                    switch (ft)
                                    {
                                        case "image/jpeg":
                                            file += ".jpg";
                                            break;

                                        case "image/png":
                                            file += ".png";
                                            break;

                                        default:
                                            break;
                                    }
                                    File.WriteAllBytes(file, rb);

                                    await e.Channel.SendFile(file);
                                }
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