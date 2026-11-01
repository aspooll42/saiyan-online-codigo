using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class CanStartQuestCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.CanStartQuest;

	public Guid QuestId { get; set; }
}
