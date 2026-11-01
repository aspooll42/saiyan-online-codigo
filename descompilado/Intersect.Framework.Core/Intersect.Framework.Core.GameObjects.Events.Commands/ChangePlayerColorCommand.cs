namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangePlayerColorCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ChangePlayerColor;

	public Color Color { get; set; } = new Color(255, 255, 255, 255);
}
