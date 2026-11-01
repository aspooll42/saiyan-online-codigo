namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangeLevelCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ChangeLevel;

	public int Level { get; set; }
}
