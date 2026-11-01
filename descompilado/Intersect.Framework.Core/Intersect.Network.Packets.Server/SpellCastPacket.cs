using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class SpellCastPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public Guid SpellId { get; set; }

	[Key(2)]
	public int Duration { get; set; } = -1;

	public SpellCastPacket()
	{
	}

	public SpellCastPacket(Guid entityId, Guid spellId, int duration = -1)
	{
		EntityId = entityId;
		SpellId = spellId;
		Duration = duration;
	}
}
