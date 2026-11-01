using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SelectCharacterPacket : IntersectPacket
{
	[Key(0)]
	public Guid CharacterId { get; set; }

	public SelectCharacterPacket()
	{
	}

	public SelectCharacterPacket(Guid charId)
	{
		CharacterId = charId;
	}
}
