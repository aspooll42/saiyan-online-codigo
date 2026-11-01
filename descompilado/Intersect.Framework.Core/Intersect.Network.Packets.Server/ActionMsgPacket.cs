using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ActionMsgPacket : IntersectPacket
{
	[Key(0)]
	public Guid MapId { get; set; }

	[Key(1)]
	public int X { get; set; }

	[Key(2)]
	public int Y { get; set; }

	[Key(3)]
	public string Message { get; set; }

	[Key(4)]
	public Color Color { get; set; }

	[Key(5)]
	public Guid EntityId { get; set; }

	[Key(6)]
	public Guid AttackerId { get; set; }

	public ActionMsgPacket()
	{
	}

	public ActionMsgPacket(Guid mapId, int x, int y, string message, Color color, Guid entityId = default(Guid), Guid attackerId = default(Guid))
	{
		MapId = mapId;
		X = x;
		Y = y;
		Message = message;
		Color = color;
		EntityId = entityId;
		AttackerId = attackerId;
	}
}
