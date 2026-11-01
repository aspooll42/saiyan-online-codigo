namespace Intersect.Framework.Core.GameObjects.Conditions.ConditionMetadata;

public class CheckEquippedSlot : Condition
{
	public override ConditionType Type { get; } = ConditionType.CheckEquipment;

	public string Name { get; set; }
}
