using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TrackingRadarEntryPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public int Level { get; set; }

	[Key(3)]
	public long Bounty { get; set; }

	[Key(4)]
	public bool Online { get; set; }

	[Key(5)]
	public int OfflineMinutes { get; set; } = -1;

	[Key(6)]
	public string Sprite { get; set; } = string.Empty;

	[Key(7)]
	public string Face { get; set; } = string.Empty;

	public TrackingRadarEntryPacket()
	{
	}

	public TrackingRadarEntryPacket(Guid id, string name, int level, long bounty, bool online, int offlineMinutes, string sprite, string face)
	{
		Id = id;
		Name = name;
		Level = level;
		Bounty = bounty;
		Online = online;
		OfflineMinutes = offlineMinutes;
		Sprite = sprite;
		Face = face;
	}
}
