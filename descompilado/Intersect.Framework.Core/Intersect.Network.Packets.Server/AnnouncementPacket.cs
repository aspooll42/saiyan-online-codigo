using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class AnnouncementPacket : IntersectPacket
{
	[Key(0)]
	public string Message { get; set; }

	[Key(1)]
	public long Duration { get; set; }

	public AnnouncementPacket()
	{
	}

	public AnnouncementPacket(string message, long duration)
	{
		Message = message;
		Duration = duration;
	}
}
