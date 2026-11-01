using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CharacterCreationPacket : IntersectPacket
{
	[Key(0)]
	public bool Force { get; set; }

	public CharacterCreationPacket()
	{
	}

	public CharacterCreationPacket(bool force)
	{
		Force = force;
	}
}
