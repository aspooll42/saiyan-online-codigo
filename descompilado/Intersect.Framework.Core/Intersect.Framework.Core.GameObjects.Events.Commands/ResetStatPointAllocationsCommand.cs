namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ResetStatPointAllocationsCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ResetStatPointAllocations;
}
