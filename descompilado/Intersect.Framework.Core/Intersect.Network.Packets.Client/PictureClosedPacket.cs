using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PictureClosedPacket : IntersectPacket
{
	[Key(0)]
	public Guid EventId { get; set; }

	public PictureClosedPacket()
	{
	}

	public PictureClosedPacket(Guid eventId)
	{
		EventId = eventId;
	}
}
