using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CancelCastPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	public CancelCastPacket()
	{
	}

	public CancelCastPacket(Guid entityId)
	{
		EntityId = entityId;
	}
}
