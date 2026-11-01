namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class StopSoundsCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.StopSounds;
}
