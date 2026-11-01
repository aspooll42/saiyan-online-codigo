using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class NpcNoticePacket : IntersectPacket
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public Guid MapId { get; set; }

	[Key(2)]
	public int DurationMs { get; set; }

	[Key(3)]
	public Guid Cible { get; set; }

	[Key(4)]
	public NpcNoticeGenre Genre { get; set; }

	[Key(5)]
	public bool Marque { get; set; }

	[Key(6)]
	public byte Pas { get; set; }

	public NpcNoticePacket()
	{
	}

	public NpcNoticePacket(Guid id, Guid mapId, int durationMs)
	{
		Id = id;
		MapId = mapId;
		DurationMs = durationMs;
	}

	public NpcNoticePacket(Guid id, Guid mapId, int durationMs, Guid cible, NpcNoticeGenre genre, bool marque, byte pas = 0)
		: this(id, mapId, durationMs)
	{
		Cible = cible;
		Genre = genre;
		Marque = marque;
		Pas = pas;
	}
}
