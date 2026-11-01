using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UseSpellPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	[Key(2)]
	public bool SoftRetargetOnSelfCast { get; init; }

	[Key(3)]
	public byte Lien { get; set; }

	public UseSpellPacket()
	{
	}

	public UseSpellPacket(int slot, Guid targetId, bool softRetargetOnSelfCast)
	{
		Slot = slot;
		TargetId = targetId;
		SoftRetargetOnSelfCast = softRetargetOnSelfCast;
	}
}
