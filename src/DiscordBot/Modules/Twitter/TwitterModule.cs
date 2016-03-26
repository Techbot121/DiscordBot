using Discord;
using Discord.Modules;
using System.Text.RegularExpressions;
using TweetSharp;

namespace DiscordBot.Modules.Twitter
{
    internal class TwitterModule : IModule
    {
        private ModuleManager _manager;
        private DiscordClient _client;
        private HttpService _http;

        void IModule.Install(ModuleManager manager)
        {
            _manager = manager;
            _client = manager.Client;
            _http = _client.GetService<HttpService>();

            var ts = new TwitterService(GlobalSettings.Twitter.ConsumerKey, GlobalSettings.Twitter.ConsumerSecret);
            ts.AuthenticateWith(GlobalSettings.Twitter.AccessKey, GlobalSettings.Twitter.AccessSecret);

            _client.MessageReceived += async (s, e) =>
            {
                try
                {
                    var mt = Regex.Match(e.Message.Text, @"https?://(www.)?twitter.com/([a-zA-Z0-9_]+)/status/([0-9]+)", RegexOptions.IgnoreCase);
                    if (mt.Success)
                    {
                        var tweetId = long.Parse(mt.Groups[3].ToString());
                        var tweet = ts.GetTweet(new GetTweetOptions { Id = tweetId });
                        try
                        {
                            if (tweet.Entities.Media.Count < 1) { return; }

                            foreach (var i in tweet.ExtendedEntities.Media)
                            {
                                if (i.ExtendedEntityType > 0)
                                    await e.Channel.SendMessage(i.VideoInfo.Variants[0].Url.ToString());
                                else
                                    await e.Channel.SendMessage(i.MediaUrl.ToString());
                            }
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Could not get Media from that Tweet.");
                            _client.Log.Error("Twitter", "Couldn't get media from a Tweet from " + e.User.Name.ToString() + ".");
                        }
                    }
                    else { return; }
                }
                catch
                {
                    // ignored
                }
            };
        }
    }
}