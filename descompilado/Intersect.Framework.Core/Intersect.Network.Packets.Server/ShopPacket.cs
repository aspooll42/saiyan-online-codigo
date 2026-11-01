using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ShopPacket : IntersectPacket
{
	[Key(0)]
	public string ShopData { get; set; }

	[Key(1)]
	public bool Close { get; set; }

	public ShopPacket()
	{
	}

	public ShopPacket(string shopData, bool close)
	{
		ShopData = shopData;
		Close = close;
	}
}
