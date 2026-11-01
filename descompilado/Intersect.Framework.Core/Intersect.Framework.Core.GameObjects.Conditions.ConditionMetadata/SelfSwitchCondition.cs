namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class SelfSwitchCondition : Condition
{
	public override ConditionType Type { get; } = ConditionType.SelfSwitch;

	public int SwitchIndex { get; set; }

	public bool Value { get; set; }
}
