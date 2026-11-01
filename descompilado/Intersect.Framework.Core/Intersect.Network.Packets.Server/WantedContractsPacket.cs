using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class WantedContractsPacket : IntersectPacket
{
	[Key(0)]
	public WantedContractEntryPacket[] Entries { get; set; } = Array.Empty<WantedContractEntryPacket>();

	public WantedContractsPacket()
	{
	}

	public WantedContractsPacket(WantedContractEntryPacket[] entries)
	{
		Entries = entries;
	}
}
