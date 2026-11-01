namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class ExitEventProcessingCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.ExitEventProcess;
}
