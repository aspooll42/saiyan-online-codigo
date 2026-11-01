using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class LoginPacket : IntersectPacket
{
	[Key(0)]
	public string Username { get; set; }

	[Key(1)]
	public string Password { get; set; }

	[Key(2)]
	public string MachineId { get; set; }

	public LoginPacket()
	{
	}

	public LoginPacket(string username, string password)
	{
		Username = username;
		Password = password;
	}
}
