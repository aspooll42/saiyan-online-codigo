namespace Intersect.Framework.Core.GameObjects.Events;

public class EventMovement
{
	public EventMovementType Type { get; set; }

	public EventMovementFrequency Frequency { get; set; } = EventMovementFrequency.Normal;

	public EventMovementSpeed Speed { get; set; } = EventMovementSpeed.Normal;

	public EventMoveRoute Route { get; set; } = new EventMoveRoute();
}
