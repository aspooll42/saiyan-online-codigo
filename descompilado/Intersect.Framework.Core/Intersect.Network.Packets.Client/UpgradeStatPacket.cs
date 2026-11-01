using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UpgradeStatPacket : IntersectPacket
{
	public const byte ToutReprendre = byte.MaxValue;

	[Key(0)]
	public byte Stat { get; set; }

	[Key(1)]
	public int Amount { get; set; } = 1;

	public UpgradeStatPacket()
	{
	}

	public UpgradeStatPacket(byte stat)
		: this(stat, 1)
	{
	}

	public UpgradeStatPacket(byte stat, int amount)
	{
		Stat = stat;
		Amount = amount;
	}
}
