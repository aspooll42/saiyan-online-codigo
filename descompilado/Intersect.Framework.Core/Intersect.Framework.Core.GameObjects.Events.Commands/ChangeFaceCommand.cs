namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangeFaceCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ChangeFace;

	public string Face { get; set; } = string.Empty;
}
