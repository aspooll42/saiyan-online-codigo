using System;
using Intersect.Network;
using MessagePack;

namespace Saiyan.Paquets;

[MessagePackObject(false)]
public class CleDesRessourcesPacket : IntersectPacket
{
	[Key(0)]
	public byte[] Cle { get; set; } = Array.Empty<byte>();

	public CleDesRessourcesPacket()
	{
	}

	public CleDesRessourcesPacket(byte[] cle)
	{
		Cle = cle;
	}
}
