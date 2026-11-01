using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class ActivateEventPacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	public ActivateEventPacket()
	{
	}

	public ActivateEventPacket(Guid eventId)
	{
		EventId = eventId;
	}
}
