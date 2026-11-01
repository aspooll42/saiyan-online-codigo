namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ChangePlayerLabelCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.PlayerLabel;

	public string Value { get; set; }

	public int Position { get; set; }

	public Color Color { get; set; }

	public bool MatchNameColor { get; set; }
}
