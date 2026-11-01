namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class RestoreMpCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.RestoreMp;

	public int Amount { get; set; }
}
