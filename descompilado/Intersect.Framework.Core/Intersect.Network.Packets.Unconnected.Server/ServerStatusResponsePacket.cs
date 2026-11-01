using MessagePack;

namespace Intersect.Network.Packets.Unconnected.Server;

[MessagePackObject(false)]
public class ServerStatusResponsePacket : UnconnectedResponsePacket
{
	[Key(1)]
	public NetworkStatus Status { get; set; }

	[Key(2)]
	public byte[] StateToken { get; set; }

	[Key(3)]
	public int OnlineCount { get; set; }

	[Key(4)]
	public int MaxCount { get; set; }

	[Key(5)]
	public bool AdminOnly { get; set; }
}
