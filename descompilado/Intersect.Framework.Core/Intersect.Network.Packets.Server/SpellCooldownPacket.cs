using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class SpellCooldownPacket : IntersectPacket
{
	[Key(0)]
	public Dictionary<Guid, long> SpellCds;

	public SpellCooldownPacket()
	{
	}

	public SpellCooldownPacket(Dictionary<Guid, long> spellCds)
	{
		SpellCds = spellCds;
	}
}
