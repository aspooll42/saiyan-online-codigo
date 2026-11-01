namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class SetSelfSwitchCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.SetSelfSwitch;

	public int SwitchId { get; set; }

	public bool Value { get; set; }
}
