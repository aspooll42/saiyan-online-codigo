namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ReleasePlayerCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ReleasePlayer;
}
