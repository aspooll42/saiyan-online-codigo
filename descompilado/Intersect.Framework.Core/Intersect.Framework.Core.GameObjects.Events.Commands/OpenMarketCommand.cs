namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class OpenMarketCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.OpenMarket;
}
