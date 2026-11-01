using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TradeStatusPacket : IntersectPacket
{
	[Key(0)]
	public long MyZeni { get; set; }

	[Key(1)]
	public long TheirZeni { get; set; }

	[Key(2)]
	public bool MyReady { get; set; }

	[Key(3)]
	public bool TheirReady { get; set; }

	[Key(4)]
	public int Revision { get; set; }

	public TradeStatusPacket()
	{
	}

	public TradeStatusPacket(long myZeni, long theirZeni, bool myReady, bool theirReady, int revision)
	{
		MyZeni = myZeni;
		TheirZeni = theirZeni;
		MyReady = myReady;
		TheirReady = theirReady;
		Revision = revision;
	}
}
