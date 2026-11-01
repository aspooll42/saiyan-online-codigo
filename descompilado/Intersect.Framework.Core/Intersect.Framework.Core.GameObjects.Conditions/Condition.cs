namespace Intersect.Framework.Core.GameObjects.Conditions;

public class Condition
{
	public virtual ConditionType Type { get; }

	public bool Negated { get; set; }

	public bool ElseEnabled { get; set; } = true;
}
