using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CharactersPacket : IntersectPacket
{
	[Key(0)]
	public string Username { get; set; }

	[Key(1)]
	public CharacterPacket[] Characters { get; set; }

	[Key(2)]
	public bool FreeSlot { get; set; }

	public CharactersPacket()
	{
	}

	public CharactersPacket(string username, CharacterPacket[] characters, bool freeSlot)
	{
		Username = username;
		Characters = characters;
		FreeSlot = freeSlot;
	}
}
