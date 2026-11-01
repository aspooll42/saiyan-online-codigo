using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class LabelPacket : IntersectPacket
{
	[Key(0)]
	public Color Color;

	[Key(1)]
	public string Label;

	public LabelPacket()
	{
	}

	public LabelPacket(string label, Color color)
	{
		Label = label;
		Color = color;
	}
}
