using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class GuildCreatePacket : IntersectPacket
{
	[Key(0)]
	public string Name { get; set; } = string.Empty;

	public GuildCreatePacket()
	{
	}

	public GuildCreatePacket(string name)
	{
		Name = name;
	}
}
