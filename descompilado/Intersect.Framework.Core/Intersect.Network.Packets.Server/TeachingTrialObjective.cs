using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TeachingTrialObjective
{
	[Key(0)]
	public string Label { get; set; } = string.Empty;

	[Key(1)]
	public int Kind { get; set; }

	[Key(2)]
	public int Current { get; set; }

	[Key(3)]
	public int Max { get; set; }

	[Key(4)]
	public bool Done { get; set; }

	[Key(5)]
	public bool Bonus { get; set; }
}
