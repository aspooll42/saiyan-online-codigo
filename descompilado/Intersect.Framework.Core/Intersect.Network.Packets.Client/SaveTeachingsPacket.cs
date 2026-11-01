using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SaveTeachingsPacket : IntersectPacket
{
	[Key(0)]
	public int Main { get; set; }

	[Key(1)]
	public int Secondary { get; set; }

	[Key(2)]
	public Dictionary<string, int> Ranks { get; set; } = new Dictionary<string, int>();

	public SaveTeachingsPacket()
	{
	}

	public SaveTeachingsPacket(int main, int secondary, Dictionary<string, int> ranks)
	{
		Main = main;
		Secondary = secondary;
		Ranks = ranks;
	}
}
