using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class SpellsPacket : IntersectPacket
{
	[Key(0)]
	public SpellUpdatePacket[] Slots { get; set; }

	public SpellsPacket()
	{
	}

	public SpellsPacket(SpellUpdatePacket[] slots)
	{
		Slots = slots;
	}
}
