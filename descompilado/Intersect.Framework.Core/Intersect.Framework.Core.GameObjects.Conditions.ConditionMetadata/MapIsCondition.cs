using System;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class MapIsCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.MapIs;

	public Guid MapId { get; set; }
}
