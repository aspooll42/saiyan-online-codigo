using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class MapInstanceChangedPacket : IntersectPacket
{
	[Key(0)]
	public EntityPacket[] EntitiesToDispose { get; set; }

	[Key(1)]
	public List<Guid> MapIdsToRefresh { get; set; }

	public MapInstanceChangedPacket()
	{
	}

	public MapInstanceChangedPacket(EntityPacket[] entitiesToDispose, List<Guid> mapIds)
	{
		EntitiesToDispose = entitiesToDispose;
		MapIdsToRefresh = mapIds;
	}
}
