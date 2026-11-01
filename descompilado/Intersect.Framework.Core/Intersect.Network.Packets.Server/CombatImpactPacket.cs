using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CombatImpactPacket : IntersectPacket
{
	[Key(0)]
	public Guid AttackerId { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	[Key(2)]
	public byte ComboStep { get; set; }

	[Key(3)]
	public byte ComboHits { get; set; }

	[Key(4)]
	public bool Critical { get; set; }

	[Key(5)]
	public bool Ecrase { get; set; }

	[IgnoreMember]
	public bool IsFinisher
	{
		get
		{
			if (ComboStep > 0)
			{
				return ComboStep >= ComboHits;
			}
			return false;
		}
	}

	public CombatImpactPacket()
	{
	}

	public CombatImpactPacket(Guid attackerId, Guid targetId, byte comboStep, byte comboHits, bool critical)
	{
		AttackerId = attackerId;
		TargetId = targetId;
		ComboStep = comboStep;
		ComboHits = comboHits;
		Critical = critical;
	}
}
