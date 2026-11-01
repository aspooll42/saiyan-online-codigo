using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class BeamRequestPacket : IntersectPacket
{
	[Key(0)]
	public int Slot { get; set; }

	[Key(1)]
	public bool Charging { get; set; }

	[Key(2)]
	public bool Cancel { get; set; }

	[Key(3)]
	public float Angle { get; set; }

	[Key(4)]
	public Guid CibleLiee { get; set; }

	[Key(5)]
	public float Courbe { get; set; }

	public BeamRequestPacket()
	{
	}

	public BeamRequestPacket(int slot, bool charging, bool cancel, float angle)
	{
		Slot = slot;
		Charging = charging;
		Cancel = cancel;
		Angle = angle;
	}
}
