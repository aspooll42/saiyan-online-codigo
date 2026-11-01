using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class VanishPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public VanishEvent Event { get; set; }

	[Key(2)]
	public Guid AttackerId { get; set; }

	[Key(3)]
	public byte FromX { get; set; }

	[Key(4)]
	public byte FromY { get; set; }

	[Key(5)]
	public byte ToX { get; set; }

	[Key(6)]
	public byte ToY { get; set; }

	[Key(7)]
	public byte Direction { get; set; }

	[Key(8)]
	public int WindowMs { get; set; }

	[Key(9)]
	public int CooldownMs { get; set; }

	[Key(10)]
	public byte Jetons { get; set; } = byte.MaxValue;

	[Key(11)]
	public int JetonProchainMs { get; set; }

	public VanishPacket()
	{
	}

	public VanishPacket(Guid entityId, VanishEvent vanishEvent, Guid attackerId, byte fromX, byte fromY, byte toX, byte toY, byte direction, int windowMs, int cooldownMs)
	{
		EntityId = entityId;
		Event = vanishEvent;
		AttackerId = attackerId;
		FromX = fromX;
		FromY = fromY;
		ToX = toX;
		ToY = toY;
		Direction = direction;
		WindowMs = windowMs;
		CooldownMs = cooldownMs;
	}
}
