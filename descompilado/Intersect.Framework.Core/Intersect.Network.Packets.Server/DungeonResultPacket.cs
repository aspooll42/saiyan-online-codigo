using Intersect.Framework.Core.GameObjects.Dungeons;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DungeonResultPacket : IntersectPacket
{
	[Key(0)]
	public DungeonResult Result { get; set; }

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public long Amount { get; set; }

	public DungeonResultPacket()
	{
	}

	public DungeonResultPacket(DungeonResult result, string name = "", long amount = 0L)
	{
		Result = result;
		Name = name;
		Amount = amount;
	}
}
