using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Telegraphe
{
	private sealed record Coup(long Debut, long Impact, bool Imparable, Guid Cible);

	private static readonly Dictionary<Guid, Coup> Coups = new Dictionary<Guid, Coup>();

	private static readonly Color Blanc = new Color(255, 255, 255, 255);

	private static readonly Color Orange = new Color(255, 255, 138, 31);

	private const int ContourAllumeMs = 70;

	private const int ContourPeriodeMs = 130;

	private const int ContourClignotements = 3;

	private const int EcouteDeLaRiposte = 12;

	private const int ContourMaxPx = 4;

	internal static Action<string, Entity, float>? TemoinDuSon;

	internal const int ContourPx = 1;

	internal static Color TeinteDuCoupArme => Blanc;

	internal static Color TeinteDeLImparable => Orange;

	public static void Recevoir(NpcNoticePacket packet)
	{
		if (!Globals.Entities.TryGetValue(packet.Id, out Entity value) || value == null)
		{
			Coups.Remove(packet.Id);
			return;
		}
		long maintenant = Timing.Global.Milliseconds;
		if (Heritage.Actif)
		{
			LacherLeCoupDAvant(packet.Id, packet.Genre == NpcNoticeGenre.Annule, maintenant);
		}
		if (packet.Genre == NpcNoticeGenre.Annule)
		{
			Coups.Remove(packet.Id);
			return;
		}
		bool flag = packet.Genre == NpcNoticeGenre.Imparable;
		Coups[packet.Id] = new Coup(maintenant, maintenant + Math.Max(0, packet.DurationMs), flag, packet.Cible);
		if (packet.Pas > 0)
		{
			value.ChoisirLaPlancheDuPas(packet.Pas);
		}
		if (Coups.Count > 64)
		{
			Guid[] array = (from paire in Coups
				where paire.Value.Impact < maintenant
				select paire.Key).ToArray();
			foreach (Guid key in array)
			{
				Coups.Remove(key);
			}
		}
		Player me = Globals.Me;
		if (me != null && !(packet.Cible != me.Id))
		{
			if (flag)
			{
				CombatFeedback.Callout(packet.Id, "!!", Orange, 16);
			}
			else if (packet.Marque)
			{
				CombatFeedback.Callout(packet.Id, "!", Blanc, 14);
			}
			if (Heritage.Actif)
			{
				AlerterHeritage(value, flag, maintenant);
			}
			if ((flag || packet.Pas <= 1 || !EstCombattant(value)) && DansLeDos(me, value))
			{
				CombatFeedback.Callout(me.Id, flag ? "!!" : "!", flag ? Orange : Blanc, 12);
			}
		}
	}

	private static bool EstCombattant(Entity monstre)
	{
		string nom = Path.GetFileNameWithoutExtension(monstre.Sprite ?? string.Empty);
		string[] combattantsSprites = Options.Instance.Combat.CombattantsSprites;
		if (combattantsSprites != null && combattantsSprites.Length > 0)
		{
			return combattantsSprites.Any((string combattant) => string.Equals(combattant, nom, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	public static bool EnCours(Entity entity)
	{
		if (Coups.Count == 0 || !Coups.TryGetValue(entity.Id, out Coup value))
		{
			return false;
		}
		if (Timing.Global.Milliseconds < value.Impact)
		{
			return true;
		}
		Coups.Remove(entity.Id);
		return false;
	}

	public static void DessinerContour(Entity entity, FloatRect destination, IGameTexture? texture, FloatRect source)
	{
		if (Heritage.Actif)
		{
			Heritage.Lisere.Dessiner(entity, destination, texture, source);
		}
		if (texture == null || source.Width <= 0f || Coups.Count == 0 || !Coups.TryGetValue(entity.Id, out Coup value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = milliseconds - value.Debut;
		if (Heritage.Actif && milliseconds >= value.Impact)
		{
			LacherLeMot(entity.Id);
		}
		if ((Heritage.Actif && ContourDuCoupArmeHeritage(entity, destination, texture, source, value, milliseconds)) || milliseconds >= value.Impact || Globals.Me == null || value.Cible != Globals.Me.Id || num >= 390 || num % 130 >= 70)
		{
			return;
		}
		if (Heritage.Actif)
		{
			TracerLeContour(entity, destination, texture, source, value.Imparable ? Orange : Blanc, milliseconds);
			return;
		}
		IGameTexture gameTexture = KiCharge.MaskOf(texture, source, 1);
		if (gameTexture != null)
		{
			float num2 = destination.Width / source.Width;
			float num3 = destination.Height / source.Height;
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(destination.X - num2, destination.Y - num3, destination.Width + 2f * num2, destination.Height + 2f * num3), value.Imparable ? Orange : Blanc);
		}
	}

	private static bool DansLeDos(Entity moi, Entity attaquant)
	{
		(float X, float Y) tuple = CorpsACorps.Vecteur(moi.DirectionFacing);
		float item = tuple.X;
		float item2 = tuple.Y;
		float num = attaquant.Origin.X - moi.Origin.X;
		float num2 = attaquant.Origin.Y - moi.Origin.Y;
		return item * num + item2 * num2 < 0f;
	}

	internal static int EpaisseurDuContour(int reglage)
	{
		if (reglage <= 0)
		{
			return 1;
		}
		return Math.Min(reglage, 4);
	}

	internal static bool ContourAllume(long depuis, long duree, bool tenu)
	{
		if (depuis < duree)
		{
			if (!tenu)
			{
				if (depuis < 390)
				{
					return depuis % 130 < 70;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal static bool SonneLaRiposte(bool tenace, bool imparable, string? fichier, int volume)
	{
		if (tenace && !imparable && !string.IsNullOrEmpty(fichier))
		{
			return volume > 0;
		}
		return false;
	}

	internal static float VolumeDuSon(int pourCent)
	{
		return (float)Math.Clamp(pourCent, 0, 100) / 100f;
	}

	internal static void AlerterHeritage(Entity monstre, bool imparable, long maintenant)
	{
		HeritageRiposteOptions riposte = Options.Instance.Combat.Heritage.Riposte;
		CombatFeedback.GrandirLAvertissement(monstre.Id, maintenant, riposte.AvertissementPoints);
		if (SonneLaRiposte(Heritage.Riposte.Tenace(monstre.Id, maintenant), imparable, riposte.Son, riposte.VolumeDuSon))
		{
			SonnerLaRiposte(monstre, riposte.Son, VolumeDuSon(riposte.VolumeDuSon));
		}
	}

	private static void SonnerLaRiposte(Entity monstre, string fichier, float volume)
	{
		TemoinDuSon?.Invoke(fichier, monstre, volume);
		MapSound mapSound = Audio.AddMapSound(fichier, monstre.X, monstre.Y, monstre.MapId, loop: false, 0, 12, monstre);
		if (mapSound != null)
		{
			mapSound.Fade = volume;
		}
	}

	private static bool ContourDuCoupArmeHeritage(Entity entite, FloatRect destination, IGameTexture texture, FloatRect source, Coup coup, long maintenant)
	{
		HeritageRiposteOptions riposte = Options.Instance.Combat.Heritage.Riposte;
		if (!riposte.ContourTenu && riposte.ContourPx <= 0)
		{
			return false;
		}
		Player me = Globals.Me;
		if (me != null && coup.Cible == me.Id && ContourAllume(maintenant - coup.Debut, coup.Impact - coup.Debut, riposte.ContourTenu))
		{
			TracerLeContour(entite, destination, texture, source, coup.Imparable ? Orange : Blanc, maintenant, EpaisseurDuContour(riposte.ContourPx));
		}
		return true;
	}

	internal static bool TracerLeContour(Entity entite, FloatRect destination, IGameTexture texture, FloatRect source, Color couleur, long maintenant, int px)
	{
		IGameTexture gameTexture = ((Heritage.Lisere.Masque != null) ? Heritage.Lisere.Masque(texture, source, px) : KiCharge.MaskOf(texture, source, px));
		if (gameTexture == null)
		{
			return false;
		}
		var (penche, tremble) = Heritage.Lisere.PoseDuCorps(entite, maintenant);
		Heritage.Lisere.Tracer(gameTexture, destination, source, px, penche, tremble, couleur);
		return true;
	}

	internal static bool ArmeContre(Guid cibleId)
	{
		if (Coups.Count == 0 || cibleId == Guid.Empty)
		{
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		foreach (var (key, coup2) in Coups)
		{
			if (coup2.Cible == cibleId && milliseconds < coup2.Impact && Globals.Entities.TryGetValue(key, out Entity value) && value != null && !value.IsDying)
			{
				return true;
			}
		}
		return false;
	}

	internal static void LacherLeMot(Guid monstreId)
	{
		if (Coups.Remove(monstreId, out Coup value))
		{
			CombatFeedback.RetirerLAvertissement(monstreId, value.Debut);
			if (value.Cible != Guid.Empty && !ArmeContre(value.Cible))
			{
				CombatFeedback.RetirerLAvertissement(value.Cible, value.Debut);
			}
		}
	}

	internal static void LacherLeCoupDAvant(Guid monstreId, bool annule, long maintenant)
	{
		if (annule || (Coups.TryGetValue(monstreId, out Coup value) && maintenant >= value.Impact))
		{
			LacherLeMot(monstreId);
		}
	}

	internal static void SurPlante(Guid monstreId)
	{
		if (!Coups.TryGetValue(monstreId, out Coup value) || !value.Imparable || Timing.Global.Milliseconds >= value.Impact)
		{
			CombatFeedback.RetirerLAvertissement(monstreId, long.MinValue);
		}
	}

	internal static bool TracerLeContour(Entity entite, FloatRect destination, IGameTexture texture, FloatRect source, Color couleur, long maintenant)
	{
		IGameTexture gameTexture = ((Heritage.Lisere.Masque != null) ? Heritage.Lisere.Masque(texture, source, 1) : KiCharge.MaskOf(texture, source, 1));
		if (gameTexture == null)
		{
			return false;
		}
		var (penche, tremble) = Heritage.Lisere.PoseDuCorps(entite, maintenant);
		Heritage.Lisere.Tracer(gameTexture, destination, source, 1, penche, tremble, couleur);
		return true;
	}
}
