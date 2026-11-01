using System;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class GiveTitleCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.GiveTitle;

	public Guid TitleId { get; set; }

	public bool Remove { get; set; }
}
