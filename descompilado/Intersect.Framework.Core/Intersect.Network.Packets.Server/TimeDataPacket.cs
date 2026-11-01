using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TimeDataPacket : IntersectPacket
{
	[Key(0)]
	public string TimeJson { get; set; }

	public TimeDataPacket()
	{
	}

	public TimeDataPacket(string timeJson)
	{
		TimeJson = timeJson;
	}
}
