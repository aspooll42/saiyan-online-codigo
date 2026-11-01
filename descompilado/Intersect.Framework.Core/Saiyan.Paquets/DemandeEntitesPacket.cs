using System;
using Intersect.Network;
using MessagePack;

namespace Saiyan.Paquets;

[MessagePackObject(false)]
public class DemandeEntitesPacket : IntersectPacket
{
	public const byte PerteInconnue = 0;

	public const byte PerteHorsDeVue = 1;

	public const byte PerteCarteAbsente = 2;

	public const byte PerteListe = 3;

	public const byte PerteDepart = 4;

	public const byte PerteImmobile = 128;

	[Key(0)]
	public Guid[] Ids { get; set; } = Array.Empty<Guid>();

	[Key(1)]
	public byte[] Raisons { get; set; } = Array.Empty<byte>();

	public DemandeEntitesPacket()
	{
	}

	public DemandeEntitesPacket(Guid[] ids)
	{
		Ids = ids;
	}

	public DemandeEntitesPacket(Guid[] ids, byte[] raisons)
	{
		Ids = ids;
		Raisons = raisons;
	}

	public static string NomDeLaRaison(byte raison)
	{
		string text = (raison & -129) switch
		{
			1 => "hors vue", 
			2 => "carte absente", 
			3 => "liste", 
			4 => "depart", 
			_ => "jamais vue", 
		};
		if ((raison & 0x80) == 0)
		{
			return text;
		}
		return text + ", immobile";
	}
}
