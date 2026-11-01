using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class ClassIsCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.ClassIs;

	public Guid ClassId { get; set; }
}
