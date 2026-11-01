using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class HellStatusPacket : IntersectPacket
{
	[Key(0)]
	public long SentenceMs { get; set; }

	[Key(1)]
	public long Bail { get; set; }

	[Key(2)]
	public DetentionPlace Place { get; set; }

	public HellStatusPacket()
	{
	}

	public HellStatusPacket(long sentenceMs, long bail, DetentionPlace place)
	{
		SentenceMs = sentenceMs;
		Bail = bail;
		Place = place;
	}
}
