namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class LabelCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.Label;

	public string Label { get; set; }
}
