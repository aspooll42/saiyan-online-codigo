using Intersect.Framework.Core.Security;
using Intersect.Network;
using MessagePack;

namespace Intersect.Framework.Core.Network.Packets.Security;

[MessagePackObject(false)]
public class PermissionSetPacket : IntersectPacket
{
	[Key(0)]
	public PermissionSet PermissionSet { get; set; }

	public PermissionSetPacket()
	{
	}

	public PermissionSetPacket(PermissionSet permissionSet)
	{
		PermissionSet = permissionSet;
	}
}
