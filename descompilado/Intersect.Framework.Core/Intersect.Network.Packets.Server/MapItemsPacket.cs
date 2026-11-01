using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapItemsPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId;

	[Key(1)]
	public MapItemUpdatePacket[] Items { get; set; }

	public MapItemsPacket()
	{
	}

	public MapItemsPacket(Guid mapId, MapItemUpdatePacket[] items)
	{
		MapId = mapId;
		Items = items;
	}
}
