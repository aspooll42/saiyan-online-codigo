namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class InGuildWithRank : Condition
{
	public override ConditionType Type { get; } = ConditionType.InGuildWithRank;

	public int Rank { get; set; }
}
