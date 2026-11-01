namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ScreenFadeCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.Fade;

	public FadeType FadeType { get; set; }

	public bool WaitForCompletion { get; set; }

	public int DurationMs { get; set; }
}
