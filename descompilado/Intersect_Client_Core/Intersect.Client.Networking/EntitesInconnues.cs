using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Saiyan.Paquets;

namespace Intersect.Client.Networking;

internal static class EntitesInconnues
{
	private sealed class Suivi
	{
		public long Premier;

		public long Dernier;

		public long Demande;

		public int Demandes;

		public bool Immobile;
	}

	internal const long AttenteMs = 300L;

	internal const long RelanceMs = 3000L;

	internal const long OubliApresDeuxDemandesMs = 60000L;

	private const long OubliMs = 5000L;

	private const long TourMs = 500L;

	private const int ParDemande = 16;

	internal const long SursisHorsDeVueMs = 1500L;

	internal const long RecensementMs = 5000L;

	internal const long RecensementApresPassageMs = 800L;

	private const int PertesRetenues = 4096;

	private static readonly Dictionary<Guid, Suivi> Suivis = new Dictionary<Guid, Suivi>();

	private static readonly Dictionary<Guid, byte> DernieresPertes = new Dictionary<Guid, byte>();

	private static long _prochainTour;

	private static long _prochainRecensement;

	private static Guid _carteDuRecensement;

	public static void NoterPerte(Guid id, byte raison)
	{
		if (DernieresPertes.Count >= 4096)
		{
			DernieresPertes.Clear();
		}
		DernieresPertes[id] = raison;
	}

	internal static bool SursisEcoule(long horsDeVueDepuis, long maintenant)
	{
		long num = maintenant - horsDeVueDepuis;
		if (num < 1500)
		{
			return num < 0;
		}
		return true;
	}

	public static void Recenser(Guid carte, Guid[]? ids)
	{
		Player me = Globals.Me;
		if (me == null || ids == null || me.MapId != carte)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Guid guid in ids)
		{
			if (!(guid == me.Id) && !Globals.Entities.ContainsKey(guid))
			{
				if (Suivis.TryGetValue(guid, out Suivi value))
				{
					value.Premier = Math.Min(value.Premier, milliseconds - 300);
					value.Dernier = milliseconds;
					continue;
				}
				Suivis[guid] = new Suivi
				{
					Premier = milliseconds - 300,
					Dernier = milliseconds,
					Immobile = true
				};
			}
		}
	}

	public static void Noter(Guid id, Guid carte = default(Guid))
	{
		if (id == Guid.Empty)
		{
			return;
		}
		Guid? obj = Globals.Me?.Id;
		if (!(id == obj) && (!(carte != Guid.Empty) || (MapInstance.TryGet(carte, out MapInstance instance) && instance.InView())))
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (!Suivis.TryGetValue(id, out Suivi value))
			{
				Suivis[id] = new Suivi
				{
					Premier = milliseconds,
					Dernier = milliseconds
				};
			}
			else
			{
				value.Dernier = milliseconds;
				value.Immobile = false;
			}
		}
	}

	public static void Tour()
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (_prochainTour - milliseconds > 500)
		{
			_prochainTour = milliseconds;
		}
		if (_prochainRecensement - milliseconds > 5000)
		{
			_prochainRecensement = milliseconds;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			Suivis.Clear();
			DernieresPertes.Clear();
			return;
		}
		DemanderLeRecensement(me, milliseconds);
		if (Suivis.Count == 0 || milliseconds < _prochainTour)
		{
			return;
		}
		_prochainTour = milliseconds + 500;
		List<Guid> list = null;
		List<byte> list2 = null;
		List<Guid> list3 = null;
		foreach (var (guid2, suivi2) in Suivis)
		{
			if (Globals.Entities.ContainsKey(guid2) || Math.Abs(milliseconds - suivi2.Dernier) > 5000)
			{
				(list3 ?? (list3 = new List<Guid>())).Add(guid2);
			}
			else if (ADemander(suivi2, milliseconds) && (list?.Count ?? 0) < 16)
			{
				suivi2.Demande = milliseconds;
				suivi2.Demandes++;
				(list ?? (list = new List<Guid>())).Add(guid2);
				byte valueOrDefault = DernieresPertes.GetValueOrDefault(guid2);
				(list2 ?? (list2 = new List<byte>())).Add(suivi2.Immobile ? ((byte)(valueOrDefault | 0x80)) : valueOrDefault);
			}
		}
		if (list3 != null)
		{
			foreach (Guid item in list3)
			{
				Suivis.Remove(item);
			}
		}
		if (list != null)
		{
			Network.SendPacket(new DemandeEntitesPacket(list.ToArray(), list2.ToArray()));
		}
	}

	private static void DemanderLeRecensement(Player moi, long maintenant)
	{
		if (moi.MapId != _carteDuRecensement)
		{
			_carteDuRecensement = moi.MapId;
			_prochainRecensement = Math.Min(_prochainRecensement, maintenant + 800);
		}
		if (maintenant >= _prochainRecensement && moi.Dashing == null && !ChargementDesCartes.Bloque)
		{
			_prochainRecensement = maintenant + 5000;
			Network.SendPacket(new DemandeVoisinsPacket());
		}
	}

	private static bool ADemander(Suivi suivi, long maintenant)
	{
		return ADemander(suivi.Premier, suivi.Dernier, suivi.Demande, suivi.Demandes, maintenant);
	}

	internal static bool ADemander(long premier, long dernier, long demande, int demandes, long maintenant)
	{
		if (dernier - premier < 300)
		{
			return false;
		}
		if (demandes < 2)
		{
			if (demandes == 0)
			{
				return true;
			}
			return maintenant - demande >= 3000;
		}
		return maintenant - demande >= 60000;
	}
}
