using System;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class SetActiveTitlePacket : IntersectPacket
{
	[Key(0)]
	public Guid TitleId { get; set; }

	public SetActiveTitlePacket()
	{
	}

	public SetActiveTitlePacket(Guid titleId)
	{
		TitleId = titleId;
	}
}
