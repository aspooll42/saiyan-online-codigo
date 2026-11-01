using System;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Events;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayerEntityPacket : EntityPacket
{
	[Key(24)]
	public Access AccessLevel { get; set; }

	[Key(25)]
	public Gender Gender { get; set; }

	[Key(26)]
	public Guid ClassId { get; set; }

	[Key(27)]
	public EquipmentPacket Equipment { get; set; }

	[Key(28)]
	public long CombatTimeRemaining { get; set; }

	[Key(29)]
	public string Guild { get; set; }

	[Key(30)]
	public int GuildRank { get; set; }

	[Key(31)]
	public int KarmaTier { get; set; }

	[Key(32)]
	public long AggressorRemainingMs { get; set; }

	[Key(33)]
	public Guid TitleId { get; set; }

	[Key(34)]
	public Guid OrnamentId { get; set; }

	[Key(35)]
	public int FlightMode { get; set; }

	[Key(36)]
	public string GuildTag { get; set; } = string.Empty;

	[Key(37)]
	public int GuildEmblem { get; set; }

	[Key(38)]
	public int GuildColor { get; set; }
}
