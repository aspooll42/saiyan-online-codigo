using Intersect.Framework.Core.GameObjects.Events;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EventEntityPacket : EntityPacket
{
	[Key(24)]
	public bool DirectionFix { get; set; }

	[Key(25)]
	public bool WalkingAnim { get; set; }

	[Key(26)]
	public bool DisablePreview { get; set; }

	[Key(27)]
	public string Description { get; set; }

	[Key(28)]
	public EventGraphic Graphic { get; set; }

	[Key(29)]
	public byte RenderLayer { get; set; }

	[Key(30)]
	public EventTrigger Trigger { get; set; }

	[Key(31)]
	public byte QuestMarker { get; set; }
}
