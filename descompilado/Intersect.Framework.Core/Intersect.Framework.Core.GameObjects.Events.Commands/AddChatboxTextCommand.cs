using Intersect.Enums;

namespace Intersect.Framework.Core.GameObjects.Events.Commands;

public class AddChatboxTextCommand : EventCommand
{
	public override EventCommandType Type { get; } = EventCommandType.AddChatboxText;

	public string Text { get; set; } = string.Empty;

	public ChatMessageType MessageType { get; set; } = ChatMessageType.Notice;

	public string Color { get; set; } = string.Empty;

	public ChatboxChannel Channel { get; set; }

	public bool ShowChatBubble { get; set; }

	public bool ShowChatBubbleInProximity { get; set; }
}
