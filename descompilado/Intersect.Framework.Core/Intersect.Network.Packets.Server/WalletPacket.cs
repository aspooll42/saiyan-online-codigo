using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WalletPacket : IntersectPacket
{
	[Key(0)]
	public long Zeni { get; set; }

	[Key(1)]
	public Guid CurrencyItemId { get; set; }

	public WalletPacket()
	{
	}

	public WalletPacket(long zeni, Guid currencyItemId)
	{
		Zeni = zeni;
		CurrencyItemId = currencyItemId;
	}
}
