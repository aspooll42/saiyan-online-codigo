namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class LevelUpCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.LevelUp;
}
