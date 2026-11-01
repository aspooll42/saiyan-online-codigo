namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class CombatCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.IsInCombat;
}
