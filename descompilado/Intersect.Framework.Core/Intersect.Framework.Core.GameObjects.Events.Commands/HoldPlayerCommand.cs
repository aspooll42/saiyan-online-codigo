namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class HoldPlayerCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.HoldPlayer;
}
