using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Core.Controls;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Clash
{
	private enum Genre
	{
		Corps,
		Rayons
	}

	private sealed class Cercle
	{
		public int Numero;

		public long Apparition;

		public int Duree;

		public float Echelle = 1f;

		public long? Frappe;

		public byte Note;

		public long Fin;

		public long Cible => Apparition + Duree;
	}

	private sealed class Duel
	{
		public Genre Genre;

		public Guid A;

		public Guid B;

		public long Debut;

		public long OuvertureMs;

		public float X;

		public float Y;

		public float Sol;

		public Cercle? Cercle;

		public Cercle? Ancien;

		public bool PremierDit;

		public int ScoreA;

		public int ScoreB;

		public float K = 0.5f;

		public float KVu = 0.5f;

		public long Fin;

		public long ProchainImpact;

		public (float X, float Y)? DernierImpact;

		public (float X, float Cible)[] Ombres = new(float, float)[2]
		{
			(0f, 0f),
			(0f, 0f)
		};
	}

	private sealed class Eclat
	{
		public float X;

		public float Y;

		public long Debut;

		public float Force;

		public Color Teinte = Blanc;
	}

	private sealed class Vague
	{
		public Guid Duel;

		public bool DeA;

		public bool Forte;

		public long Debut;
	}

	private sealed class Deviation
	{
		public Guid Tireur;

		public Guid Garde;

		public float Biais;

		public long Debut;
	}

	private static readonly List<Duel> Duels = new List<Duel>();

	private static readonly List<Eclat> Eclats = new List<Eclat>();

	private static readonly List<(float X1, float Y1, float X2, float Y2, long Debut)> Traits = new List<(float, float, float, float, long)>();

	private static readonly List<(float X, float Y, float VitesseX, float VitesseY, long Debut, Color Couleur)> Etincelles = new List<(float, float, float, float, long, Color)>();

	private static readonly List<(float X, float Y, long Debut, long Duree, string Texte, Color Couleur, int Taille)> Mots = new List<(float, float, long, long, string, Color, int)>();

	private static readonly List<Vague> Vagues = new List<Vague>();

	private static readonly List<Deviation> Deviations = new List<Deviation>();

	private static readonly Color Blanc = new Color(255, 255, 255, 255);

	private static readonly Color Or = new Color(255, 255, 226, 150);

	private static readonly Color OrVif = new Color(255, 255, 211, 92);

	private static readonly Color Bleu = new Color(255, 108, 203, 255);

	private static readonly Color Pourpre = new Color(255, 211, 107, 255);

	private static readonly Color Rouge = new Color(255, 255, 123, 110);

	private static readonly Color Nuit = new Color(255, 5, 7, 13);

	private static readonly Color KiExterieur = new Color(255, 47, 123, 255);

	private static readonly Color KiMilieu = new Color(255, 108, 203, 255);

	private static readonly Color KiClair = new Color(255, 207, 244, 255);

	private static IFont? _police;

	private static bool Actif => (Options.Instance?.Combat?.Clash)?.Actif ?? false;

	public static bool OnEchange(EchangePacket packet)
	{
		switch (packet.Event)
		{
		case EchangeEvent.ClashDebut:
			OuvrirLeClash(packet);
			return true;
		case EchangeEvent.ClashCercle:
			NouveauCercle(packet);
			return true;
		case EchangeEvent.ClashCoup:
			CoupDecisif(packet);
			return true;
		case EchangeEvent.ClashFin:
			FinDuClash(packet);
			return true;
		case EchangeEvent.ContactDebut:
			OuvrirLeContact(packet);
			return true;
		case EchangeEvent.ContactPoussee:
			Poussee(packet);
			return true;
		case EchangeEvent.ContactFin:
			FinDuContact(packet);
			return true;
		case EchangeEvent.RayonDevie:
			Devier(packet);
			return true;
		default:
			return false;
		}
	}

	private static Duel? DuelEntre(Guid a, Guid b)
	{
		return Duels.Find((Duel duel) => duel.Fin == 0L && duel.A == a && duel.B == b);
	}

	private static Duel? DuelDe(Guid id)
	{
		return Duels.Find((Duel duel) => duel.Fin == 0L && (duel.A == id || duel.B == id));
	}

	private static bool EstMoi(Guid id)
	{
		Player? me = Globals.Me;
		if (me == null)
		{
			return false;
		}
		return me.Id == id;
	}

	private static (float X, float Y) Centre(Entity entite)
	{
		FloatRect worldPos = entite.WorldPos;
		return (X: worldPos.X + worldPos.Width / 2f, Y: worldPos.Y + worldPos.Height * 0.55f - Echanges.Hauteur(entite));
	}

	private static float Pieds(Entity entite)
	{
		return entite.WorldPos.Y + entite.WorldPos.Height - 2f;
	}

	private static void OuvrirLeClash(EchangePacket packet)
	{
		Globals.Entities.TryGetValue(packet.EntityId, out Entity value);
		Globals.Entities.TryGetValue(packet.AutreId, out Entity value2);
		if (value != null && value2 != null)
		{
			Duels.RemoveAll((Duel duel2) => duel2.A == packet.EntityId || duel2.B == packet.EntityId || duel2.A == packet.AutreId || duel2.B == packet.AutreId);
			long milliseconds = Timing.Global.Milliseconds;
			(float X, float Y) tuple = Centre(value);
			float item = tuple.X;
			float item2 = tuple.Y;
			(float X, float Y) tuple2 = Centre(value2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			Duel duel = new Duel
			{
				Genre = Genre.Corps,
				A = packet.EntityId,
				B = packet.AutreId,
				Debut = milliseconds,
				OuvertureMs = Math.Max(0, packet.DureeMs),
				X = (item + item3) / 2f,
				Y = (item2 + item4) / 2f - 34f,
				Sol = (Pieds(value) + Pieds(value2)) / 2f,
				ProchainImpact = milliseconds + 160
			};
			duel.Ombres = new(float, float)[2]
			{
				(item, item),
				(item3, item3)
			};
			Duels.Add(duel);
			float num = (item + item3) / 2f;
			float num2 = (item2 + item4) / 2f;
			float x = num;
			Eclats.Add(new Eclat
			{
				X = x,
				Y = num2,
				Debut = milliseconds,
				Force = 1.6f,
				Teinte = OrVif
			});
			Jaillir(x, num2, 30, milliseconds, OrVif, 0.16f);
			Jaillir(x, num2, 18, milliseconds, Blanc, 0.12f);
			Echanges.Poussiere(value, 4, 2.2f);
			Echanges.Poussiere(value2, 4, 2.2f);
			Mot(x, num2 - 26f, Strings.Clash.Debut, OrVif, 22, 650L);
			CombatAudio.Play("strongcoup.wav", value, 0.05f);
			if (EstMoi(duel.A) || EstMoi(duel.B))
			{
				CombatFeedback.HitStop(140L);
				CombatFeedback.StartShake(4f, 300L);
			}
		}
	}

	private static void NouveauCercle(EchangePacket packet)
	{
		Duel duel = DuelEntre(packet.EntityId, packet.AutreId);
		if (duel != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			if (duel.Cercle != null && duel.Cercle.Fin == 0L)
			{
				duel.Cercle.Fin = milliseconds;
			}
			duel.Ancien = duel.Cercle;
			duel.Cercle = new Cercle
			{
				Numero = packet.Index,
				Apparition = milliseconds + Math.Max(0, packet.Valeur),
				Duree = Math.Max(100, packet.DureeMs),
				Echelle = Math.Clamp((float)(EstMoi(duel.A) ? packet.Valeur2 : packet.Valeur3) / 1000f, 0.3f, 3f)
			};
		}
	}

	private static void CoupDecisif(EchangePacket packet)
	{
		Duel duel = DuelEntre(packet.EntityId, packet.AutreId);
		if (duel == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		duel.ScoreA = packet.DureeMs / 10;
		duel.ScoreB = packet.DureeMs % 10;
		Cercle cercle = duel.Cercle;
		if (cercle != null)
		{
			cercle.Fin = milliseconds;
			NoterLAbsence(duel, cercle, packet.Valeur2, packet.Valeur3);
		}
		int valeur = packet.Valeur;
		bool flag = EstMoi(duel.A);
		bool flag2 = EstMoi(duel.B);
		Color color = valeur switch
		{
			0 => flag2 ? Pourpre : Bleu, 
			1 => (flag || !flag2) ? Pourpre : Bleu, 
			2 => Blanc, 
			_ => KiClair, 
		};
		Eclats.Add(new Eclat
		{
			X = duel.X,
			Y = duel.Y,
			Debut = milliseconds,
			Force = ((valeur == 3) ? 0.7f : 1.3f),
			Teinte = color
		});
		Jaillir(duel.X, duel.Y, (valeur == 2) ? 30 : 22, milliseconds, color, 0.15f);
		Jaillir(duel.X, duel.Y, 10, milliseconds, Or, 0.12f);
		NouvelImpact(duel, duel.X, duel.Y, milliseconds);
		bool flag3 = flag || flag2;
		string text;
		switch (valeur)
		{
		case 0:
			if (flag3)
			{
				text = (flag ? Strings.Clash.Touche.ToString() : Strings.Clash.Encaisse.ToString());
				break;
			}
			goto default;
		case 1:
			if (flag3)
			{
				text = (flag2 ? Strings.Clash.Touche.ToString() : Strings.Clash.Encaisse.ToString());
				break;
			}
			goto default;
		case 2:
			text = Strings.Clash.Parade.ToString();
			break;
		case 3:
			text = Strings.Clash.DansLeVide.ToString();
			break;
		default:
			text = null;
			break;
		}
		string text2 = text;
		if (text2 != null)
		{
			bool flag4 = (valeur == 0 && flag) || (valeur == 1 && flag2);
			Mot(duel.X, duel.Y + 30f, text2, valeur switch
			{
				3 => KiClair, 
				2 => OrVif, 
				_ => flag4 ? Bleu : Rouge, 
			}, (valeur == 3) ? 11 : 14, 650L);
		}
		if (Globals.Entities.TryGetValue(duel.A, out Entity value) && value != null)
		{
			CombatAudio.Play((valeur == 3) ? "coup.wav" : "strongcoup.wav", value, 0.06f);
		}
		if (flag3)
		{
			CombatFeedback.HitStop((valeur == 3) ? 30 : 80);
			CombatFeedback.StartShake((valeur == 3) ? 1.5f : 3f, 200L);
		}
	}

	private static void NoterLAbsence(Duel duel, Cercle cercle, int noteA, int noteB)
	{
		Player me = Globals.Me;
		if (me != null && !cercle.Frappe.HasValue && (!(me.Id != duel.A) || !(me.Id != duel.B)))
		{
			cercle.Frappe = cercle.Fin;
			cercle.Note = 0;
			var (x, num) = PointDuCercle(duel);
			Mot(x, num - RayonDe(duel) - 12f, Strings.Clash.Rate, Rouge, 11, 650L);
		}
	}

	private static void FinDuClash(EchangePacket packet)
	{
		Duel duel = DuelEntre(packet.EntityId, packet.AutreId) ?? DuelEntre(packet.AutreId, packet.EntityId);
		if (duel == null)
		{
			return;
		}
		long num = (duel.Fin = Timing.Global.Milliseconds);
		if (duel.Cercle != null && duel.Cercle.Fin == 0L)
		{
			duel.Cercle.Fin = num;
		}
		bool flag = EstMoi(duel.A) || EstMoi(duel.B);
		switch (packet.Index)
		{
		case 0:
		{
			if (Globals.Entities.TryGetValue(packet.AutreId, out Entity value) && value != null)
			{
				float x = duel.X;
				float y = duel.Y + 20f;
				float x2 = x;
				Eclats.Add(new Eclat
				{
					X = x2,
					Y = y,
					Debut = num,
					Force = 2f,
					Teinte = OrVif
				});
				Jaillir(x2, y, 36, num, OrVif, 0.2f);
				Jaillir(x2, y, 20, num, Blanc, 0.16f);
				CombatAudio.Play("crithit.wav", value, 0.04f);
				if (flag)
				{
					CombatFeedback.HitStop(150L);
					CombatFeedback.StartShake(5f, 380L);
				}
			}
			break;
		}
		case 1:
			Eclats.Add(new Eclat
			{
				X = duel.X,
				Y = duel.Y + 20f,
				Debut = num,
				Force = 1.8f,
				Teinte = Blanc
			});
			Jaillir(duel.X, duel.Y + 20f, 40, num, Blanc, 0.18f);
			Mot(duel.X, duel.Y - 10f, Strings.Clash.Egalite, OrVif, 18, 650L);
			if (flag)
			{
				CombatFeedback.HitStop(120L);
				CombatFeedback.StartShake(4f, 300L);
			}
			break;
		}
	}

	private static void OuvrirLeContact(EchangePacket packet)
	{
		Duels.RemoveAll((Duel duel2) => duel2.A == packet.EntityId || duel2.B == packet.EntityId || duel2.A == packet.AutreId || duel2.B == packet.AutreId);
		long milliseconds = Timing.Global.Milliseconds;
		float num = Math.Clamp((float)packet.Valeur / 1000f, 0f, 1f);
		Duel duel = new Duel
		{
			Genre = Genre.Rayons,
			A = packet.EntityId,
			B = packet.AutreId,
			Debut = milliseconds,
			K = num,
			KVu = num
		};
		Duels.Add(duel);
		(float, float)? tuple = PointDeContact(duel);
		if (tuple.HasValue)
		{
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			Eclats.Add(new Eclat
			{
				X = valueOrDefault.Item1,
				Y = valueOrDefault.Item2,
				Debut = milliseconds,
				Force = 1.8f,
				Teinte = Blanc
			});
			Jaillir(valueOrDefault.Item1, valueOrDefault.Item2, 34, milliseconds, KiClair, 0.18f);
			Mot(valueOrDefault.Item1, valueOrDefault.Item2 - 34f, Strings.Clash.Contact, OrVif, 16, 650L);
		}
		if (EstMoi(duel.A) || EstMoi(duel.B))
		{
			CombatFeedback.HitStop(170L);
			CombatFeedback.StartShake(3f, 420L);
		}
	}

	private static void Poussee(EchangePacket packet)
	{
		Duel duel = DuelEntre(packet.EntityId, packet.AutreId);
		if (duel != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			duel.K = Math.Clamp((float)packet.Valeur / 1000f, 0f, 1f);
			if (duel.Cercle != null)
			{
				duel.Cercle.Fin = milliseconds;
				NoterLAbsence(duel, duel.Cercle, packet.Valeur2, packet.Valeur3);
			}
			if (packet.Valeur2 > 0)
			{
				Vagues.Add(new Vague
				{
					Duel = duel.A,
					DeA = true,
					Forte = (packet.Valeur2 >= 2),
					Debut = milliseconds
				});
			}
			if (packet.Valeur3 > 0)
			{
				Vagues.Add(new Vague
				{
					Duel = duel.A,
					DeA = false,
					Forte = (packet.Valeur3 >= 2),
					Debut = milliseconds
				});
			}
		}
	}

	private static void FinDuContact(EchangePacket packet)
	{
		Duel duel = DuelEntre(packet.EntityId, packet.AutreId) ?? DuelEntre(packet.AutreId, packet.EntityId);
		if (duel != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			(float, float)? tuple = PointDeContact(duel);
			duel.Fin = milliseconds;
			if (duel.Cercle != null && duel.Cercle.Fin == 0L)
			{
				duel.Cercle.Fin = milliseconds;
			}
			bool flag = EstMoi(duel.A) || EstMoi(duel.B);
			Entity value;
			if (packet.Index == 1 && tuple.HasValue)
			{
				(float, float) valueOrDefault = tuple.GetValueOrDefault();
				Eclats.Add(new Eclat
				{
					X = valueOrDefault.Item1,
					Y = valueOrDefault.Item2,
					Debut = milliseconds,
					Force = 2.4f,
					Teinte = Blanc
				});
				Jaillir(valueOrDefault.Item1, valueOrDefault.Item2, 60, milliseconds, KiClair, 0.22f);
				Jaillir(valueOrDefault.Item1, valueOrDefault.Item2, 30, milliseconds, OrVif, 0.18f);
				Mot(valueOrDefault.Item1, valueOrDefault.Item2 - 30f, Strings.Clash.Egalite, OrVif, 18, 650L);
			}
			else if (packet.Index == 0 && Globals.Entities.TryGetValue(packet.AutreId, out value) && value != null)
			{
				var (x, y) = Centre(value);
				Eclats.Add(new Eclat
				{
					X = x,
					Y = y,
					Debut = milliseconds,
					Force = 2.4f,
					Teinte = KiClair
				});
				Jaillir(x, y, 50, milliseconds, KiClair, 0.22f);
				Jaillir(x, y, 24, milliseconds, Blanc, 0.18f);
				Echanges.Poussiere(value, 6, 2.6f);
			}
			if (flag && packet.Index != 2)
			{
				CombatFeedback.HitStop(160L);
				CombatFeedback.StartShake(5f, 500L);
			}
		}
	}

	private static void Devier(EchangePacket packet)
	{
		Deviations.RemoveAll((Deviation deviation) => deviation.Tireur == packet.EntityId && deviation.Garde == packet.AutreId);
		Deviations.Add(new Deviation
		{
			Tireur = packet.EntityId,
			Garde = packet.AutreId,
			Biais = packet.Valeur,
			Debut = Timing.Global.Milliseconds
		});
		if (Globals.Entities.TryGetValue(packet.AutreId, out Entity value) && value != null)
		{
			CombatFeedback.Callout(value.Id, Strings.Clash.Devie, KiClair, 14);
			CombatAudio.Play("ki_impact.wav", value, 0.05f);
		}
	}

	public static bool LireLaTouche(Player moi, bool appuiNeuf)
	{
		if (Duels.Count != 0)
		{
			Duel duel = DuelDe(moi.Id);
			if (duel != null)
			{
				if (appuiNeuf)
				{
					Frapper(duel, moi);
				}
				return true;
			}
		}
		return false;
	}

	private static void Frapper(Duel duel, Player moi)
	{
		Cercle cercle = duel.Cercle;
		long milliseconds = Timing.Global.Milliseconds;
		if (cercle != null && !cercle.Frappe.HasValue && cercle.Fin == 0L && milliseconds >= cercle.Apparition)
		{
			ClashOptions clash = Options.Instance.Combat.Clash;
			int num = (int)(milliseconds - cercle.Cible);
			int num2 = Math.Abs(num);
			cercle.Frappe = milliseconds;
			cercle.Note = (byte)((!((float)num < (float)(-clash.BienMs) * cercle.Echelle)) ? (((float)num2 <= (float)clash.ParfaitMs * cercle.Echelle) ? ((byte)2) : (((float)num2 <= (float)clash.BienMs * cercle.Echelle) ? ((byte)1) : ((byte)0))) : 0);
			PacketSender.SendFrappeDeClash((duel.A == moi.Id) ? duel.B : duel.A, cercle.Numero, num);
			var (x, num3) = PointDuCercle(duel);
			var (texte, couleur, taille) = cercle.Note switch
			{
				2 => (Strings.Clash.Parfait.ToString(), OrVif, 13), 
				1 => (Strings.Clash.Bien.ToString(), Blanc, 11), 
				_ => (Strings.Clash.Rate.ToString(), Rouge, 11), 
			};
			Mot(x, num3 - RayonDe(duel) - 12f, texte, couleur, taille, 650L);
			if (cercle.Note == 2)
			{
				Jaillir(x, num3, 10, milliseconds, OrVif, 0.12f);
			}
		}
	}

	public static bool Cache(Entity entite)
	{
		if (Duels.Count == 0)
		{
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Duel duel in Duels)
		{
			if (duel.Genre == Genre.Corps && duel.Fin == 0L && (duel.A == entite.Id || duel.B == entite.Id) && milliseconds - duel.Debut >= 160)
			{
				return true;
			}
		}
		return false;
	}

	public static bool Retient(Entity entite)
	{
		if (Duels.Count > 0 && entite == Globals.Me)
		{
			return DuelDe(entite.Id) != null;
		}
		return false;
	}

	public static (float X, float Y)? BouleDeChoc(Guid tireur)
	{
		if (Duels.Count == 0)
		{
			return null;
		}
		foreach (Duel duel in Duels)
		{
			if (duel.Genre == Genre.Rayons && duel.Fin == 0L && (duel.A == tireur || duel.B == tireur))
			{
				return PointDeContact(duel);
			}
		}
		return null;
	}

	private static float RayonDe(Duel duel)
	{
		if (duel.Genre != Genre.Corps)
		{
			return 17f;
		}
		return 10f;
	}

	private static float RayonDeDepart(Duel duel)
	{
		if (duel.Genre != Genre.Corps)
		{
			return 46f;
		}
		return 36f;
	}

	private static (float X, float Y) PointDuCercle(Duel duel)
	{
		if (duel.Genre != Genre.Corps)
		{
			return PointDeContact(duel) ?? (duel.X, duel.Y);
		}
		return (X: duel.X, Y: duel.Y);
	}

	private static (float X, float Y)? PointDeContact(Duel duel)
	{
		if (!Globals.Entities.TryGetValue(duel.A, out Entity value) || value == null || !Globals.Entities.TryGetValue(duel.B, out Entity value2) || value2 == null)
		{
			return null;
		}
		(float, float, float)? tuple = KiBeam.OrigineDuTir(value);
		float num;
		float num2;
		if (!tuple.HasValue)
		{
			(num, num2) = Centre(value);
		}
		else
		{
			(float, float, float) valueOrDefault = tuple.GetValueOrDefault();
			float item = valueOrDefault.Item1;
			float item2 = valueOrDefault.Item2;
			num2 = item2;
			num = item;
		}
		tuple = KiBeam.OrigineDuTir(value2);
		float num3;
		float num4;
		if (!tuple.HasValue)
		{
			(num3, num4) = Centre(value2);
		}
		else
		{
			(float, float, float) valueOrDefault2 = tuple.GetValueOrDefault();
			float item3 = valueOrDefault2.Item1;
			float item2 = valueOrDefault2.Item2;
			num4 = item2;
			num3 = item3;
		}
		(float, float) tuple4 = (num + (num3 - num) * duel.KVu, num2 + (num4 - num2) * duel.KVu);
		(duel.X, duel.Y) = tuple4;
		return tuple4;
	}

	public static void Draw(IGameTexture pixel)
	{
		if (Duels.Count == 0 && Eclats.Count == 0 && Etincelles.Count == 0 && Mots.Count == 0 && Deviations.Count == 0 && Traits.Count == 0)
		{
			return;
		}
		long maintenant = Timing.Global.Milliseconds;
		Duels.RemoveAll((Duel duel) => (duel.Fin != 0L && maintenant - duel.Fin > 400) || maintenant - duel.Debut > 30000);
		foreach (Duel duel in Duels)
		{
			if (duel.Fin == 0L)
			{
				if (duel.Genre == Genre.Corps)
				{
					DessinerLEchange(pixel, duel, maintenant);
				}
				else
				{
					DessinerLaLutte(pixel, duel, maintenant);
				}
			}
		}
		DessinerLesDeviations(pixel, maintenant);
		DessinerLesTraits(pixel, maintenant);
		DessinerLesEclats(pixel, maintenant);
		DessinerLesEtincelles(pixel, maintenant);
		Player me = Globals.Me;
		foreach (Duel duel2 in Duels)
		{
			if (duel2.Fin == 0L && me != null && (duel2.A == me.Id || duel2.B == me.Id))
			{
				var (x, num) = PointDuCercle(duel2);
				DessinerLeCercle(pixel, duel2, duel2.Ancien, x, num, maintenant);
				DessinerLeCercle(pixel, duel2, duel2.Cercle, x, num, maintenant);
				if (duel2.Genre == Genre.Corps)
				{
					DessinerLeScore(pixel, duel2, x, num - RayonDeDepart(duel2) - 8f, me.Id == duel2.A);
				}
				if (!duel2.PremierDit && duel2.Cercle != null && maintenant >= duel2.Cercle.Apparition)
				{
					duel2.PremierDit = true;
					Mot(x, num - RayonDeDepart(duel2) - 22f, Strings.Clash.AuBonMoment, OrVif, 12, 1300L);
				}
			}
		}
		DessinerLesMots(maintenant);
	}

	private static void DessinerLEchange(IGameTexture pixel, Duel duel, long maintenant)
	{
		if (maintenant - duel.Debut < 160)
		{
			return;
		}
		if (maintenant >= duel.ProchainImpact)
		{
			duel.ProchainImpact = maintenant + 120 + Random.Shared.Next(110);
			Cercle cercle = duel.Cercle;
			bool flag = cercle != null && cercle.Fin == 0L && maintenant >= cercle.Apparition;
			float x = (flag ? (duel.X + ((Random.Shared.Next(2) == 0) ? (-1f) : 1f) * (38f + (float)Random.Shared.NextDouble() * 50f)) : (duel.X + ((float)Random.Shared.NextDouble() - 0.5f) * 150f));
			float y = duel.Y + ((float)Random.Shared.NextDouble() - 0.6f) * 60f;
			Eclats.Add(new Eclat
			{
				X = x,
				Y = y,
				Debut = maintenant,
				Force = 0.55f,
				Teinte = KiClair
			});
			Jaillir(x, y, 7, maintenant, (Random.Shared.Next(2) == 0) ? Or : Blanc, 0.1f);
			NouvelImpact(duel, x, y, maintenant);
			if (!flag && (EstMoi(duel.A) || EstMoi(duel.B)))
			{
				CombatFeedback.StartShake(1.5f, 90L);
			}
			if (Globals.Entities.TryGetValue(duel.A, out Entity value) && value != null && Random.Shared.Next(3) == 0)
			{
				CombatAudio.Play("coup.wav", value, 0.12f);
			}
		}
		for (int i = 0; i < 2; i++)
		{
			(float X, float Cible) tuple = duel.Ombres[i];
			float item = tuple.X;
			float item2 = tuple.Cible;
			item += (item2 - item) * 0.35f;
			duel.Ombres[i] = (X: item, Cible: item2);
			Ellipse(pixel, item, duel.Sol, 7f, 2.5f, new Color(80, 0, 0, 0));
		}
	}

	private static void NouvelImpact(Duel duel, float x, float y, long maintenant)
	{
		(float, float)? dernierImpact = duel.DernierImpact;
		if (dernierImpact.HasValue)
		{
			(float, float) valueOrDefault = dernierImpact.GetValueOrDefault();
			Traits.Add((valueOrDefault.Item1, valueOrDefault.Item2, x, y, maintenant));
		}
		duel.DernierImpact = (x, y);
		float num = ((Random.Shared.Next(2) == 0) ? 1f : (-1f));
		duel.Ombres[0] = (X: duel.Ombres[0].X, Cible: x - 12f * num);
		duel.Ombres[1] = (X: duel.Ombres[1].X, Cible: x + 12f * num);
	}

	private static void DessinerLaLutte(IGameTexture pixel, Duel duel, long maintenant)
	{
		duel.KVu += (duel.K - duel.KVu) * 0.18f;
		(float, float)? tuple = PointDeContact(duel);
		if (!tuple.HasValue)
		{
			return;
		}
		(float, float) valueOrDefault = tuple.GetValueOrDefault();
		Globals.Entities.TryGetValue(duel.A, out Entity value);
		Globals.Entities.TryGetValue(duel.B, out Entity value2);
		Vagues.RemoveAll((Vague vague) => maintenant - vague.Debut > 160);
		foreach (Vague vague in Vagues)
		{
			if (vague.Duel != duel.A)
			{
				continue;
			}
			Entity entity = (vague.DeA ? value : value2);
			if (entity != null)
			{
				(float X, float Y) tuple2 = Centre(entity);
				float item = tuple2.X;
				float item2 = tuple2.Y;
				float num = Math.Clamp((float)(maintenant - vague.Debut) / 140f, 0f, 1f);
				float cx = item + (valueOrDefault.Item1 - item) * num;
				float cy = item2 + (valueOrDefault.Item2 - item2) * num;
				float num2 = (vague.Forte ? 8f : 6f);
				Disque(pixel, cx, cy, num2 + 1f, KiExterieur);
				Disque(pixel, cx, cy, num2, KiMilieu);
				Disque(pixel, cx, cy, num2 * 0.6f, KiClair);
				Disque(pixel, cx, cy, num2 * 0.3f, Blanc);
				if (num >= 1f)
				{
					Jaillir(valueOrDefault.Item1, valueOrDefault.Item2, 6, maintenant, KiClair, 0.1f);
				}
			}
		}
		int num3 = (int)(maintenant / 45);
		float num4 = 10f + MathF.Sin((float)maintenant / 45f) * 1.5f + (float)(num3 % 2);
		for (int num5 = 0; num5 < 12; num5++)
		{
			float x = Hasard(num5 * 11 + num3) * (float)Math.PI * 2f;
			float num6 = num4 + 4f + Hasard(num3 * 5 + num5) * 9f;
			Ligne(pixel, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item1 + MathF.Cos(x) * num6, valueOrDefault.Item2 + MathF.Sin(x) * num6, 2f, (num5 % 2 == 0) ? KiExterieur : new Color(255, 138, 43, 226));
		}
		Disque(pixel, valueOrDefault.Item1, valueOrDefault.Item2, num4 + 1f, new Color(255, 90, 75, 214));
		Disque(pixel, valueOrDefault.Item1, valueOrDefault.Item2, num4 * 0.8f, new Color(255, 159, 184, 255));
		Disque(pixel, valueOrDefault.Item1, valueOrDefault.Item2, num4 * 0.55f, new Color(255, 233, 230, 255));
		Disque(pixel, valueOrDefault.Item1, valueOrDefault.Item2, num4 * 0.3f, Blanc);
		for (int num7 = 0; num7 < 3; num7++)
		{
			float x2 = Hasard(num3 * 3 + num7) * (float)Math.PI * 2f;
			float num8 = num4 + 6f + Hasard(num7 + num3) * 10f;
			Eclair(pixel, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item1 + MathF.Cos(x2) * num8, valueOrDefault.Item2 + MathF.Sin(x2) * num8 * 0.8f, (num7 % 2 == 0) ? Blanc : KiClair, num3 * 13 + num7);
		}
		if (Random.Shared.Next(3) == 0)
		{
			Jaillir(valueOrDefault.Item1, valueOrDefault.Item2, 2, maintenant, (Random.Shared.Next(2) == 0) ? KiClair : Blanc, 0.12f);
		}
	}

	private static void DessinerLesDeviations(IGameTexture pixel, long maintenant)
	{
		Deviations.RemoveAll((Deviation deviation) => maintenant - deviation.Debut > 3500 || !Globals.Entities.TryGetValue(deviation.Tireur, out Entity value) || !(value is Player { IsBeaming: not false }) || !Globals.Entities.TryGetValue(deviation.Garde, out Entity value2) || value2 == null);
		foreach (Deviation deviation in Deviations)
		{
			Entity entite = Globals.Entities[deviation.Tireur];
			Entity entite2 = Globals.Entities[deviation.Garde];
			(float X, float Y) tuple = Centre(entite);
			float item = tuple.X;
			float item2 = tuple.Y;
			(float X, float Y) tuple2 = Centre(entite2);
			float item3 = tuple2.X;
			float item4 = tuple2.Y;
			float x = MathF.Atan2(item4 - item2, item3 - item) + deviation.Biais * (float)Math.PI / 180f;
			float num = Math.Clamp((float)(maintenant - deviation.Debut) / 180f, 0f, 1f);
			float num2 = 130f * num;
			float num3 = item3 + MathF.Cos(x) * num2;
			float num4 = item4 + MathF.Sin(x) * num2;
			float num5 = MathF.Sin((float)maintenant / 40f) * 0.8f;
			Ligne(pixel, item3, item4, num3, num4, 9f + num5, KiExterieur);
			Ligne(pixel, item3, item4, num3, num4, 6f + num5, KiMilieu);
			Ligne(pixel, item3, item4, num3, num4, 3f, KiClair);
			Ligne(pixel, item3, item4, num3, num4, 1.5f, Blanc);
			if (num >= 1f)
			{
				float num6 = 7f + MathF.Sin((float)maintenant / 50f) * 2f;
				Disque(pixel, num3, num4, num6 + 2f, KiExterieur);
				Disque(pixel, num3, num4, num6, KiMilieu);
				Disque(pixel, num3, num4, num6 * 0.55f, Blanc);
				if (Random.Shared.Next(2) == 0)
				{
					Jaillir(num3, num4, 3, maintenant, KiClair, 0.14f);
				}
			}
			if (Random.Shared.Next(2) == 0)
			{
				Jaillir(item3, item4, 2, maintenant, (Random.Shared.Next(2) == 0) ? Or : KiClair, 0.12f);
			}
		}
	}

	private static void DessinerLeCercle(IGameTexture pixel, Duel duel, Cercle? cercle, float x, float y, long maintenant)
	{
		if (cercle == null || maintenant < cercle.Apparition)
		{
			return;
		}
		float num = Math.Min(1f, (float)(maintenant - cercle.Apparition) / 120f);
		if (cercle.Fin != 0L)
		{
			num *= Math.Max(0f, 1f - (float)(maintenant - cercle.Fin) / 220f);
		}
		if (!(num <= 0.01f))
		{
			ClashOptions clash = Options.Instance.Combat.Clash;
			float num2 = RayonDe(duel);
			float num3 = (RayonDeDepart(duel) - num2) / (float)Math.Max(1, cercle.Duree);
			long num4 = (cercle.Frappe ?? maintenant) - cercle.Cible;
			float num5 = (float)clash.BienMs * cercle.Echelle;
			float num6 = (float)clash.ParfaitMs * cercle.Echelle;
			bool flag = !cercle.Frappe.HasValue && cercle.Fin == 0L && (float)Math.Abs(num4) <= num5;
			Anneau(pixel, x, y, num2, Math.Max(1f, 2f * num5 * num3), Fondu(Blanc, num * (flag ? 0.3f : 0.16f)));
			Anneau(pixel, x, y, num2, Math.Max(1f, 2f * num6 * num3), Fondu(OrVif, num * (flag ? 0.95f : 0.5f)));
			float rayon = Math.Max(1f, num2 - (float)Math.Min(num4, (long)num5 + 40) * num3);
			Color color = ((!cercle.Frappe.HasValue) ? (((float)Math.Abs(num4) <= num6) ? OrVif : Blanc) : (cercle.Note switch
			{
				2 => OrVif, 
				1 => Blanc, 
				_ => Rouge, 
			}));
			Color couleur = color;
			Anneau(pixel, x, y, rayon, 2.4f, Fondu(Nuit, num * 0.8f));
			Anneau(pixel, x, y, rayon, 1.3f, Fondu(couleur, num));
			if (!cercle.Frappe.HasValue && cercle.Fin == 0L)
			{
				DessinerLaTouche(pixel, x, y + num2 + 9f, num);
			}
		}
	}

	private static void DessinerLaTouche(IGameTexture pixel, float x, float y, float alpha)
	{
		string text = (Manette.EnUsage ? "B" : (Touches.NomDe(Control.AttackInteract) ?? "E"));
		float num = Math.Max(9f, 5f + (float)text.Length * 4f);
		Rect(pixel, x - num / 2f - 1f, y - 5.5f, num + 2f, 11f, Fondu(Nuit, alpha));
		Rect(pixel, x - num / 2f, y - 4.5f, num, 9f, Fondu(new Color(255, 242, 242, 242), alpha));
		Ecrire(text, x, y, 8, Fondu(Nuit, alpha), null, 1f);
	}

	private static void DessinerLeScore(IGameTexture pixel, Duel duel, float x, float y, bool moiA)
	{
		int num = Math.Max(1, Options.Instance.Combat.Clash.CoupsGagnants);
		int num2 = (moiA ? duel.ScoreA : duel.ScoreB);
		int num3 = (moiA ? duel.ScoreB : duel.ScoreA);
		for (int i = 0; i < num; i++)
		{
			Rect(pixel, x - 8f - (float)(i + 1) * 7f, y - 2f, 5f, 4f, (i < num2) ? Bleu : new Color(70, 108, 203, 255));
			Rect(pixel, x + 8f + (float)i * 7f, y - 2f, 5f, 4f, (i < num3) ? Pourpre : new Color(70, 211, 107, 255));
		}
	}

	private static void DessinerLesTraits(IGameTexture pixel, long maintenant)
	{
		Traits.RemoveAll(((float X1, float Y1, float X2, float Y2, long Debut) trait) => maintenant - trait.Debut > 130);
		foreach (var trait in Traits)
		{
			float item = trait.X1;
			float item2 = trait.Y1;
			float item3 = trait.X2;
			float item4 = trait.Y2;
			long item5 = trait.Debut;
			float num = (float)(maintenant - item5) / 130f;
			float num2 = Math.Max(1f, MathF.Max(MathF.Abs(item3 - item), MathF.Abs(item4 - item2)));
			for (float num3 = 0f; num3 <= num2; num3 += 1f)
			{
				float num4 = num3 / num2;
				if (!(num4 < num))
				{
					float x = item + (item3 - item) * num4;
					float num5 = item2 + (item4 - item2) * num4;
					Rect(pixel, x, num5, 1f, 1f, Fondu(new Color(255, 234, 246, 255), 0.6f * (1f - num)));
					Rect(pixel, x, num5 + 3f, 1f, 1f, Fondu(new Color(255, 159, 200, 255), 0.6f * (1f - num)));
				}
			}
		}
	}

	private static void DessinerLesEclats(IGameTexture pixel, long maintenant)
	{
		Eclats.RemoveAll((Eclat eclat) => maintenant - eclat.Debut > 260);
		foreach (Eclat eclat in Eclats)
		{
			float num = (float)(maintenant - eclat.Debut) / 260f;
			float force = eclat.Force;
			Anneau(pixel, eclat.X, eclat.Y, 3f + (16f + 14f * force) * (1f - (1f - num) * (1f - num)), 1.5f, Fondu(Blanc, 1f - num));
			if (num < 0.45f)
			{
				float num2 = 1f - num / 0.45f;
				for (int num3 = 0; num3 < 4; num3++)
				{
					float x = (float)num3 * (float)Math.PI / 2f + 0.4f;
					float num4 = (10f + 6f * force) * num2 + 3f;
					Ligne(pixel, eclat.X, eclat.Y, eclat.X + MathF.Cos(x) * num4, eclat.Y + MathF.Sin(x) * num4, 1.6f, Fondu(Or, num2));
				}
				Disque(pixel, eclat.X, eclat.Y, (5f + 3f * force) * num2 + 1f, Fondu(Blanc, num2));
				Disque(pixel, eclat.X, eclat.Y, (2f + 1.5f * force) * num2 + 0.6f, Fondu(eclat.Teinte, num2));
			}
		}
	}

	private static void DessinerLesEtincelles(IGameTexture pixel, long maintenant)
	{
		Etincelles.RemoveAll(((float X, float Y, float VitesseX, float VitesseY, long Debut, Color Couleur) etincelle) => maintenant - etincelle.Debut > 380);
		foreach (var etincelle in Etincelles)
		{
			float item = etincelle.X;
			float item2 = etincelle.Y;
			float item3 = etincelle.VitesseX;
			float item4 = etincelle.VitesseY;
			long item5 = etincelle.Debut;
			Color item6 = etincelle.Couleur;
			long num = maintenant - item5;
			Rect(pixel, MathF.Round(item + item3 * (float)num), MathF.Round(item2 + item4 * (float)num + 0.00012f * (float)num * (float)num), 1f, 1f, Fondu(item6, 1f - (float)num / 380f));
		}
	}

	private static void Mot(float x, float y, string texte, Color couleur, int taille, long dureeMs = 650L)
	{
		Mots.Add((x, y, Timing.Global.Milliseconds, Math.Max(200L, dureeMs), texte, couleur, taille));
		if (Mots.Count > 12)
		{
			Mots.RemoveAt(0);
		}
	}

	private static void DessinerLesMots(long maintenant)
	{
		Mots.RemoveAll(((float X, float Y, long Debut, long Duree, string Texte, Color Couleur, int Taille) mot) => maintenant - mot.Debut > mot.Duree);
		foreach (var mot in Mots)
		{
			float item = mot.X;
			float item2 = mot.Y;
			long item3 = mot.Debut;
			long item4 = mot.Duree;
			string item5 = mot.Texte;
			Color item6 = mot.Couleur;
			int item7 = mot.Taille;
			long num = Math.Max(0L, maintenant - item3);
			float num2 = (float)num / (float)item4;
			float echelle = ((num < 110) ? (1.35f - 0.35f * ((float)num / 110f)) : 1f);
			float alpha = ((num2 < 0.65f) ? 1f : (1f - (num2 - 0.65f) / 0.35f));
			Ecrire(item5, item, item2 - 8f * MathF.Sqrt(num2), item7, Fondu(item6, alpha), Fondu(new Color(255, 20, 10, 0), alpha), echelle);
		}
	}

	private static void Ecrire(string texte, float x, float y, int taille, Color couleur, Color? contour, float echelle)
	{
		GameRenderer rendu = Graphics.Renderer;
		if (rendu == null)
		{
			return;
		}
		if (_police == null)
		{
			_police = Globals.ContentManager.GetFont("rajdhanibold");
		}
		if (_police != null)
		{
			Vector2 vector = rendu.MeasureText(texte, _police, taille, echelle);
			float gauche = x - vector.X / 2f;
			float haut = y - vector.Y / 2f;
			TexteMonde.Lisse(delegate
			{
				rendu.DrawString(texte, _police, taille, gauche, haut, echelle, couleur, worldPos: true, null, contour ?? Color.Transparent);
			});
		}
	}

	private static void Jaillir(float x, float y, int nombre, long maintenant, Color couleur, float vitesse)
	{
		for (int i = 0; i < nombre; i++)
		{
			float x2 = (float)(Random.Shared.NextDouble() * Math.PI * 2.0);
			float num = vitesse * (0.4f + (float)Random.Shared.NextDouble());
			Etincelles.Add((x, y, MathF.Cos(x2) * num, MathF.Sin(x2) * num, maintenant, couleur));
		}
		if (Etincelles.Count > 600)
		{
			Etincelles.RemoveRange(0, Etincelles.Count - 600);
		}
	}

	private static Color Fondu(Color couleur, float alpha)
	{
		return new Color((int)Math.Clamp((float)(int)couleur.A * alpha, 0f, 255f), couleur.R, couleur.G, couleur.B);
	}

	private static void Rect(IGameTexture pixel, float x, float y, float largeur, float hauteur, Color couleur)
	{
		if (couleur.A > 0)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, largeur, hauteur), couleur);
		}
	}

	private static void Disque(IGameTexture pixel, float cx, float cy, float rayon, Color couleur)
	{
		if (rayon <= 0.5f)
		{
			Rect(pixel, cx, cy, 1f, 1f, couleur);
			return;
		}
		int num = (int)MathF.Ceiling(rayon);
		for (int i = -num; i <= num; i++)
		{
			float num2 = MathF.Sqrt(MathF.Max(0f, rayon * rayon - (float)(i * i)));
			if (!(num2 < 0.5f))
			{
				Rect(pixel, MathF.Round(cx - num2), MathF.Round(cy + (float)i), MathF.Round(num2 * 2f), 1f, couleur);
			}
		}
	}

	private static void Ellipse(IGameTexture pixel, float cx, float cy, float rx, float ry, Color couleur)
	{
		int num = (int)MathF.Ceiling(ry);
		for (int i = -num; i <= num; i++)
		{
			float num2 = rx * MathF.Sqrt(MathF.Max(0f, 1f - (float)(i * i) / (ry * ry)));
			if (num2 >= 0.5f)
			{
				Rect(pixel, MathF.Round(cx - num2), MathF.Round(cy + (float)i), MathF.Round(num2 * 2f), 1f, couleur);
			}
		}
	}

	private static void Anneau(IGameTexture pixel, float cx, float cy, float rayon, float epaisseur, Color couleur)
	{
		if (couleur.A > 0 && !(rayon <= 0f))
		{
			float num = MathF.Max(1f, epaisseur);
			int num2 = Math.Max(16, (int)(rayon * 2.4f));
			for (int i = 0; i < num2; i++)
			{
				float x = (float)i / (float)num2 * (float)Math.PI * 2f;
				Rect(pixel, cx + MathF.Cos(x) * rayon - num / 2f, cy + MathF.Sin(x) * rayon - num / 2f, num, num, couleur);
			}
		}
	}

	private static void Ligne(IGameTexture pixel, float x1, float y1, float x2, float y2, float epaisseur, Color couleur)
	{
		float num = MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
		int num2 = Math.Max(1, (int)num);
		float num3 = MathF.Max(1f, epaisseur);
		for (int i = 0; i <= num2; i++)
		{
			float num4 = (float)i / (float)num2;
			Rect(pixel, x1 + (x2 - x1) * num4 - num3 / 2f, y1 + (y2 - y1) * num4 - num3 / 2f, num3, num3, couleur);
		}
	}

	private static void Eclair(IGameTexture pixel, float x1, float y1, float x2, float y2, Color couleur, int graine)
	{
		float x3 = x1;
		float y3 = y1;
		for (int i = 1; i <= 4; i++)
		{
			float num = (float)i / 4f;
			float num2 = x1 + (x2 - x1) * num + ((i < 4) ? ((Hasard(graine * 7 + i) - 0.5f) * 6f) : 0f);
			float num3 = y1 + (y2 - y1) * num + ((i < 4) ? ((Hasard(graine * 13 + i) - 0.5f) * 6f) : 0f);
			Ligne(pixel, x3, y3, num2, num3, 1f, couleur);
			x3 = num2;
			y3 = num3;
		}
	}

	private static float Hasard(int n)
	{
		float num = MathF.Sin((float)n * 12.9898f + 78.233f) * 43758.547f;
		return num - MathF.Floor(num);
	}
}
