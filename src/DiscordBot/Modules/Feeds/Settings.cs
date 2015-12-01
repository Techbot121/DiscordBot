using System;
using System.Collections.Concurrent;

namespace DiscordBot.Modules.Feeds
{
	public class Settings
	{
		public class Feed
		{
			public long ChannelId { get; set; }
			public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.UtcNow;
		}

		public ConcurrentDictionary<string, Feed> Feeds = new ConcurrentDictionary<string, Feed>();
		public bool AddFeed(string url, long channelId)
		{
			return Feeds.TryAdd(url, new Feed { ChannelId = channelId });
		}
		public bool RemoveFeed(string url)
		{
			Feed ignored;
			return Feeds.TryRemove(url, out ignored);
		}
	}
}
