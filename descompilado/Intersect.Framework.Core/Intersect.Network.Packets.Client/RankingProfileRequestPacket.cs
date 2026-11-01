using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RankingProfileRequestPacket : IntersectPacket
{
	[Key(0)]
	public Guid PlayerId { get; set; }

	public RankingProfileRequestPacket()
	{
	}

	public RankingProfileRequestPacket(Guid playerId)
	{
		PlayerId = playerId;
	}
}
