using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class LogoutPacket : IntersectPacket
{
	[Key(0)]
	public bool ReturningToCharSelect { get; set; }

	public LogoutPacket()
	{
	}

	public LogoutPacket(bool returnToCharSelect)
	{
		ReturningToCharSelect = returnToCharSelect;
	}
}
