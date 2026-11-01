using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class RibbonContractEntryPacket : IntersectPacket
{
	[Key(0)]
	public string Key { get; set; } = string.Empty;

	[Key(1)]
	public RibbonContractKind Kind { get; set; }

	[Key(2)]
	public int Grade { get; set; }

	[Key(3)]
	public string Name { get; set; } = string.Empty;

	[Key(4)]
	public string Sprite { get; set; } = string.Empty;

	[Key(5)]
	public string Face { get; set; } = string.Empty;

	[Key(6)]
	public string Icon { get; set; } = string.Empty;

	[Key(7)]
	public string Icon2 { get; set; } = string.Empty;

	[Key(8)]
	public int Level { get; set; }

	[Key(9)]
	public long Zeni { get; set; }

	[Key(10)]
	public int Reputation { get; set; }

	[Key(11)]
	public Guid MapId { get; set; }

	[Key(12)]
	public string Place { get; set; } = string.Empty;

	[Key(13)]
	public int Minutes { get; set; } = -1;

	[Key(14)]
	public int CriminalsDown { get; set; }

	[Key(15)]
	public bool Online { get; set; }

	[Key(16)]
	public bool Taken { get; set; }

	[Key(17)]
	public bool Done { get; set; }

	[Key(18)]
	public Guid TargetMapId { get; set; }

	[Key(19)]
	public int Objet { get; set; } = -1;

	[Key(20)]
	public bool Bois { get; set; }
}
