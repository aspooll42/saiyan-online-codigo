using Intersect.Framework.Core.GameObjects.Events;

namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class StringVariableComparison : VariableComparison
{
	public StringVariableComparator Comparator { get; set; }

	public string Value { get; set; }
}
