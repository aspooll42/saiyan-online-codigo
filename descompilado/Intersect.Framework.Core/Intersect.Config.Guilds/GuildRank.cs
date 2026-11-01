namespace Intersect.Config.Guilds;

public class GuildRank
{
	public string Title { get; set; }

	public int Limit { get; set; }

	public GuildPermissions Permissions { get; set; } = new GuildPermissions();
}
