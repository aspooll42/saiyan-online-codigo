using Intersect.Network;
using MessagePack;

namespace Intersect.Framework.Core.Network.Packets.Security;

[MessagePackObject(false)]
public class ActivePermissionSetPacket : IntersectPacket
{
	[Key(0)]
	public string PermissionSetName { get; set; }

	public ActivePermissionSetPacket()
	{
	}

	public ActivePermissionSetPacket(string permissionSetName)
	{
		PermissionSetName = permissionSetName;
	}
}
