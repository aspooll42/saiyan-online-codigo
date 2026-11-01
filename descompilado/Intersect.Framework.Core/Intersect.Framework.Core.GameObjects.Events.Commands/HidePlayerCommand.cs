namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class HidePlayerCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.HidePlayer;
}
