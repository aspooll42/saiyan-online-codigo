using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityDashPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public Guid EndMapId { get; set; }

	[Key(2)]
	public byte EndX { get; set; }

	[Key(3)]
	public byte EndY { get; set; }

	[Key(4)]
	public long DashEndMilliseconds { get; set; }

	[Key(5)]
	public int DashLengthMilliseconds { get; set; }

	[Key(6)]
	public Direction Direction { get; set; }

	[Key(7)]
	public float EndOffsetX { get; set; }

	[Key(8)]
	public float EndOffsetY { get; set; }

	[Key(9)]
	public NatureDeGlissade Nature { get; set; }

	[Key(10)]
	public Guid AuteurId { get; set; }

	[Key(11)]
	public bool ContreLeDecor { get; set; }

	[Key(12)]
	public byte Part { get; set; }

	[Key(13)]
	public byte Arret { get; set; }

	[Key(14)]
	public int DegatsDArret { get; set; }

	[Key(15)]
	public byte Forme { get; set; }

	public EntityDashPacket()
	{
	}

	public EntityDashPacket(Guid entityId, Guid endMapId, int endX, int endY, long dashEndMilliseconds, int dashLengthMilliseconds, Direction direction, float endOffsetX = 0f, float endOffsetY = 0f, NatureDeGlissade nature = NatureDeGlissade.Ordinaire, Guid auteurId = default(Guid))
	{
		Nature = nature;
		AuteurId = auteurId;
		EntityId = entityId;
		EndMapId = endMapId;
		EndX = (byte)endX;
		EndY = (byte)endY;
		DashEndMilliseconds = dashEndMilliseconds;
		DashLengthMilliseconds = dashLengthMilliseconds;
		Direction = direction;
		EndOffsetX = endOffsetX;
		EndOffsetY = endOffsetY;
	}
}
