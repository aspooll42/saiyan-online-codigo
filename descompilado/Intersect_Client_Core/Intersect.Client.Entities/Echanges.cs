using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Client;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Echanges
{
	private sealed class Etat
	{
		public Dash? Vol;

		public int Sens = 1;

		public long ChuteDebut;

		public float ChuteDepuis;

		public long TitubeDebut;

		public long SonneDebut;

		public long SonneJusqua;

		public Dash? Bond;

		public long ChargeDebut;

		public long ArmementJusqua;

		public Guid ArmementCibleId;

		public float ChargeNiveau;

		public long ProtectionDebut;

		public long ProtectionFin;

		public long ResisteA;

		public bool Recuperee;

		public bool Glisse;

		public long ProchainePoussiere;

		public long SonneServeurJusqua;

		public bool AuRepos(long maintenant)
		{
			if (Vol == null && Bond == null && ChuteDebut == 0L && ChargeDebut == 0L && ArmementJusqua == 0L && maintenant > SonneJusqua + 800)
			{
				return maintenant > ProtectionFin;
			}
			return false;
		}
	}

	private static readonly Color Argent = new Color(255, 214, 220, 232);

	private static readonly Color OrVif = new Color(255, 255, 211, 92);

	private static readonly Color RougeVif = new Color(255, 255, 140, 120);

	private static readonly Color Cyan = new Color(255, 150, 225, 255);

	public const float SeuilLourd = 14f;

	private const long TitubeMs = 600L;

	private const long ContournementMs = 700L;

	private const float ArcDuVol = 14f;

	private const float AtterrissageTemps = 0.45f;

	private const float PencheDuVol = 6f;

	private const float ArcDuBond = 6f;

	private const long ChuteMs = 110L;

	private const long CratereTenuMs = 2500L;

	private const long CratereFonduMs = 1500L;

	private const long OndeMs = 480L;

	private static readonly Dictionary<Guid, Etat> Etats = new Dictionary<Guid, Etat>();

	private static readonly Dictionary<Guid, Dash> Poursuites = new Dictionary<Guid, Dash>();

	private static readonly Dictionary<Guid, long> MesProjections = new Dictionary<Guid, long>();

	private static readonly List<(float X, float Y, long Debut)> Crateres = new List<(float, float, long)>();

	private static readonly List<(float X, float Y, long Debut, float Force)> Ondes = new List<(float, float, long, float)>();

	private static Guid _renvoiId;

	private static long _renvoiJusqua;

	private static long _contournementJusqua;

	private static Dash? _rueeGlissade;

	private static Guid _rueeCibleId;

	private static readonly List<(float X, float Y, long Debut, float Rayon, bool Feu)> Kiais = new List<(float, float, long, float, bool)>();

	private static readonly List<(float X, float Y, float DirX, float DirY, long Debut)> Fissures = new List<(float, float, float, float, long)>();

	private static readonly Dictionary<Guid, (long Debut, long Fin)> Ripostes = new Dictionary<Guid, (long, long)>();

	private const long KiaiMs = 420L;

	private const long ExplosionMs = 640L;

	private const long FissureMs = 900L;

	public static void OnEchange(EchangePacket packet)
	{
		if (Clash.OnEchange(packet))
		{
			return;
		}
		EchangeEvent echangeEvent = packet.Event;
		if (echangeEvent == EchangeEvent.Choc)
		{
			if (Heritage.Actif)
			{
				Heritage.SurChoc(packet);
			}
			Choc.OnChoc(packet);
		}
		else
		{
			if (!Globals.Entities.TryGetValue(packet.EntityId, out Entity value) || value == null)
			{
				return;
			}
			long milliseconds = Timing.Global.Milliseconds;
			bool flag = value == Globals.Me;
			CombatFeedback.MotsMuets = Heritage.Actif && Heritage.Mots.Muet(packet.Event, value);
			try
			{
				switch (packet.Event)
				{
				case EchangeEvent.Sonne:
				{
					Etat etat2 = EtatDe(value);
					etat2.ProtectionDebut = milliseconds + Math.Max(0, packet.DureeMs);
					etat2.ProtectionFin = etat2.ProtectionDebut + Math.Max(0, packet.Valeur);
					if (packet.Index == 1)
					{
						etat2.SonneDebut = milliseconds;
						etat2.SonneJusqua = Math.Max(etat2.SonneJusqua, milliseconds + packet.DureeMs);
						etat2.ChargeDebut = 0L;
						etat2.ArmementJusqua = 0L;
					}
					else
					{
						etat2.SonneServeurJusqua = milliseconds + Math.Max(0, packet.DureeMs);
					}
					break;
				}
				case EchangeEvent.Moitie:
					if (flag)
					{
						CombatFeedback.Callout(value.Id, Strings.Echanges.Moitie, Argent, 11);
					}
					break;
				case EchangeEvent.Immunise:
					EtatDe(value).ResisteA = milliseconds;
					CombatFeedback.Callout(value.Id, Strings.Echanges.Immunise, Argent, 12);
					break;
				case EchangeEvent.Surpris:
					CombatFeedback.Callout(value.Id, Strings.Echanges.Surpris, RougeVif, 13);
					if (flag)
					{
						CombatFeedback.StartShake(3f, 160L);
					}
					break;
				case EchangeEvent.Recuperation:
				{
					Etat etat = EtatDe(value);
					etat.Recuperee = true;
					long milliseconds2 = Timing.Global.Milliseconds;
					etat.SonneJusqua = Math.Min(etat.SonneJusqua, milliseconds2);
					value.GuardStunnedUntil = Math.Min(value.GuardStunnedUntil, milliseconds2);
					Afterimages.Vanish(value, 0f, 0f);
					CombatFeedback.Callout(value.Id, Strings.Echanges.Recuperation, flag ? Cyan : RougeVif, 13);
					break;
				}
				case EchangeEvent.Renvoi:
					CombatFeedback.Callout(value.Id, Strings.Echanges.Renvoye, OrVif, 13);
					if (!flag)
					{
						Guid autreId = packet.AutreId;
						Guid? obj2 = Globals.Me?.Id;
						if (!(autreId == obj2))
						{
							break;
						}
					}
					CombatFeedback.HitStop(70L);
					CombatFeedback.StartShake(2.5f, 140L);
					break;
				case EchangeEvent.Quille:
					value.HitFlashUntil = milliseconds + 130;
					CombatFeedback.Callout(value.Id, Strings.Echanges.Quille, OrVif, 12);
					if (!flag)
					{
						Guid autreId = packet.AutreId;
						Guid? obj3 = Globals.Me?.Id;
						if (!(autreId == obj3))
						{
							break;
						}
					}
					CombatFeedback.StartShake(2f, 120L);
					break;
				case EchangeEvent.Kiai:
					if (!Heritage.Actif || !Heritage.Kiai.Jouer(value, packet))
					{
						OnKiai(value, packet.Valeur, feu: false);
						CombatFeedback.Callout(value.Id, Strings.Echanges.Kiai, Cyan, 13);
					}
					break;
				case EchangeEvent.Explosion:
					OnKiai(value, packet.DureeMs, feu: true);
					CombatFeedback.Callout(value.Id, Strings.Echanges.Explosion, OrVif, 14);
					break;
				case EchangeEvent.OndeDeChoc:
					if (!Heritage.Actif || !Heritage.Onde.Jouer(value, packet))
					{
						OnOndeDeChoc(value, (Direction)packet.Valeur);
					}
					break;
				case EchangeEvent.Renverse:
					value.HitFlashUntil = milliseconds + 130;
					CombatFeedback.Callout(value.Id, Strings.Echanges.Renverse, OrVif, 12);
					break;
				case EchangeEvent.Riposte:
					OnRiposte(value, packet.DureeMs);
					break;
				case EchangeEvent.RiposteContre:
					CombatFeedback.Callout(value.Id, Strings.Echanges.Riposte, flag ? OrVif : RougeVif, 13);
					if (!flag)
					{
						Guid autreId = packet.AutreId;
						Guid? obj = Globals.Me?.Id;
						if (!(autreId == obj))
						{
							break;
						}
					}
					CombatFeedback.HitStop(60L);
					CombatFeedback.StartShake(2.5f, 140L);
					break;
				case EchangeEvent.Resiste:
					EtatDe(value).ResisteA = milliseconds;
					CombatFeedback.Callout(value.Id, Strings.Echanges.Resiste, Argent, 11);
					break;
				case EchangeEvent.ChargeDebut:
					if (!flag)
					{
						Etat etat4 = EtatDe(value);
						etat4.ChargeDebut = milliseconds;
						etat4.ArmementJusqua = 0L;
						value.ChoisirLaPlancheDuPas(1);
					}
					break;
				case EchangeEvent.ChargeLacher:
					if (!flag)
					{
						Etat etat3 = EtatDe(value);
						etat3.ChargeDebut = 0L;
						etat3.ChargeNiveau = Math.Clamp((float)packet.Valeur / 100f, 0f, 1f);
						etat3.ArmementJusqua = milliseconds + Math.Max(1, packet.DureeMs);
						etat3.ArmementCibleId = packet.AutreId;
						value.ChoisirLaPlancheDuPas((byte)Math.Max(1, Options.Instance.Combat.ComboHits));
					}
					break;
				case EchangeEvent.ChargeAnnulee:
				{
					if (Etats.TryGetValue(value.Id, out Etat value2))
					{
						value2.ChargeDebut = 0L;
						value2.ArmementJusqua = 0L;
					}
					break;
				}
				case EchangeEvent.Avantage:
					CombatFeedback.Callout(value.Id, Strings.Echanges.ACoupSur, OrVif, 13);
					break;
				case EchangeEvent.CoupRefuse:
					if (Heritage.Actif && flag)
					{
						Heritage.OnRefus(packet);
					}
					break;
				case EchangeEvent.Souleve:
					if (Heritage.Actif)
					{
						Heritage.Soulever(value, packet.DureeMs, packet.Valeur);
					}
					break;
				case EchangeEvent.Etat:
					if (Heritage.Actif)
					{
						Heritage.EtatsVus.Recevoir(value, packet);
					}
					break;
				case EchangeEvent.Arret:
					if (Heritage.Actif)
					{
						Heritage.Decor.SurArret(value, packet);
					}
					break;
				case EchangeEvent.Choc:
					break;
				}
			}
			finally
			{
				CombatFeedback.MotsMuets = false;
			}
		}
	}

	public static bool ChargeLocale(Entity entity)
	{
		if (entity == Globals.Me && Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value))
		{
			return value.ChargeDebut > 0;
		}
		return false;
	}

	public static long ChargeDepuis(Entity entity)
	{
		if (!Etats.TryGetValue(entity.Id, out Etat value) || value.ChargeDebut <= 0)
		{
			return 0L;
		}
		return Timing.Global.Milliseconds - value.ChargeDebut;
	}

	public static bool ArmementEnCours(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value))
		{
			return value.ArmementJusqua > 0;
		}
		return false;
	}

	public static void CommencerLaCharge(Player player)
	{
		Etat etat = EtatDe(player);
		etat.ChargeDebut = Timing.Global.Milliseconds;
		etat.ArmementJusqua = 0L;
	}

	public static void AbandonnerLaCharge(Player player)
	{
		if (Etats.TryGetValue(player.Id, out Etat value) && value.ChargeDebut != 0L)
		{
			value.ChargeDebut = 0L;
			if (player == Globals.Me)
			{
				PacketSender.SendAttack(Guid.Empty, ruee: false, GesteDeCoup.ChargeAnnulee, 0);
			}
		}
	}

	public static void LacherLaChargeLocale(Player player, Entity? cible)
	{
		CombatOptions combat = Options.Instance.Combat;
		long milliseconds = Timing.Global.Milliseconds;
		Etat etat = EtatDe(player);
		etat.ChargeNiveau = ((etat.ChargeDebut > 0) ? Math.Clamp((float)(milliseconds - etat.ChargeDebut) / (float)Math.Max(1, combat.CoupChargePleineMs), 0f, 1f) : 0f);
		etat.ChargeDebut = 0L;
		etat.ArmementJusqua = milliseconds + Math.Max(1, combat.CoupChargeArmementMs);
		etat.ArmementCibleId = cible?.Id ?? Guid.Empty;
		if (cible != null && player.WorldPos.Width > 0f && cible.WorldPos.Width > 0f)
		{
			float num = cible.WorldPos.X + cible.WorldPos.Width / 2f - (player.WorldPos.X + player.WorldPos.Width / 2f);
			float num2 = cible.WorldPos.Y + cible.WorldPos.Height - (player.WorldPos.Y + player.WorldPos.Height);
			player.DirectionFacing = ((!(MathF.Abs(num) >= MathF.Abs(num2))) ? ((!(num2 < 0f)) ? Direction.Down : Direction.Up) : ((num < 0f) ? Direction.Left : Direction.Right));
		}
	}

	public static void SuivreLaCharge(Player player)
	{
		if (player == Globals.Me && Etats.Count != 0 && Etats.TryGetValue(player.Id, out Etat value) && value.ArmementJusqua != 0L && Timing.Global.Milliseconds >= value.ArmementJusqua)
		{
			value.ArmementJusqua = 0L;
			int num = player.CalculateAttackTime();
			player.AttackTimer = Timing.Global.Milliseconds + num;
			CombatAudio.Swing(player);
			CombatFeedback.StartShake(2f + 2f * value.ChargeNiveau, 150L);
			if (Globals.Entities.TryGetValue(value.ArmementCibleId, out Entity value2) && value2 != null && !value2.IsDying)
			{
				CombatFeedback.OnLocalStrike(value2, num);
			}
		}
	}

	public static bool PoseDeCombat(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value))
		{
			if (value.ChargeDebut <= 0)
			{
				return value.ArmementJusqua > 0;
			}
			return true;
		}
		return false;
	}

	public static int? ImageDeCombat(Entity entity, int images)
	{
		if (images <= 0)
		{
			return null;
		}
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return null;
		}
		if (value.ChargeDebut <= 0 && value.ArmementJusqua <= 0)
		{
			return null;
		}
		return 0;
	}

	private static void DessinerChargesOuProtections(IGameTexture pixel, bool charges)
	{
		long milliseconds = Timing.Global.Milliseconds;
		foreach (var (key, etat2) in Etats)
		{
			bool num;
			if (!charges)
			{
				if (milliseconds < etat2.ProtectionDebut)
				{
					continue;
				}
				num = milliseconds < etat2.ProtectionFin;
			}
			else
			{
				if (etat2.ChargeDebut > 0)
				{
					goto IL_006c;
				}
				num = etat2.ArmementJusqua > 0;
			}
			if (!num)
			{
				continue;
			}
			goto IL_006c;
			IL_006c:
			if (!Globals.Entities.TryGetValue(key, out Entity value) || value == null || value.IsDying || value.IsDisposed)
			{
				continue;
			}
			FloatRect worldPos = value.WorldPos;
			if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f))
			{
				if (charges)
				{
					Flamme(pixel, value, worldPos, etat2, milliseconds);
				}
				else
				{
					AnneauDeProtection(pixel, value, worldPos, etat2, milliseconds);
				}
			}
		}
	}

	public static (float Rayon, float Force, Color Couleur)? LueurDe(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value) || (value.ChargeDebut <= 0 && value.ArmementJusqua <= 0))
		{
			return null;
		}
		float num = ((value.ChargeDebut > 0) ? Math.Clamp((float)(Timing.Global.Milliseconds - value.ChargeDebut) / (float)Math.Max(1, Options.Instance.Combat.CoupChargePleineMs), 0f, 1f) : value.ChargeNiveau);
		return (24f + 18f * num, 0.5f + 0.45f * num, new Color(255, 255, 150, 60));
	}

	private static void Flamme(IGameTexture pixel, Entity entity, FloatRect cadre, Etat etat, long maintenant)
	{
		CombatOptions combat = Options.Instance.Combat;
		float num = ((etat.ChargeDebut > 0) ? Math.Clamp((float)(maintenant - etat.ChargeDebut) / (float)Math.Max(1, combat.CoupChargePleineMs), 0f, 1f) : etat.ChargeNiveau);
		float num2 = Hauteur(entity);
		int num3;
		switch (entity.DirectionFacing)
		{
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			num3 = -1;
			break;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			num3 = 1;
			break;
		default:
			num3 = 0;
			break;
		}
		int num4 = num3;
		float x = cadre.X + cadre.Width / 2f + ((num4 != 0) ? ((float)(-num4) * 11f) : 7f);
		float y = cadre.Y + cadre.Height - 13f - num2;
		long num5 = maintenant / 55;
		Random random = new Random(((int)num5 * 7919) ^ entity.Id.GetHashCode());
		int num6 = 4 + (int)MathF.Round(num * 5f);
		for (int i = 0; i < num6; i++)
		{
			float angle = -(float)Math.PI / 2f + ((float)random.NextDouble() - 0.5f) * 2.2f;
			float longueur = 4f + num * 8f + (float)random.NextDouble() * 5f;
			Color couleur = ((i % 2 == 0) ? new Color(235, 255, 122, 26) : new Color(240, 255, 210, 138));
			Langue(pixel, x, y, angle, longueur, 1.4f + num, couleur);
		}
		Disque(pixel, x, y, 2f + num * 2.5f, new Color(245, 255, 210, 138));
		Disque(pixel, x, y, 1f + num * 1.2f, new Color(255, 255, 255, 255));
		if (etat.ChargeDebut > 0)
		{
			float num7 = cadre.X + cadre.Width / 2f;
			float num8 = cadre.Y + cadre.Height - 1f;
			bool flag = num >= 1f && maintenant / 120 % 2 == 0;
			for (int j = 0; j < 48; j++)
			{
				float x2 = (float)Math.PI / 2f + (float)j / 48f * (float)Math.PI * 2f;
				Color renderColor = (((float)(j + 1) / 48f <= num + 0.001f) ? (flag ? new Color(255, 255, 255, 255) : new Color(255, 255, 210, 138)) : new Color(128, 20, 12, 4));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num7 + MathF.Cos(x2) * 13f), MathF.Round(num8 + MathF.Sin(x2) * 4.5f), 1f, 1f), renderColor);
			}
		}
	}

	private static void AnneauDeProtection(IGameTexture pixel, Entity entity, FloatRect cadre, Etat etat, long maintenant)
	{
		float num = Math.Clamp((float)(etat.ProtectionFin - maintenant) / (float)Math.Max(1L, etat.ProtectionFin - etat.ProtectionDebut), 0f, 1f);
		float num2 = MathF.Round(cadre.X + cadre.Width / 2f);
		float num3 = MathF.Round(cadre.Y - 6f - Hauteur(entity));
		Color color = ((maintenant - etat.ResisteA < 350) ? new Color(255, 255, 255, 255) : new Color(240, 238, 240, 244));
		Color color2 = new Color(150, 20, 24, 38);
		for (int i = 0; i < 18; i++)
		{
			float x = -(float)Math.PI / 2f + (float)i / 18f * (float)Math.PI * 2f;
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num2 + MathF.Cos(x) * 5f), MathF.Round(num3 + MathF.Sin(x) * 5f), 1f, 1f), ((float)i / 18f < num) ? color : color2);
		}
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num2, num3 - 1f, 1f, 3f), color);
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num2 - 1f, num3, 3f, 1f), color);
	}

	private static void Langue(IGameTexture pixel, float x, float y, float angle, float longueur, float pied, Color couleur)
	{
		float num = MathF.Cos(angle);
		float num2 = MathF.Sin(angle);
		int num3 = Math.Max(1, (int)MathF.Ceiling(longueur));
		for (int i = 0; i <= num3; i++)
		{
			float num4 = (float)i / (float)num3;
			float num5 = pied * (1f - num4);
			float num6 = x + num * longueur * num4;
			float x2 = y + num2 * longueur * num4;
			float num7 = MathF.Max(1f, MathF.Round(num5 * 2f));
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num6 - num7 / 2f), MathF.Round(x2), num7, 1f), couleur);
		}
	}

	private static void Disque(IGameTexture pixel, float x, float y, float rayon, Color couleur)
	{
		int num = (int)MathF.Ceiling(rayon);
		for (int i = -num; i <= num; i++)
		{
			float num2 = rayon * rayon - (float)(i * i);
			if (!(num2 < 0f))
			{
				float num3 = MathF.Floor(MathF.Sqrt(num2));
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x - num3), MathF.Round(y + (float)i), num3 * 2f + 1f, 1f), couleur);
			}
		}
	}

	public static bool EstLourd(Entity entity)
	{
		return entity.RayonDuCorps > 14f;
	}

	private static Etat EtatDe(Entity entity)
	{
		if (!Etats.TryGetValue(entity.Id, out Etat value))
		{
			value = new Etat();
			Etats[entity.Id] = value;
			if (Etats.Count > 128)
			{
				Etats.Clear();
				Etats[entity.Id] = value;
			}
		}
		return value;
	}

	public static void SuivreLaRuee(Player player)
	{
		if (_rueeGlissade != null && player.Dashing != _rueeGlissade)
		{
			_rueeGlissade = null;
			if (!Heritage.Actif && Globals.Entities.TryGetValue(_rueeCibleId, out Entity value) && value != null && !value.IsDying)
			{
				CombatAudio.Swing(player);
				CombatFeedback.OnLocalStrike(value, player.CalculateAttackTime());
			}
		}
	}

	public static void OnDash(Entity entity, Dash dash)
	{
		if (Heritage.Actif)
		{
			Heritage.Mouvement.SurGlissade(entity, dash);
			Heritage.Decor.SurGlissade(entity, dash);
		}
		switch (dash.Nature)
		{
		case NatureDeGlissade.Projection:
		{
			float item = dash.StartOffset.X;
			float item2 = dash.EndOffset.X;
			Etat etat = EtatDe(entity);
			etat.Vol = dash;
			etat.Sens = ((item2 - item >= 0f) ? 1 : (-1));
			etat.Glisse = false;
			etat.ChuteDebut = 0L;
			etat.SonneJusqua = 0L;
			etat.TitubeDebut = 0L;
			if (Globals.Me != null && dash.AuteurId == Globals.Me.Id)
			{
				MesProjections[entity.Id] = dash.EndsAt;
			}
			break;
		}
		case NatureDeGlissade.Chute:
			ChuteDansLeVide.Commencer(entity, dash);
			Poussiere(entity, 6, 0.9f);
			break;
		case NatureDeGlissade.Bond:
			EtatDe(entity).Bond = dash;
			Poussiere(entity, 3, 0.5f);
			break;
		case NatureDeGlissade.Dash:
			DashAnime.Depart(entity, dash);
			break;
		case NatureDeGlissade.Ruee:
			if (entity == Globals.Me)
			{
				_rueeGlissade = dash;
				_rueeCibleId = dash.AuteurId;
			}
			break;
		case NatureDeGlissade.Poursuite:
			var (num, num2) = dash.StartOffset;
			var (num3, num4) = dash.EndOffset;
			Afterimages.Vanish(entity, num3 - num, num4 - num2);
			Poursuites[entity.Id] = dash;
			Audio.AddMapSound("Esquive.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
			if (entity == Globals.Me)
			{
				_renvoiId = dash.AuteurId;
				_renvoiJusqua = Timing.Global.Milliseconds + 800;
				_contournementJusqua = 0L;
				if (MesProjections.TryGetValue(dash.AuteurId, out var value))
				{
					_renvoiJusqua = value + 250;
				}
			}
			break;
		}
	}

	public static void OnCoupFinal(Entity? attaquant, Entity cible)
	{
		if (!EstLourd(cible) || cible.IsDying || cible.IsBlocking)
		{
			return;
		}
		if (Heritage.Actif && Heritage.Effets.PrendLeCoup(attaquant, cible, dejaMontre: true))
		{
			_contournementJusqua = ((attaquant != null && attaquant == Globals.Me) ? (Timing.Global.Milliseconds + 700) : _contournementJusqua);
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Etat etat = EtatDe(cible);
		etat.TitubeDebut = milliseconds;
		etat.SonneDebut = milliseconds;
		etat.SonneJusqua = milliseconds + 600;
		etat.Sens = ((attaquant == null || !(attaquant.WorldPos.X > cible.WorldPos.X)) ? 1 : (-1));
		FloatRect worldPos = cible.WorldPos;
		if (worldPos.Width > 0f && worldPos.Height > 0f)
		{
			float num = worldPos.X + worldPos.Width / 2f;
			float num2 = worldPos.Y + worldPos.Height;
			Ondes.Add((num, num2, milliseconds, 3f));
			Ondes.Add((num, num2, milliseconds + 90, 2f));
			for (int i = 0; i < 8; i++)
			{
				SprintDust.AddPuff(num + ((float)i - 3.5f) * 6f, num2, (i % 2 == 0) ? Direction.Left : Direction.Right);
			}
		}
		Player me = Globals.Me;
		if (me != null && (attaquant == me || cible == me))
		{
			CombatFeedback.StartShake(4f, 260L);
		}
		if (me != null && attaquant == me)
		{
			_contournementJusqua = milliseconds + 700;
		}
	}

	internal static Entity? ProjeteeRecemment(long maintenant, int apresMs)
	{
		Entity result = null;
		long num = long.MinValue;
		foreach (var (key, num3) in MesProjections)
		{
			if (maintenant <= num3 + Math.Max(0, apresMs) && num3 > num && Globals.Entities.TryGetValue(key, out Entity value) && value != null && !value.IsDying)
			{
				result = value;
				num = num3;
			}
		}
		if (MesProjections.Count > 32)
		{
			Guid[] array = (from paire in MesProjections
				where maintenant > paire.Value + 5000
				select paire.Key).ToArray();
			foreach (Guid key2 in array)
			{
				MesProjections.Remove(key2);
			}
		}
		return result;
	}

	public static bool EstProjetee(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value) && value.Vol != null && entity.Dashing == value.Vol)
		{
			return !value.Recuperee;
		}
		return false;
	}

	public static bool Cache(Entity entity)
	{
		if (Poursuites.Count == 0 || !Poursuites.TryGetValue(entity.Id, out Dash value))
		{
			return false;
		}
		if (entity.Dashing == value)
		{
			return true;
		}
		Poursuites.Remove(entity.Id);
		return false;
	}

	public static bool PoursuitePossible()
	{
		Player me = Globals.Me;
		if (me == null || me.Level < Options.Instance.Combat.PoursuiteNiveau)
		{
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (milliseconds <= _contournementJusqua)
		{
			return true;
		}
		foreach (long value in MesProjections.Values)
		{
			if (milliseconds < value - 90)
			{
				return true;
			}
		}
		MesProjections.Clear();
		return false;
	}

	public static Entity? PrendreLaCibleDuRenvoi()
	{
		if (_renvoiId == Guid.Empty)
		{
			return null;
		}
		Guid renvoiId = _renvoiId;
		_renvoiId = Guid.Empty;
		if (Timing.Global.Milliseconds > _renvoiJusqua || !Globals.Entities.TryGetValue(renvoiId, out Entity value) || value == null || value.IsDying)
		{
			return null;
		}
		return value;
	}

	public static void Observer(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.Vol != null && entity.Dashing == value.Vol)
		{
			float num = (float)(milliseconds - value.Vol.StartedAt) / (float)Math.Max(1L, value.Vol.EndsAt - value.Vol.StartedAt);
			if (num >= 0.45f && !value.Glisse)
			{
				value.Glisse = true;
				value.ProchainePoussiere = milliseconds + 45;
				Poussiere(entity, 4, 1.5f);
			}
			else if (value.Glisse && milliseconds >= value.ProchainePoussiere)
			{
				FloatRect worldPos = entity.WorldPos;
				if (worldPos.Width > 0f && worldPos.Height > 0f)
				{
					value.ProchainePoussiere = milliseconds + 45;
					SprintDust.AddPuff(worldPos.X + worldPos.Width / 2f, worldPos.Y + worldPos.Height, (value.Sens > 0) ? Direction.Left : Direction.Right);
				}
			}
		}
		if (value.Vol != null && entity.Dashing != value.Vol)
		{
			value.Vol = null;
			if (value.Recuperee)
			{
				value.Recuperee = false;
				value.SonneJusqua = 0L;
				value.SonneServeurJusqua = 0L;
				Poussiere(entity, 3, 0.5f);
			}
			else
			{
				value.SonneDebut = milliseconds;
				value.SonneJusqua = ((value.SonneServeurJusqua > 0) ? Math.Max(milliseconds, value.SonneServeurJusqua) : (milliseconds + Math.Max(0, Options.Instance.Combat.ProjectionAuSolMs)));
				value.SonneServeurJusqua = 0L;
				Poussiere(entity, 5, 0f);
			}
		}
		if (value.ChuteDebut > 0 && milliseconds - value.ChuteDebut >= 110)
		{
			value.ChuteDebut = 0L;
			value.SonneDebut = milliseconds;
			value.SonneJusqua = milliseconds + Math.Max(0, Options.Instance.Combat.ProjectionAuSolMs) + 400;
			FloatRect worldPos2 = entity.WorldPos;
			if (worldPos2.Width > 0f && worldPos2.Height > 0f)
			{
				Crateres.Add((worldPos2.X + worldPos2.Width / 2f, worldPos2.Y + worldPos2.Height, milliseconds));
				if (Crateres.Count > 16)
				{
					Crateres.RemoveAt(0);
				}
			}
			Poussiere(entity, 8, 4f);
		}
		if (value.Bond != null && entity.Dashing != value.Bond)
		{
			value.Bond = null;
		}
		if (entity != Globals.Me && value.ArmementJusqua > 0 && milliseconds >= value.ArmementJusqua)
		{
			value.ArmementJusqua = 0L;
		}
		if (value.AuRepos(milliseconds))
		{
			Etats.Remove(entity.Id);
		}
	}

	internal static void Poussiere(Entity entity, int bouffees, float onde)
	{
		FloatRect worldPos = entity.WorldPos;
		if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f))
		{
			float num = worldPos.X + worldPos.Width / 2f;
			float num2 = worldPos.Y + worldPos.Height;
			for (int i = 0; i < bouffees; i++)
			{
				SprintDust.AddPuff(num + ((float)i - (float)(bouffees - 1) / 2f) * 5f, num2, (i % 2 == 0) ? Direction.Left : Direction.Right);
			}
			Ondes.Add((num, num2, Timing.Global.Milliseconds, (onde > 0f) ? onde : 1f));
		}
	}

	public static bool EstSonne(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return false;
		}
		if ((value.Vol == null || entity.Dashing != value.Vol) && value.ChuteDebut <= 0)
		{
			return Timing.Global.Milliseconds < value.SonneJusqua;
		}
		return true;
	}

	public static float Hauteur(Entity entity)
	{
		float num = ChuteDansLeVide.Enfoncement(entity);
		if (num > 0f)
		{
			return 0f - num;
		}
		if (Heritage.Actif)
		{
			float num2 = Heritage.HauteurDe(entity);
			if (num2 > 0f)
			{
				return num2;
			}
		}
		if (Heritage.Actif && Heritage.Mouvement.AuSol(entity))
		{
			return 0f;
		}
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return 0f;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.ChuteDebut > 0)
		{
			float num3 = Math.Clamp((float)(milliseconds - value.ChuteDebut) / 110f, 0f, 1f);
			return value.ChuteDepuis * (1f - num3 * num3);
		}
		if (value.Vol != null && entity.Dashing == value.Vol)
		{
			float num4 = Math.Clamp((float)(milliseconds - value.Vol.StartedAt) / (float)Math.Max(1L, value.Vol.EndsAt - value.Vol.StartedAt), 0f, 1f);
			if (num4 >= 0.45f)
			{
				return 0f;
			}
			float num5 = num4 / 0.45f;
			return 56f * num5 * (1f - num5);
		}
		if (value.Bond != null && entity.Dashing == value.Bond)
		{
			float num6 = Math.Clamp((float)(milliseconds - value.Bond.StartedAt) / (float)Math.Max(1L, value.Bond.EndsAt - value.Bond.StartedAt), 0f, 1f);
			return 24f * num6 * (1f - num6);
		}
		return 0f;
	}

	public static (float Penche, float Tremble) Pose(Entity entity)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return (Penche: 0f, Tremble: 0f);
		}
		long milliseconds = Timing.Global.Milliseconds;
		float item = 0f;
		if (!(entity is Player))
		{
			if (value.Vol != null && entity.Dashing == value.Vol)
			{
				float num = Math.Clamp((float)(milliseconds - value.Vol.StartedAt) / (float)Math.Max(1L, value.Vol.EndsAt - value.Vol.StartedAt + 150), 0f, 1f);
				item = (float)value.Sens * 6f * (1f - num) * (1f - num);
			}
			else if (value.TitubeDebut > 0 && milliseconds - value.TitubeDebut < 720)
			{
				long num2 = milliseconds - value.TitubeDebut;
				item = ((num2 < 260) ? ((float)value.Sens * 5f * (1f - (float)num2 / 260f)) : ((num2 < 500) ? ((float)(-value.Sens) * 3f * (1f - (float)(num2 - 260) / 240f)) : ((float)value.Sens * 2f * (1f - (float)(num2 - 500) / 220f))));
			}
		}
		float item2 = 0f;
		if (milliseconds < value.SonneJusqua && entity.Dashing == null && !entity.IsDying)
		{
			item2 = (((milliseconds - value.SonneDebut) / 40 % 2 == 0L) ? 1f : (-1f));
		}
		return (Penche: item, Tremble: item2);
	}

	public static void OnEcrase(CombatImpactPacket packet)
	{
		if (Globals.Entities.TryGetValue(packet.TargetId, out Entity value) && value != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			Etat etat = EtatDe(value);
			etat.ChuteDepuis = Hauteur(value) + 8f;
			etat.ChuteDebut = milliseconds;
			etat.Vol = null;
			MesProjections.Remove(value.Id);
			CombatAudio.Play("crithit.wav", value);
			Player me = Globals.Me;
			if (me != null && (packet.AttackerId == me.Id || packet.TargetId == me.Id))
			{
				CombatFeedback.HitStop(90L);
				CombatFeedback.StartShake(5f, 280L);
				CombatFeedback.PunchZoom(0.05f, 280L);
			}
		}
	}

	public static void DrawGround()
	{
		if (Heritage.Actif)
		{
			Heritage.Effets.DessinerAuSol();
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		DessinerLesTechniquesAuSol(gameTexture);
		if (Crateres.Count == 0 && Ondes.Count == 0)
		{
			return;
		}
		long maintenant = Timing.Global.Milliseconds;
		Crateres.RemoveAll(((float X, float Y, long Debut) cratere) => maintenant - cratere.Debut > 4000);
		foreach (var cratere in Crateres)
		{
			float item = cratere.X;
			float item2 = cratere.Y;
			long item3 = cratere.Debut;
			long num = maintenant - item3;
			float num2 = ((num <= 2500) ? 1f : (1f - (float)(num - 2500) / 1500f));
			if (!(num2 <= 0f))
			{
				Color renderColor = new Color((int)(120f * num2), 36, 22, 10);
				for (int num3 = -6; num3 <= 6; num3++)
				{
					float num4 = MathF.Floor(15f * MathF.Sqrt(1f - (float)(num3 * num3) / 36f));
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(item - num4), MathF.Round(item2 + (float)num3), num4 * 2f + 1f, 1f), renderColor);
				}
				Color renderColor2 = new Color((int)(130f * num2), 201, 177, 132);
				for (int num5 = 0; num5 < 18; num5++)
				{
					float x = (float)num5 / 18f * (float)Math.PI * 2f;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(item + MathF.Cos(x) * 16f), MathF.Round(item2 + MathF.Sin(x) * 7f), 1f, 1f), renderColor2);
				}
			}
		}
		Ondes.RemoveAll(((float X, float Y, long Debut, float Force) onde) => maintenant - onde.Debut > 480);
		foreach (var (num6, num7, num8, num9) in Ondes)
		{
			if (maintenant >= num8)
			{
				float num10 = (float)(maintenant - num8) / 480f;
				float num11 = 6f + num10 * (14f + num9 * 8f);
				float num12 = num11 * 0.36f;
				int num13 = 40 + (int)(num9 * 8f);
				Color renderColor3 = new Color((int)(215f * (1f - num10)), 255, 240, 184);
				for (int num14 = 0; num14 < num13; num14++)
				{
					float x2 = (float)num14 / (float)num13 * (float)Math.PI * 2f;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num6 + MathF.Cos(x2) * num11), MathF.Round(num7 + MathF.Sin(x2) * num12), 1f, 1f), renderColor3);
				}
			}
		}
	}

	public static void DrawFront()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null)
		{
			Choc.Draw(gameTexture);
			Clash.Draw(gameTexture);
			if (Etats.Count > 0)
			{
				DessinerChargesOuProtections(gameTexture, charges: false);
			}
		}
	}

	public static void DrawUnderCanopy()
	{
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null || Etats.Count == 0)
		{
			return;
		}
		DessinerChargesOuProtections(gameTexture, charges: true);
		long milliseconds = Timing.Global.Milliseconds;
		foreach (var (key, etat2) in Etats)
		{
			if (milliseconds >= etat2.SonneJusqua || !Globals.Entities.TryGetValue(key, out Entity value) || value == null || value.IsDying || value.IsDisposed || value.Dashing != null)
			{
				continue;
			}
			FloatRect worldPos = value.WorldPos;
			if (!(worldPos.Width <= 0f) && !(worldPos.Height <= 0f))
			{
				Random random = new Random(((int)(milliseconds / 70) * 397) ^ key.GetHashCode());
				float num = Math.Clamp((float)(etat2.SonneJusqua - milliseconds) / 300f, 0f, 1f);
				int alpha = (int)(245f * MathF.Min(1f, num * 1.6f));
				for (int i = 0; i < 4; i++)
				{
					int x = (int)MathF.Round(worldPos.X + 8f + (float)random.NextDouble() * MathF.Max(1f, worldPos.Width - 16f));
					int y = (int)MathF.Round(worldPos.Y + worldPos.Height * 0.22f + (float)random.NextDouble() * worldPos.Height * 0.45f);
					Zigzag(gameTexture, x, y, (random.Next(2) == 0) ? 1 : (-1), alpha);
				}
			}
		}
	}

	private static void Zigzag(IGameTexture pixel, int x, int y, int side, int alpha)
	{
		Color renderColor = new Color(alpha, 255, 196, 50);
		Color renderColor2 = new Color(alpha, 255, 251, 224);
		_003C_003Ey__InlineArray4<(int, int)> buffer = default(_003C_003Ey__InlineArray4<(int, int)>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 0) = (0, 0);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 1) = (2 * side, 3);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 2) = (0, 4);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(ref buffer, 3) = (3 * side, 8);
		ReadOnlySpan<(int, int)> readOnlySpan = global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray4<(int, int)>, (int, int)>(in buffer, 4);
		for (int i = 0; i < readOnlySpan.Length - 1; i++)
		{
			(int, int) tuple = readOnlySpan[i];
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			(int, int) tuple2 = readOnlySpan[i + 1];
			int item3 = tuple2.Item1;
			int item4 = tuple2.Item2;
			int num = Math.Max(Math.Abs(item3 - item), Math.Abs(item4 - item2));
			for (int j = 0; j <= num; j++)
			{
				int num2 = x + item + (item3 - item) * j / Math.Max(1, num);
				int num3 = y + item2 + (item4 - item2) * j / Math.Max(1, num);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num2, num3, 1f, 1f), renderColor);
			}
		}
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x + readOnlySpan[1].Item1, y + readOnlySpan[1].Item2, 1f, 1f), renderColor2);
		Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x + readOnlySpan[2].Item1, y + readOnlySpan[2].Item2, 1f, 1f), renderColor2);
	}

	private static bool Pieds(Entity entity, out float x, out float y)
	{
		FloatRect worldPos = entity.WorldPos;
		x = worldPos.X + worldPos.Width / 2f;
		y = worldPos.Y + worldPos.Height;
		if (worldPos.Width > 0f)
		{
			return worldPos.Height > 0f;
		}
		return false;
	}

	private static void OnKiai(Entity entity, float rayon, bool feu)
	{
		if (Pieds(entity, out var x, out var y))
		{
			Kiais.Add((x, y, Timing.Global.Milliseconds, Math.Max(24f, rayon), feu));
			if (Kiais.Count > 12)
			{
				Kiais.RemoveAt(0);
			}
			Poussiere(entity, feu ? 12 : 6, feu ? 4f : 2f);
			CombatAudio.Play("strongcoup.wav", entity, 0.05f);
			Player me = Globals.Me;
			if (me != null && (entity == me || (me.WorldPos.X - x) * (me.WorldPos.X - x) + (me.WorldPos.Y - y) * (me.WorldPos.Y - y) < 25600f))
			{
				CombatFeedback.StartShake(feu ? 6f : 4f, feu ? 300 : 220);
				CombatFeedback.PunchZoom(feu ? 0.06f : 0.04f, 260L);
				CombatFeedback.HitStop(feu ? 120 : 60);
			}
		}
	}

	private static void OnOndeDeChoc(Entity entity, Direction direction)
	{
		if (!Pieds(entity, out var x, out var y))
		{
			return;
		}
		(float, float) tuple;
		switch (direction)
		{
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			tuple = (-1f, 0f);
			break;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			tuple = (1f, 0f);
			break;
		case Direction.Up:
			tuple = (0f, -1f);
			break;
		default:
			tuple = (0f, 1f);
			break;
		}
		var (num, num2) = tuple;
		if (!Options.Instance.Casse.Active)
		{
			Fissures.Add((x + num * 10f, y + num2 * 6f, num, num2 * 0.6f, Timing.Global.Milliseconds));
			if (Fissures.Count > 8)
			{
				Fissures.RemoveAt(0);
			}
		}
		Ondes.Add((x + num * 10f, y, Timing.Global.Milliseconds, 3f));
		Poussiere(entity, 10, 0f);
		CombatAudio.Play("strongcoup.wav", entity, 0.05f);
		if (entity == Globals.Me)
		{
			CombatFeedback.StartShake(4f, 200L);
			CombatFeedback.PunchZoom(0.03f, 200L);
		}
	}

	private static void OnRiposte(Entity entity, int dureeMs)
	{
		long maintenant = Timing.Global.Milliseconds;
		Ripostes[entity.Id] = (maintenant, maintenant + Math.Max(100, dureeMs));
		if (Ripostes.Count > 32)
		{
			Guid[] array = (from r in Ripostes
				where maintenant > r.Value.Fin
				select r.Key).ToArray();
			foreach (Guid key in array)
			{
				Ripostes.Remove(key);
			}
		}
		if (Pieds(entity, out var x, out var y))
		{
			Ondes.Add((x, y, maintenant, 1f));
		}
	}

	private static void DessinerLaRechargeDuDash(IGameTexture pixel)
	{
		Player me = Globals.Me;
		if (me != null && !(me.RechargeDuDash <= 0f) && me.PositionMonde(out var mondeX, out var mondeY))
		{
			mondeY += 8f;
			float num = 1f - me.RechargeDuDash;
			Color color = new Color(150, 20, 24, 36);
			Color color2 = new Color(235, 255, 154, 58);
			for (int i = 0; i < 72; i++)
			{
				float num2 = (float)i / 72f;
				float x = -(float)Math.PI / 2f + num2 * (float)Math.PI * 2f;
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(mondeX + MathF.Cos(x) * 12f), MathF.Round(mondeY + MathF.Sin(x) * 5f), 1f, 1f), (num2 <= num) ? color2 : color);
			}
		}
	}

	private static void DessinerLesTechniquesAuSol(IGameTexture pixel)
	{
		DessinerLaRechargeDuDash(pixel);
		long maintenant = Timing.Global.Milliseconds;
		Kiais.RemoveAll(((float X, float Y, long Debut, float Rayon, bool Feu) k) => maintenant - k.Debut > (k.Feu ? 640 : 420));
		foreach (var kiai in Kiais)
		{
			float item = kiai.X;
			float item2 = kiai.Y;
			long item3 = kiai.Debut;
			float item4 = kiai.Rayon;
			bool item5 = kiai.Feu;
			long num = (item5 ? 640 : 420);
			float num2 = (float)(maintenant - item3) / (float)num;
			for (int num3 = 0; num3 < (item5 ? 4 : 3); num3++)
			{
				float num4 = Math.Clamp(num2 * 1.4f - (float)num3 * 0.18f, 0f, 1f);
				if (!(num4 <= 0f))
				{
					float num5 = 8f + num4 * item4;
					float num6 = num5 * 0.5f;
					int a = (int)(230f * (1f - num4));
					Color renderColor = ((!item5) ? ((num3 == 0) ? new Color(a, 255, 255, 255) : new Color(a, 207, 244, 255)) : ((num3 == 0) ? new Color(a, 255, 255, 255) : ((num3 % 2 == 1) ? new Color(a, 255, 210, 138) : new Color(a, 255, 122, 26))));
					int num7 = 48 + (int)num5;
					for (int num8 = 0; num8 < num7; num8++)
					{
						float x = (float)num8 / (float)num7 * (float)Math.PI * 2f;
						Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(item + MathF.Cos(x) * num5), MathF.Round(item2 + MathF.Sin(x) * num6), 1f, 1f), renderColor);
					}
				}
			}
		}
		Fissures.RemoveAll(((float X, float Y, float DirX, float DirY, long Debut) f) => maintenant - f.Debut > 900);
		foreach (var fissure in Fissures)
		{
			float item6 = fissure.X;
			float item7 = fissure.Y;
			float item8 = fissure.DirX;
			float item9 = fissure.DirY;
			long item10 = fissure.Debut;
			long num9 = maintenant - item10;
			int num10 = (int)MathF.Round(72f * Math.Clamp((float)num9 / 200f, 0f, 1f));
			float num11 = ((num9 < 500) ? 1f : (1f - (float)(num9 - 500) / 400f));
			Color renderColor2 = new Color((int)(230f * num11), 36, 22, 10);
			Color color = new Color((int)(230f * num11), 201, 177, 132);
			Color color2 = new Color((int)(230f * num11), 255, 210, 138);
			for (int num12 = 0; num12 < num10; num12 += 2)
			{
				int num13 = (((num12 >> 2) % 2 == 1) ? 1 : (-1)) * (1 + ((num12 % 8 == 0) ? 1 : 0));
				float x2 = item6 + item8 * (float)num12 + (0f - item9) * (float)num13;
				float x3 = item7 + item9 * (float)num12 + item8 * (float)num13 * 0.6f;
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x2), MathF.Round(x3), 2f, 1f), renderColor2);
				Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x2), MathF.Round(x3) - 1f, 1f, 1f), (num12 % 6 == 0) ? color2 : color);
			}
		}
		if (Ripostes.Count == 0)
		{
			return;
		}
		KeyValuePair<Guid, (long, long)>[] array = Ripostes.ToArray();
		foreach (KeyValuePair<Guid, (long, long)> keyValuePair in array)
		{
			keyValuePair.Deconstruct(out var key, out var value);
			(long, long) tuple = value;
			Guid key2 = key;
			var (num15, num16) = tuple;
			float x4;
			float y;
			if (maintenant >= num16 || !Globals.Entities.TryGetValue(key2, out Entity value2) || value2 == null || value2.IsDying)
			{
				Ripostes.Remove(key2);
			}
			else if (Pieds(value2, out x4, out y))
			{
				float num17 = (float)(num16 - maintenant) / (float)Math.Max(1L, num16 - num15);
				float num18 = 0.75f + 0.25f * MathF.Sin((float)maintenant / 40f);
				for (int num19 = 0; num19 < 24; num19++)
				{
					float x5 = -(float)Math.PI / 2f + (float)num19 / 24f * (float)Math.PI * 2f;
					Color renderColor3 = (((float)num19 / 24f < num17) ? new Color((int)(255f * num18), 255, 211, 92) : new Color(120, 20, 24, 38));
					Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x4 + MathF.Cos(x5) * 12f), MathF.Round(y - 1f + MathF.Sin(x5) * 4f), 1f, 1f), renderColor3);
				}
			}
		}
	}
}
