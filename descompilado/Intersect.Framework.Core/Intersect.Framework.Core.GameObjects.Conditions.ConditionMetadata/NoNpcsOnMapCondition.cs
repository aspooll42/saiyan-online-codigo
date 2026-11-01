using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class NoNpcsOnMapCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.NoNpcsOnMap;

	public bool SpecificNpc { get; set; }

	public Guid NpcId { get; set; }
}
