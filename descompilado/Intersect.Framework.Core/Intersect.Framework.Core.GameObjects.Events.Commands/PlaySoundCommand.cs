namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class PlaySoundCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.PlaySound;

	public string File { get; set; } = string.Empty;
}
