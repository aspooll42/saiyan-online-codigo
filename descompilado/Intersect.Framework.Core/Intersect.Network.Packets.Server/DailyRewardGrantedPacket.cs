using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class DailyRewardGrantedPacket : IntersectPacket
{
	[Key(0)]
	public int Day { get; set; }

	[Key(1)]
	public string Label { get; set; } = string.Empty;

	public DailyRewardGrantedPacket()
	{
	}

	public DailyRewardGrantedPacket(int day, string label)
	{
		Day = day;
		Label = label;
	}
}
