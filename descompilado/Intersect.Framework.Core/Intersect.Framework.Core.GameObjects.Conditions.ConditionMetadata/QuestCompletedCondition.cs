using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class QuestCompletedCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.QuestCompleted;

	public Guid QuestId { get; set; }
}
