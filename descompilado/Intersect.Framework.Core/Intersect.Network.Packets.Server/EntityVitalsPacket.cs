using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityVitalsPacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public Guid MapId { get; set; }

	[Key(3)]
	public long[] Vitals { get; set; }

	[Key(4)]
	public long[] MaxVitals { get; set; }

	[Key(5)]
	public StatusPacket[] StatusEffects { get; set; }

	[Key(6)]
	public long CombatTimeRemaining { get; set; }

	public EntityVitalsPacket()
	{
	}

	public EntityVitalsPacket(Guid id, EntityType type, Guid mapId, long[] vitals, long[] maxVitals, StatusPacket[] statusEffects, long combatTimeRemaining)
	{
		Id = id;
		Type = type;
		MapId = mapId;
		Vitals = vitals;
		MaxVitals = maxVitals;
		StatusEffects = statusEffects;
		if (combatTimeRemaining > 0)
		{
			CombatTimeRemaining = combatTimeRemaining;
		}
	}
}
