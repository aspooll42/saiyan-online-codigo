using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityFlightPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public int X { get; set; }

	[Key(3)]
	public int Y { get; set; }

	[Key(4)]
	public FlightMode Mode { get; set; }

	[Key(5)]
	public FlightEvent Event { get; set; }

	[Key(6)]
	public long LockRemainingMs { get; set; }

	public EntityFlightPacket()
	{
	}

	public EntityFlightPacket(Guid id, Guid mapId, int x, int y, FlightMode mode, FlightEvent flightEvent, long lockRemainingMs)
	{
		Id = id;
		MapId = mapId;
		X = x;
		Y = y;
		Mode = mode;
		Event = flightEvent;
		LockRemainingMs = lockRemainingMs;
	}
}
