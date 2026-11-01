using MessagePack;

namespace Intersect.Network.Packets.Unconnected.Client;

[MessagePackObject(false)]
public class ServerStatusRequestPacket : UnconnectedRequestPacket
{
	[Key(2)]
	public byte[] VersionData { get; set; }

	[Key(3)]
	public byte[] StateToken { get; set; }

	[SerializationConstructor]
	public ServerStatusRequestPacket()
	{
	}

	public ServerStatusRequestPacket(byte[] responseKey)
		: base(responseKey)
	{
	}
}
