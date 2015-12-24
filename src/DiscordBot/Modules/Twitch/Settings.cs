using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DiscordBot.Modules.Twitch
{
	public class Settings
	{
		public class Channel
		{
			public bool UseSticky = false;
			public ulong? StickyMessageId = null;

			[JsonIgnore]
			private ConcurrentDictionary<string, Stream> _streams = new ConcurrentDictionary<string, Stream>();
			public IEnumerable<KeyValuePair<string, Stream>> Streams { get { return _streams; } set { _streams = new ConcurrentDictionary<string, Stream>(value); } }
			public bool AddStream(string username)
			{
				return _streams.TryAdd(username, new Stream());
			}
			public bool RemoveStream(string username)
			{
				Stream ignored;
				return _streams.TryRemove(username, out ignored);
			}
		}
		public class Stream
		{
			public bool IsStreaming { get; set; }
			public string CurrentGame { get; set; }

			public Stream()
			{
				CurrentGame = null;
			}
		}

		[JsonIgnore]
		private ConcurrentDictionary<ulong, Channel> _channels = new ConcurrentDictionary<ulong, Channel>();
		public IEnumerable<KeyValuePair<ulong, Channel>> Channels { get { return _channels; } set { _channels = new ConcurrentDictionary<ulong, Channel>(value); } }
		public Channel GetOrAddChannel(ulong channelId)
            => _channels.GetOrAdd(channelId, x => new Channel());
		public void RemoveChannel(ulong channelId)
		{
			Channel ignored;
			_channels.TryRemove(channelId, out ignored);
		}
	}
}
