using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityAttackPacket : AbstractTimedPacket
{
	[Key(3)]
	public Guid Id { get; set; }

	[Key(4)]
	public EntityType Type { get; set; }

	[Key(5)]
	public Guid MapId { get; set; }

	[Key(6)]
	public int AttackTimer { get; set; }

	[Key(7)]
	public bool IsBlocking { get; set; }

	[Key(8)]
	public byte Direction { get; set; }

	[Key(9)]
	public byte Pas { get; set; }

	[Key(10)]
	public byte Coup { get; set; }

	public EntityAttackPacket()
	{
	}

	public EntityAttackPacket(Guid id, EntityType type, Guid mapId, int attackTimer, bool isBlocking, byte direction = 0, byte pas = 0, byte coup = 0)
	{
		Id = id;
		Type = type;
		MapId = mapId;
		AttackTimer = attackTimer;
		IsBlocking = isBlocking;
		Direction = direction;
		Pas = pas;
		Coup = coup;
	}
}
