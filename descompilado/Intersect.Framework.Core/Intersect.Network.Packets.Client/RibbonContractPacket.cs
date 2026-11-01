using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject(false)]
public class RibbonContractPacket : IntersectPacket
{
	[Key(0)]
	public string Key { get; set; } = string.Empty;

	[Key(1)]
	public bool Take { get; set; }

	public RibbonContractPacket()
	{
	}

	public RibbonContractPacket(string key, bool take)
	{
		Key = key;
		Take = take;
	}
}
