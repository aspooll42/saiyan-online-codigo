using Intersect.Framework.Core.GameObjects.Housing;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class HouseSetAccessPacket : IntersectPacket
{
	[Key(0)]
	public HouseAccess Access { get; set; }

	public HouseSetAccessPacket()
	{
	}

	public HouseSetAccessPacket(HouseAccess access)
	{
		Access = access;
	}
}
