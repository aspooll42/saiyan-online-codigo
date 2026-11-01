using System;
using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class IntegerVariableComparison : VariableComparison
{
	public VariableComparator Comparator { get; set; }

	public VariableType CompareVariableType { get; set; }

	public Guid CompareVariableId { get; set; }

	public long Value { get; set; }

	public long MaxValue { get; set; }

	public bool TimeSystem { get; set; }
}
