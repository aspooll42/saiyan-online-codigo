using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class UserRegistrationRequestPacket : IntersectPacket
{
	[Key(0)]
	public string Username { get; set; }

	[Key(1)]
	public string Password { get; set; }

	[Key(2)]
	public string Email { get; set; }

	[Key(3)]
	public string MachineId { get; set; }

	public UserRegistrationRequestPacket()
	{
	}

	public UserRegistrationRequestPacket(string username, string password, string email)
	{
		Username = username;
		Password = password;
		Email = email;
	}
}
