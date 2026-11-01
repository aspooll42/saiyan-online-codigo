using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RequestPasswordResetPacket : IntersectPacket
{
	[Key(0)]
	public string NameOrEmail { get; set; }

	public RequestPasswordResetPacket()
	{
	}

	public RequestPasswordResetPacket(string nameOrEmail)
	{
		NameOrEmail = nameOrEmail;
	}
}
