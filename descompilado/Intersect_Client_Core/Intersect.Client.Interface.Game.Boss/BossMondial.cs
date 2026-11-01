using System;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Boss;

public static class BossMondial
{
	private const long BandeauMs = 5200L;

	private const long ResultatMs = 11000L;

	private static BossMondialPacket? _etat;

	private static long _recuA;

	private static string _annonce = string.Empty;

	private static string _sous = string.Empty;

	private static long _annonceA = -5200L;

	private static BossMondialResultPacket? _resultat;

	private static long _resultatA = -11000L;

	private static long Maintenant => Timing.Global.MillisecondsUtc;

	public static BossMondialPacket? Paquet => _etat;

	public static int Etat => _etat?.Etat ?? 0;

	public static string Nom => _etat?.Nom ?? string.Empty;

	public static string Zone => _etat?.Zone ?? string.Empty;

	public static string NomAffiche => Traduction.Contenu(Nom);

	public static string ZoneAffichee => Traduction.Contenu(Zone);

	public static int Cristaux => _etat?.Cristaux ?? 0;

	public static int CristauxTotal => _etat?.CristauxTotal ?? 0;

	public static int Phase => _etat?.Phase ?? 0;

	public static long RestantMs
	{
		get
		{
			if (_etat != null)
			{
				return Math.Max(0L, _etat.RestantMs - (Maintenant - _recuA));
			}
			return 0L;
		}
	}

	public static float Vie
	{
		get
		{
			if (_etat != null)
			{
				return Math.Clamp((float)_etat.Vie / (float)Math.Max(1L, _etat.VieMax), 0f, 1f);
			}
			return 0f;
		}
	}

	public static bool SurPlace
	{
		get
		{
			if (_etat != null)
			{
				return Globals.Me?.MapId == _etat.CarteId;
			}
			return false;
		}
	}

	public static BossMondialResultPacket? Resultat => _resultat;

	public static bool BandeauVisible(out float fondu, out string titre, out string sous)
	{
		long num = Maintenant - _annonceA;
		titre = _annonce;
		sous = _sous;
		fondu = ((num < 0 || num > 5200 || string.IsNullOrEmpty(_annonce)) ? 0f : Math.Min(1f, Math.Min((float)num / 320f, (float)(5200 - num) / 420f)));
		return fondu > 0.01f;
	}

	public static bool ResultatVisible(out float fondu)
	{
		long num = Maintenant - _resultatA;
		fondu = ((num < 0 || num > 11000 || _resultat == null) ? 0f : Math.Min(1f, Math.Min((float)num / 360f, (float)(11000 - num) / 500f)));
		return fondu > 0.01f;
	}

	public static void OnEtat(BossMondialPacket paquet)
	{
		_recuA = Maintenant;
		if (!string.IsNullOrEmpty(paquet.Annonce))
		{
			_annonce = paquet.Annonce;
			_sous = paquet.Sous;
			_annonceA = _recuA;
		}
		_etat = ((paquet.Etat == 3) ? null : paquet);
	}

	public static void OnResultat(BossMondialResultPacket paquet)
	{
		_resultat = paquet;
		_resultatA = Maintenant;
	}

	public static string Horloge(long ms)
	{
		long num = Math.Max(0L, ms / 1000);
		return $"{num / 60}:{num % 60:00}";
	}
}
