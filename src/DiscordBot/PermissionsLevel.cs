namespace DiscordBot
{
    public enum PermissionLevel : byte
    {
		User = 0,
		ChannelModerator, //Manage Messages (Channel)
		ChannelAdmin, //Manage Permissions (Channel)
		ServerModerator, //Manage Messages, Kick, Ban (Server)
		ServerAdmin, //Manage Roles (Server)
		ServerOwner, //Owner (Server)
		BotOwner, //Bot Owner (Global)
	}
}
