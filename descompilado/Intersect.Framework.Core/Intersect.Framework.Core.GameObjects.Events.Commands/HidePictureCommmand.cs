namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class HidePictureCommmand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.HidePicture;
}
