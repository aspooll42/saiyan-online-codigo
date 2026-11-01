using System.Collections.Generic;
using Intersect.Collections;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class EvolveSpellPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	public EvolveSpellPacket()
	{
	}

	public EvolveSpellPacket(int slot)
	{
		Slot = slot;
	}

	public override Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		Sanitizer sanitizer = default(Sanitizer);
		Slot = sanitizer.Maximum("Slot", Slot, 0);
		return sanitizer.Sanitized;
	}
}
