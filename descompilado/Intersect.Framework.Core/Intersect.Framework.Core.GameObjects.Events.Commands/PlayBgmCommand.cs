namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class PlayBgmCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.PlayBgm;

	public string File { get; set; } = string.Empty;
}
