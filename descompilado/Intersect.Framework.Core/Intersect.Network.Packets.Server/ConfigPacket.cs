using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class ConfigPacket : IntersectPacket
{
	[Key(0)]
	public string Config { get; set; }

	public ConfigPacket()
	{
	}

	public ConfigPacket(string config)
	{
		Config = config;
	}
}
