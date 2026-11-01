using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuildInvitePacket : IntersectPacket
{
	[Key(0)]
	public string Inviter { get; set; }

	[Key(1)]
	public string GuildName { get; set; }

	public GuildInvitePacket()
	{
	}

	public GuildInvitePacket(string inviter, string guildName)
	{
		Inviter = inviter;
		GuildName = guildName;
	}
}
