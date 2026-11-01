using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ErrorMessagePacket : IntersectPacket
{
	[Key(0)]
	public string Header { get; set; }

	[Key(1)]
	public string Error { get; set; }

	public ErrorMessagePacket()
	{
	}

	public ErrorMessagePacket(string header, string error)
	{
		Header = header;
		Error = error;
	}
}
