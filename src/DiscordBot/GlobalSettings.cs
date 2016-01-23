using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace DiscordBot
{
    public class GlobalSettings
	{
		private const string path = "./config/global.json";
		private static GlobalSettings _instance = new GlobalSettings();

		public static void Load()
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"{path} is missing.");
			_instance = JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText(path));

		}
		public static void Save()
		{
			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var writer = new StreamWriter(stream))
				writer.Write(JsonConvert.SerializeObject(_instance, Formatting.Indented));
		}

		//Discord
		public class DiscordSettings
		{
			[JsonProperty("username")]
			public string Email;
			[JsonProperty("password")]
			public string Password;
		}
		[JsonProperty("discord")]
		private DiscordSettings _discord = new DiscordSettings();
		public static DiscordSettings Discord => _instance._discord;

		//Users
		public class UserSettings
		{
			[JsonProperty("dev")]
			public ulong DevId;
		}
		[JsonProperty("users")]
		private UserSettings _users = new UserSettings();
		public static UserSettings Users => _instance._users;

		//Github
		public class GithubSettings
		{
			[JsonProperty("username")]
			public string Username;
			[JsonProperty("password")]
			public string Password;
			[JsonIgnore]
			public string Token => Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password));
		}
		[JsonProperty("github")]
		private GithubSettings _github = new GithubSettings();
		public static GithubSettings Github => _instance._github;
	}
}
