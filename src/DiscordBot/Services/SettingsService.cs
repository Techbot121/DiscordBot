using Discord;
using Discord.Modules;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
	public class SettingsManager<SettingsT>
		where SettingsT : class, new()
	{
		public string Directory => _dir;
		private readonly string _dir;

		public IEnumerable<KeyValuePair<ulong, SettingsT>> AllServers => _servers;
		private ConcurrentDictionary<ulong, SettingsT> _servers;

		public SettingsManager(string name)
		{
			_dir = $"./config/{name}";
			System.IO.Directory.CreateDirectory(_dir);

			LoadServerList();
		}

		public Task AddServer(ulong id, SettingsT settings)
		{
			if (_servers.TryAdd(id, settings))
				return SaveServerList();
			else
#if DNX451
                return Task.Delay(0);
#else
                return Task.CompletedTask;
#endif
        }
		public bool RemoveServer(ulong id)
		{
			SettingsT settings;
			return _servers.TryRemove(id, out settings);
		}

		public void LoadServerList()
		{
			if (File.Exists($"{_dir}/servers.json"))
			{
				var servers = JsonConvert.DeserializeObject<ulong[]>(File.ReadAllText($"{_dir}/servers.json"));
				_servers = new ConcurrentDictionary<ulong, SettingsT>(servers.ToDictionary(x => x, serverId =>
				{
					string path = $"{_dir}/{serverId}.json";
					if (File.Exists(path))
						return JsonConvert.DeserializeObject<SettingsT>(File.ReadAllText(path));
					else
						return new SettingsT();
				}));
			}
			else
				_servers = new ConcurrentDictionary<ulong, SettingsT>();
		}
		public async Task SaveServerList()
		{
			if (_servers != null)
			{
				while (true)
				{
					try
					{
						using (var fs = new FileStream($"{_dir}/servers.json", FileMode.Create, FileAccess.Write, FileShare.None))
						using (var writer = new StreamWriter(fs))
							await writer.WriteAsync(JsonConvert.SerializeObject(_servers.Keys.ToArray()));
						break;
					}
					catch (IOException) //In use
					{
						await Task.Delay(1000);
					}
				}
			}
		}

		public SettingsT Load(Server server)
			=> Load(server.Id);
		public SettingsT Load(ulong serverId)
		{
			SettingsT result;
			if (_servers.TryGetValue(serverId, out result))
				return result;
			else
				return new SettingsT();
		}

		public Task Save(Server server, SettingsT settings)
			=> Save(server.Id, settings);
		public Task Save(KeyValuePair<ulong, SettingsT> pair)
			=> Save(pair.Key, pair.Value);
        public async Task Save(ulong serverId, SettingsT settings)
		{
			_servers[serverId] = settings;

			while (true)
			{
				try
				{
					using (var fs = new FileStream($"{_dir}/{serverId}.json", FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(fs))
						await writer.WriteAsync(JsonConvert.SerializeObject(settings));
					break;
				}
				catch (IOException) //In use
				{
					await Task.Delay(1000);
				}
			}
		}
	}

	public class SettingsService : IService
	{
		public void Install(DiscordClient client) { }

		public SettingsManager<SettingsT> AddModule<ModuleT, SettingsT>(ModuleManager manager)
			where SettingsT : class, new()
		{
			return new SettingsManager<SettingsT>(manager.Id);
		}
	}
}
