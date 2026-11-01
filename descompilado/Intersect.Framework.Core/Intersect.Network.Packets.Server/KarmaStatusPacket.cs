using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class KarmaStatusPacket : IntersectPacket
{
	[Key(0)]
	public Guid PlayerId { get; set; }

	[Key(1)]
	public int Tier { get; set; }

	[Key(2)]
	public long AggressorRemainingMs { get; set; }

	public KarmaStatusPacket()
	{
	}

	public KarmaStatusPacket(Guid playerId, int tier, long aggressorRemainingMs)
	{
		PlayerId = playerId;
		Tier = tier;
		AggressorRemainingMs = aggressorRemainingMs;
	}
}
