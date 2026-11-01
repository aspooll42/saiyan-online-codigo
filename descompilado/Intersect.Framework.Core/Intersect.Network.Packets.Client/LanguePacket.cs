using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class LanguePacket : IntersectPacket
{
	[Key(0)]
	public string Langue { get; set; } = string.Empty;

	public LanguePacket()
	{
	}

	public LanguePacket(string langue)
	{
		Langue = langue;
	}
}
