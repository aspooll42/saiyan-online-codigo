namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class OpenBankCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.OpenBank;
}
