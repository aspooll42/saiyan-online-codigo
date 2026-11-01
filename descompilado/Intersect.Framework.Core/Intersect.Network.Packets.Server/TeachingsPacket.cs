using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TeachingsPacket : IntersectPacket
{
	[Key(0)]
	public int Main { get; set; }

	[Key(1)]
	public int Secondary { get; set; }

	[Key(2)]
	public Dictionary<string, int> Ranks { get; set; } = new Dictionary<string, int>();

	[Key(3)]
	public Dictionary<string, bool> Trials { get; set; } = new Dictionary<string, bool>();

	public TeachingsPacket()
	{
	}

	public TeachingsPacket(int main, int secondary, Dictionary<string, int> ranks, Dictionary<string, bool> trials)
	{
		Main = main;
		Secondary = secondary;
		Ranks = ranks;
		Trials = trials;
	}
}
