using System;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class BooleanVariableComparison : VariableComparison
{
	public VariableType CompareVariableType { get; set; }

	public Guid CompareVariableId { get; set; }

	public bool ComparingEqual { get; set; }

	public bool Value { get; set; }
}
