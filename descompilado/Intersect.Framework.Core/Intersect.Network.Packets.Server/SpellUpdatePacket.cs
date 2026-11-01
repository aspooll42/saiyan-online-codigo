using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class SpellUpdatePacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	[Key(1)]
	public Guid SpellId { get; set; }

	[Key(2)]
	public int Rank { get; set; } = 1;

	[Key(3)]
	public int Experience { get; set; }

	public SpellUpdatePacket()
	{
	}

	public SpellUpdatePacket(int slot, Guid spellId, int rank = 1, int experience = 0)
	{
		Slot = slot;
		SpellId = spellId;
		Rank = rank;
		Experience = experience;
	}
}
