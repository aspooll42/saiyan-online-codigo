using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class PasswordChangeRequestPacket : IntersectPacket
{
	[Key(0)]
	public string? Identifier { get; set; }

	[Key(1)]
	public string? Token { get; set; }

	[Key(2)]
	public string? Password { get; set; }

	public PasswordChangeRequestPacket()
	{
	}

	public PasswordChangeRequestPacket(string identifier, string token, string password)
	{
		Identifier = identifier;
		Token = token;
		Password = password;
	}
}
