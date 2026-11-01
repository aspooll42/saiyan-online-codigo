using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class TeachingTrialResultPacket : IntersectPacket
{
	[Key(0)]
	public string TrialId { get; set; } = string.Empty;

	[Key(1)]
	public string Name { get; set; } = string.Empty;

	[Key(2)]
	public int Master { get; set; }

	[Key(3)]
	public bool Success { get; set; }

	[Key(4)]
	public bool Bonus { get; set; }

	[Key(5)]
	public string Message { get; set; } = string.Empty;
}
