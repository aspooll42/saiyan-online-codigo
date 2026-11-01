using System;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class HasFreeInventorySlots : Condition
{
	public override ConditionType Type { get; } = ConditionType.HasFreeInventorySlots;

	public int Quantity { get; set; }

	public bool UseVariable { get; set; }

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }
}
