using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class JoinGamePacket : AbstractTimedPacket
{
}
