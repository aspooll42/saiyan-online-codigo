using Intersect.Framework.Core.GameObjects.Rankings;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RankingRequestPacket : IntersectPacket
{
	[Key(0)]
	public RankingQuery Query { get; set; } = new RankingQuery();

	public RankingRequestPacket()
	{
	}

	public RankingRequestPacket(RankingQuery query)
	{
		Query = query;
	}
}
