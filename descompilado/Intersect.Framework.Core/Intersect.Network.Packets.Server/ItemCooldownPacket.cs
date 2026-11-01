using System;
using System.Collections.Generic;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ItemCooldownPacket : IntersectPacket
{
	[Key(0)]
	public Dictionary<Guid, long> ItemCds;

	public ItemCooldownPacket()
	{
	}

	public ItemCooldownPacket(Dictionary<Guid, long> itemCds)
	{
		ItemCds = itemCds;
	}
}
