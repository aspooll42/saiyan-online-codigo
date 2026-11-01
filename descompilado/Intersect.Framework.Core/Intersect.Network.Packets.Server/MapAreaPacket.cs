using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapAreaPacket : IntersectPacket
{
	[Key(0)]
	public MapPacket[] Maps { get; set; }

	public MapAreaPacket()
	{
	}

	public MapAreaPacket(MapPacket[] maps)
	{
		Maps = maps;
	}
}
