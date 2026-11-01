using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayMusicPacket : IntersectPacket
{
	[Key(0)]
	public string BGM { get; set; }

	public PlayMusicPacket()
	{
	}

	public PlayMusicPacket(string bgm)
	{
		BGM = bgm;
	}
}
