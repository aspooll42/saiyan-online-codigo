using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class GuildDirectoryRequestPacket : IntersectPacket
{
	[Key(0)]
	public string Query { get; set; } = string.Empty;

	[Key(1)]
	public int Page { get; set; }

	public GuildDirectoryRequestPacket()
	{
	}

	public GuildDirectoryRequestPacket(string query, int page)
	{
		Query = query;
		Page = page;
	}
}
