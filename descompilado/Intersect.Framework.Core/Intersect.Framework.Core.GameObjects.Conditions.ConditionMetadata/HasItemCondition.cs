using System;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class HasItemCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.HasItem;

	public Guid ItemId { get; set; }

	public int Quantity { get; set; }

	public bool UseVariable { get; set; }

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }

	public bool CheckBank { get; set; }
}
