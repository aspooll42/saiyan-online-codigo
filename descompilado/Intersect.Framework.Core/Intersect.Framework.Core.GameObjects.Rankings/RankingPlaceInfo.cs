using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Rankings;

[MessagePackObject(false)]
public sealed class RankingPlaceInfo
{
	[Key(0)]
	public RankingQuery Query { get; set; } = new RankingQuery();

	[Key(1)]
	public int Rank { get; set; }
}
