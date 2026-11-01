using System;
using Intersect.Config;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityBeamPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public BeamPhase Phase { get; set; }

	[Key(4)]
	public float Angle { get; set; }

	[Key(5)]
	public int Level { get; set; }

	[Key(6)]
	public Guid TargetId { get; set; }

	[Key(7)]
	public float ReachTiles { get; set; }

	[Key(8)]
	public float Courbe { get; set; }

	public EntityBeamPacket()
	{
	}

	public EntityBeamPacket(Guid id, EntityType type, Guid mapId, BeamPhase phase, float angle, int level, Guid targetId, float reachTiles)
	{
		Id = id;
		Type = type;
		MapId = mapId;
		Phase = phase;
		Angle = angle;
		Level = level;
		TargetId = targetId;
		ReachTiles = reachTiles;
	}
}
