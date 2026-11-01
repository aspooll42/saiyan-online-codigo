using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EntityVitalData
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public EntityType Type { get; set; }

	[Key(2)]
	public long[] Vitals { get; set; } = new long[Enum.GetValues<Vital>().Length];

	[Key(3)]
	public long[] MaxVitals { get; set; } = new long[Enum.GetValues<Vital>().Length];

	[Key(4)]
	public long CombatTimeRemaining { get; set; }
}
