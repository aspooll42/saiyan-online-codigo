using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PartyPacket : IntersectPacket
{
	[Key(0)]
	public PartyMemberPacket[] MemberData { get; set; }

	public PartyPacket()
	{
	}

	public PartyPacket(PartyMemberPacket[] memberData)
	{
		MemberData = memberData;
	}
}
