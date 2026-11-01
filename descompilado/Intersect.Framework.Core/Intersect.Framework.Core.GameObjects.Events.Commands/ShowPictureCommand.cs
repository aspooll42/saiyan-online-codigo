namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ShowPictureCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ShowPicture;

	public string File { get; set; } = string.Empty;

	public int Size { get; set; }

	public bool Clickable { get; set; }

	public int HideTime { get; set; }

	public bool WaitUntilClosed { get; set; }
}
