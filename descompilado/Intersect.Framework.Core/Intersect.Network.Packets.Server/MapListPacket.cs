using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapListPacket : IntersectPacket
{
	[Key(0)]
	public string MapListData { get; set; }

	public MapListPacket()
	{
	}

	public MapListPacket(string mapListData)
	{
		MapListData = mapListData;
	}
}
