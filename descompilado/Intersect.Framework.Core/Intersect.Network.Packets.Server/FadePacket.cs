using Intersect.Framework.Core.GameObjects.Events;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class FadePacket : IntersectPacket
{
	[Key(0)]
	public FadeType FadeType { get; set; }

	[Key(1)]
	public bool WaitForCompletion { get; set; }

	[Key(2)]
	public int DurationMs { get; set; }

	public FadePacket()
	{
	}

	public FadePacket(FadeType fadeType, bool waitForCompletion, int durationMs)
	{
		FadeType = fadeType;
		WaitForCompletion = waitForCompletion;
		DurationMs = durationMs;
	}
}
