using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapAreaIdsPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] MapIds { get; set; }

	public MapAreaIdsPacket()
	{
	}

	public MapAreaIdsPacket(params Guid[] mapIds)
	{
		MapIds = mapIds;
	}
}
