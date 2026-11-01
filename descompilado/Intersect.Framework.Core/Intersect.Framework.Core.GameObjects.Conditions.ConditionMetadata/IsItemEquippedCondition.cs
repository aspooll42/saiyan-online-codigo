using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class IsItemEquippedCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.IsItemEquipped;

	public Guid ItemId { get; set; }
}
