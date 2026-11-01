using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class GuardPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public bool IsBlocking { get; set; }

	[Key(2)]
	public byte GuardPercent { get; set; }

	[Key(3)]
	public GuardEvent Event { get; set; }

	[Key(4)]
	public Guid AttackerId { get; set; }

	[Key(5)]
	public int DurationMs { get; set; }

	[Key(6)]
	public byte Genre { get; set; }

	public GuardPacket()
	{
	}

	public GuardPacket(Guid entityId, bool isBlocking, byte guardPercent, GuardEvent guardEvent, Guid attackerId, int durationMs)
	{
		EntityId = entityId;
		IsBlocking = isBlocking;
		GuardPercent = guardPercent;
		Event = guardEvent;
		AttackerId = attackerId;
		DurationMs = durationMs;
	}
}
