using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class TrackingRadarLocatePacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	public TrackingRadarLocatePacket()
	{
	}

	public TrackingRadarLocatePacket(Guid targetId)
	{
		TargetId = targetId;
	}
}
