namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class SetMoveRouteCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.SetMoveRoute;

	public EventMoveRoute Route { get; set; } = new EventMoveRoute();
}
