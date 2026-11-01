using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class BumpPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public Guid EventId { get; set; }

	public BumpPacket()
	{
	}

	public BumpPacket(Guid mapId, Guid eventId)
	{
		MapId = mapId;
		EventId = eventId;
	}
}
