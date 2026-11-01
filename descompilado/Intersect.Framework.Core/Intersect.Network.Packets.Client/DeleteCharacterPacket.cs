using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class DeleteCharacterPacket : IntersectPacket
{
	[Key(0)]
	public Guid CharacterId { get; set; }

	public DeleteCharacterPacket()
	{
	}

	public DeleteCharacterPacket(Guid charId)
	{
		CharacterId = charId;
	}
}
