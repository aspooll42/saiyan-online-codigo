using Intersect.Framework.Core.Security;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class PasswordChangeResultPacket : IntersectPacket
{
	[Key(0)]
	public PasswordResetResultType ResultType { get; set; }

	public PasswordChangeResultPacket()
	{
	}

	public PasswordChangeResultPacket(PasswordResetResultType succeeded)
	{
		ResultType = succeeded;
	}
}
