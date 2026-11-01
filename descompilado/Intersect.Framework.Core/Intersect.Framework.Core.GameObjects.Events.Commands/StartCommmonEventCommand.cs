using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class StartCommmonEventCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.StartCommonEvent;

	public bool AllInInstance { get; set; }

	public bool AllowInOverworld { get; set; }

	public Guid EventId { get; set; }
}
