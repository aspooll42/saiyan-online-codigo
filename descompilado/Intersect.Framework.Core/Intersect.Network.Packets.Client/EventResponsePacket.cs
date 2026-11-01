using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class EventResponsePacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	[Key(1)]
	public byte Response { get; set; }

	public EventResponsePacket()
	{
	}

	public EventResponsePacket(Guid eventId, byte response)
	{
		EventId = eventId;
		Response = response;
	}
}
