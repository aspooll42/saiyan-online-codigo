using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class GuildMotdPacket : IntersectPacket
{
	[Key(0)]
	public string Motd { get; set; } = string.Empty;

	public GuildMotdPacket()
	{
	}

	public GuildMotdPacket(string motd)
	{
		Motd = motd;
	}
}
