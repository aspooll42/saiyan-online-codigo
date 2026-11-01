namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class SetAccessCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.SetAccess;

	public Access Access { get; set; }
}
