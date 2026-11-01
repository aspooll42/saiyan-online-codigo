using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TrackingRadarPacket : IntersectPacket
{
	[Key(0)]
	public TrackingRadarEntryPacket[] Entries { get; set; } = Array.Empty<TrackingRadarEntryPacket>();

	[Key(1)]
	public int Radars { get; set; }

	public TrackingRadarPacket()
	{
	}

	public TrackingRadarPacket(TrackingRadarEntryPacket[] entries, int radars)
	{
		Entries = entries;
		Radars = radars;
	}
}
