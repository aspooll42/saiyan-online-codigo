using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HoldPlayerPacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public bool Releasing { get; set; }

	public HoldPlayerPacket()
	{
	}

	public HoldPlayerPacket(Guid eventId, Guid mapId, bool releasing)
	{
		EventId = eventId;
		MapId = mapId;
		Releasing = releasing;
	}
}
