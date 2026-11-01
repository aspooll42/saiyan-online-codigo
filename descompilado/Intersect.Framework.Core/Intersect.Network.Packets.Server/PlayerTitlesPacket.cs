using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PlayerTitlesPacket : IntersectPacket
{
	[Key(0)]
	public Guid[] TitleIds { get; set; } = Array.Empty<Guid>();

	[Key(1)]
	public Guid ActiveTitleId { get; set; }

	public PlayerTitlesPacket()
	{
	}

	public PlayerTitlesPacket(Guid[] titleIds, Guid activeTitleId)
	{
		TitleIds = titleIds;
		ActiveTitleId = activeTitleId;
	}
}
