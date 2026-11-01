namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class DespawnNpcCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.DespawnNpc;
}
