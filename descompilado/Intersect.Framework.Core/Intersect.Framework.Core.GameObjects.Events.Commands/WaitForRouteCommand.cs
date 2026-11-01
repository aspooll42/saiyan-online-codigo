using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class WaitForRouteCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.WaitForRouteCompletion;

	public Guid TargetId { get; set; }
}
