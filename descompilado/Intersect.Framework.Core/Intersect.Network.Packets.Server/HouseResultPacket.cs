using Intersect.Framework.Core.GameObjects.Housing;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HouseResultPacket : IntersectPacket
{
	[Key(0)]
	public HouseResult Result { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public long Amount { get; set; }

	public HouseResultPacket()
	{
	}

	public HouseResultPacket(HouseResult result, string name = "", long amount = 0L)
	{
		Result = result;
		Name = name;
		Amount = amount;
	}
}
