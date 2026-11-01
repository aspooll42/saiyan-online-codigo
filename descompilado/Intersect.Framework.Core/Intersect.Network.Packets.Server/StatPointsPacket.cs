using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class StatPointsPacket : IntersectPacket
{
	[Key(0)]
	public int Points { get; set; }

	public StatPointsPacket()
	{
	}

	public StatPointsPacket(int points)
	{
		Points = points;
	}
}
