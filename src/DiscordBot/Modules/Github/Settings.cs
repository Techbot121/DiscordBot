using System;
using System.Collections.Concurrent;
using System.Linq;

namespace DiscordBot.Modules.Github
{
	public class Settings
	{
		public class Repo
		{
			public ulong ChannelId { get; set; }
			public DateTimeOffset LastUpdate { get; set; }
			public string[] Branches { get; set; }

			public Repo(ulong channelId)
			{
				ChannelId = channelId;
				LastUpdate = DateTimeOffset.UtcNow;
				Branches = new string[] { "master" };
			}
			
			public bool AddBranch(string branch)
			{
				var oldBranches = Branches;
				if (oldBranches.Contains(branch))
					return false;

				var newBranches = new string[oldBranches.Length + 1];
				Array.Copy(oldBranches, newBranches, oldBranches.Length);
				newBranches[oldBranches.Length] = branch;

				Branches = newBranches;
				return true;
			}
			public bool RemoveBranch(string branch)
			{
				var oldBranches = Branches;
				if (!oldBranches.Contains(branch))
					return false;

				Branches = oldBranches.Where(x => x != branch).ToArray();
				return true;
			}
		}

		public ConcurrentDictionary<string, Repo> Repos = new ConcurrentDictionary<string, Repo>();
		public bool AddRepo(string repo, ulong channelId)
            => Repos.TryAdd(repo, new Repo(channelId));
		public bool RemoveRepo(string repo)
		{
			Repo ignored;
			return Repos.TryRemove(repo, out ignored);
		}
	}
}
