using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class JoueursEnLignePacket : IntersectPacket
{
	[Key(0)]
	public int Nombre { get; set; }

	public JoueursEnLignePacket()
	{
	}

	public JoueursEnLignePacket(int nombre)
	{
		Nombre = nombre;
	}
}
