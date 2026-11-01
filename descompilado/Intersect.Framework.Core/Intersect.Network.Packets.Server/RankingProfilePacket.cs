using System;
using Intersect.Framework.Core.GameObjects.Rankings;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RankingProfilePacket : IntersectPacket
{
	[Key(0)]
	public Guid PlayerId { get; set; }

	[Key(1)]
	public bool Found { get; set; }

	[Key(2)]
	public string Name { get; set; } = string.Empty;

	[Key(3)]
	public Guid ClassId { get; set; }

	[Key(4)]
	public string Sprite { get; set; } = string.Empty;

	[Key(5)]
	public int Level { get; set; }

	[Key(6)]
	public long Power { get; set; }

	[Key(7)]
	public int AchievementPoints { get; set; }

	[Key(8)]
	public string GuildName { get; set; } = string.Empty;

	[Key(9)]
	public Guid TitleId { get; set; }

	[Key(10)]
	public RankingPlaceInfo[] Places { get; set; } = Array.Empty<RankingPlaceInfo>();

	[Key(11)]
	public bool Online { get; set; }
}
