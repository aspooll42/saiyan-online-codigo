using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TrackingRadarResultPacket : IntersectPacket
{
	[Key(0)]
	public Guid TargetId { get; set; }

	[Key(1)]
	public TrackingRadarResult Result { get; set; }

	[Key(2)]
	public string MapName { get; set; } = string.Empty;

	[Key(3)]
	public int Radars { get; set; }

	public TrackingRadarResultPacket()
	{
	}

	public TrackingRadarResultPacket(Guid targetId, TrackingRadarResult result, string mapName, int radars)
	{
		TargetId = targetId;
		Result = result;
		MapName = mapName;
		Radars = radars;
	}
}
