namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class OpenGuildBankCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.OpenGuildBank;
}
