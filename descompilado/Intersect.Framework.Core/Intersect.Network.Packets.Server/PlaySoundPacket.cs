using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlaySoundPacket : IntersectPacket
{
	[Key(0)]
	public string Sound { get; set; }

	public PlaySoundPacket()
	{
	}

	public PlaySoundPacket(string sound)
	{
		Sound = sound;
	}
}
