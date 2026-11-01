using System;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class VariableIsCondition : Condition
{
	public override ConditionType Type { get; }

	public VariableType VariableType { get; set; }

	public Guid VariableId { get; set; }

	public VariableComparison Comparison { get; set; } = new VariableComparison();
}
