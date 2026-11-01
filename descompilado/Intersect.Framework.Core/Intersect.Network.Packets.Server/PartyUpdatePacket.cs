using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PartyUpdatePacket : IntersectPacket
{
	[Key(0)]
	public int MemberIndex { get; set; }

	[Key(1)]
	public PartyMemberPacket MemberData { get; set; }

	public PartyUpdatePacket()
	{
	}

	public PartyUpdatePacket(int memberIndex, PartyMemberPacket memberData)
	{
		MemberIndex = memberIndex;
		MemberData = memberData;
	}
}
