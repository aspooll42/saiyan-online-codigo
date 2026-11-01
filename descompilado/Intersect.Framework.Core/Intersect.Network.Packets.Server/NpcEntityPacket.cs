using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class NpcEntityPacket : EntityPacket
{
	[Key(24)]
	public NpcAggression Aggression { get; set; }

	[Key(25)]
	public bool Bleeds { get; set; } = true;

	[Key(26)]
	public int BossEnragePercent { get; set; }

	[Key(27)]
	public float RayonDuCorps { get; set; } = 10f;

	[Key(28)]
	public byte BossVisual { get; set; }
}
