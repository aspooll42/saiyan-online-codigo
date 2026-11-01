using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class EquipmentFragment
{
	[Key(0)]
	public string Name { get; set; }

	[Key(1)]
	public Color RenderColor { get; set; }
}
