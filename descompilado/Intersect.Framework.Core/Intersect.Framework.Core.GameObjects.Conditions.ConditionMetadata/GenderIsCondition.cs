using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class GenderIsCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.GenderIs;

	public Gender Gender { get; set; }
}
