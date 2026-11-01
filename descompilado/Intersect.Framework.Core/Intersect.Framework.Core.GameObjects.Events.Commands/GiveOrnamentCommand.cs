using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class GiveOrnamentCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.GiveOrnament;

	public Guid OrnamentId { get; set; }

	public bool Remove { get; set; }
}
