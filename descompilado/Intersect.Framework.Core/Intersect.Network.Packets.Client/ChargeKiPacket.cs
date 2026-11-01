using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class ChargeKiPacket : IntersectPacket
{
	[Key(0)]
	public bool Charging { get; set; }

	public ChargeKiPacket()
	{
	}

	public ChargeKiPacket(bool charging)
	{
		Charging = charging;
	}
}
