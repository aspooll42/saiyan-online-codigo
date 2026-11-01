using System;
using System.Net;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public static class HeritageDelaiDEssai
{
	public const int PlafondMs = 300;

	public const int PlancherMs = 6;

	private static ILogger? _journal;

	private static bool _mesureDite;

	public static int Ms { get; private set; }

	public static bool Operant => false;

	public static void Demander(int demandeMs, string? hote, ILogger journal)
	{
		_journal = journal;
		Ms = 0;
		_mesureDite = false;
		if (demandeMs < 6)
		{
			journal.LogWarning("delai d'essai refuse : {Demande} ms demandes, rien n'est retenu sous {Plancher} ms", demandeMs, 6);
		}
		else if (!EstLocal(hote))
		{
			journal.LogWarning("delai d'essai refuse : le serveur {Hote} n'est pas sur ce poste", hote);
		}
		else
		{
			Ms = Math.Min(demandeMs, 300);
			journal.LogWarning("delai d'essai demande au lancement : {Ms} ms (--delai-essai {Demande}), pour ce lancement seulement", Ms, demandeMs);
		}
	}

	public static void Poser(NetManager reseau, bool serveur, string? hote)
	{
		if (!(Ms <= 0 || serveur))
		{
			if (!EstLocal(hote))
			{
				_journal?.LogWarning("delai d'essai refuse : ce reseau appelle {Hote}, qui n'est pas ce poste", hote);
				return;
			}
			if (!Operant)
			{
				_journal?.LogWarning("delai d'essai demande mais inoperant : la retenue des paquets n'existe que dans une compilation Debug");
				return;
			}
			reseau.SimulateLatency = true;
			reseau.SimulationMinLatency = Ms;
			reseau.SimulationMaxLatency = Ms;
			_mesureDite = false;
			_journal?.LogWarning("delai d'essai : {Ms} ms (chaque paquet recu est retenu, les envois partent tout de suite)", Ms);
		}
	}

	public static void DireLaMesure(int demiAllerRetourMs)
	{
		if (Ms > 0 && !_mesureDite && demiAllerRetourMs >= 0)
		{
			_mesureDite = true;
			_journal?.LogWarning("delai d'essai : aller-retour mesure a environ {Mesure} ms, pour {Ms} ms demandes", demiAllerRetourMs * 2, Ms);
		}
	}

	private static bool EstLocal(string? hote)
	{
		hote = hote?.Trim();
		if (!string.Equals(hote, "localhost", StringComparison.OrdinalIgnoreCase))
		{
			if (IPAddress.TryParse(hote, out IPAddress address))
			{
				return IPAddress.IsLoopback(address);
			}
			return false;
		}
		return true;
	}
}
