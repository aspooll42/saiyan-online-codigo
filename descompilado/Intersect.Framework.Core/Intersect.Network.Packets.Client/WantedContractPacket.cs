using System;
using Intersect.Config;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class WantedContractPacket : IntersectPacket
{
	[Key(0)]
	public WantedContractKind Kind { get; set; }

	[Key(1)]
	public Guid TargetId { get; set; }

	[Key(2)]
	public bool Take { get; set; }

	public WantedContractPacket()
	{
	}

	public WantedContractPacket(WantedContractKind kind, Guid targetId, bool take)
	{
		Kind = kind;
		TargetId = targetId;
		Take = take;
	}
}
