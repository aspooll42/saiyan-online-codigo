using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Config;
using Intersect.Framework.Core;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities;

public static class Maitres
{
	private sealed class Charge
	{
		public TechniqueDeMaitre Technique;

		public long Debut;

		public float Angle;

		public int DureeMs;

		public MapSound? Son;
	}

	private sealed class Disque
	{
		public float X;

		public float Y;

		public float Cos;

		public float Sin;

		public float Longueur;

		public float Sol;

		public long Depart;

		public bool Arrete;
	}

	private sealed class Tir
	{
		public float X;

		public float Y;

		public float Cos;

		public float Sin;

		public float Longueur;

		public long Depart;

		public bool Arrete;
	}

	private sealed class Eclair
	{
		public float X;

		public float Y;

		public long Depart;

		public int Rayon;
	}

	private sealed class PoseApres
	{
		public TechniqueDeMaitre Technique;

		public float Angle;

		public long Depart;

		public long Fin;
	}

	private const int Ecoute = 12;

	private const string SonDeCharge = "kamehameha_charge.wav";

	private const string SonDuLancer = "ki_depart.wav";

	private const string SonDuTir = "ki_fatal.wav";

	private const string SonDeLEclair = "eclair.wav";

	private static readonly Color Blanc = new Color(255, 255, 255, 255);

	private static readonly Color Creme = new Color(255, 255, 248, 222);

	private static readonly Color OrPale = new Color(255, 255, 222, 140);

	private static readonly Color Jaune = new Color(255, 255, 222, 70);

	private static readonly Color Clair = new Color(255, 255, 246, 176);

	private static readonly Color CerneDisque = new Color(255, 92, 58, 0);

	private static readonly Color Violet = new Color(255, 176, 104, 255);

	private static readonly Color VioletSombre = new Color(255, 70, 24, 128);

	private static readonly Color JauneSpirale = new Color(255, 255, 226, 92);

	private static readonly Color CoeurSpirale = new Color(255, 255, 252, 226);

	private static readonly Dictionary<Guid, Charge> Charges = new Dictionary<Guid, Charge>();

	private static readonly Dictionary<Guid, PoseApres> Poses = new Dictionary<Guid, PoseApres>();

	private static readonly Dictionary<Guid, long> Aveugles = new Dictionary<Guid, long>();

	private static readonly List<Disque> Disques = new List<Disque>();

	private static readonly List<Tir> Tirs = new List<Tir>();

	private static readonly List<Eclair> Eclairs = new List<Eclair>();

	private static readonly List<(float X, float Y, float Cos, float Sin, long A)> Etincelles = new List<(float, float, float, float, long)>();

	private static MaitresOptions Reglages => Options.Instance.Maitres;

	public static bool IsActive(Entity entity)
	{
		if (!Charges.ContainsKey(entity.Id))
		{
			if (Poses.TryGetValue(entity.Id, out PoseApres value))
			{
				return value.Fin > Timing.Global.Milliseconds;
			}
			return false;
		}
		return true;
	}

	public static TechniqueDeMaitre ChargeDe(Entity entity)
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value))
		{
			return TechniqueDeMaitre.Aucune;
		}
		return value.Technique;
	}

	public static long ChargeDepuis(Entity entity)
	{
		if (!Charges.TryGetValue(entity.Id, out Charge value))
		{
			return 0L;
		}
		return Timing.Global.Milliseconds - value.Debut;
	}

	public static void CommencerLocal(Entity entity, TechniqueDeMaitre technique, float angle)
	{
		Commencer(entity, technique, angle, (technique == TechniqueDeMaitre.Kienzan) ? Reglages.KienzanFormationMs : Reglages.MakankoChargeMs);
	}

	public static void Viser(Entity entity, float angle)
	{
		if (Charges.TryGetValue(entity.Id, out Charge value) && float.IsFinite(angle))
		{
			value.Angle = angle;
		}
	}

	public static void AnnulerLocal(Entity entity)
	{
		Lacher(entity.Id);
	}

	private static void Commencer(Entity entity, TechniqueDeMaitre technique, float angle, int dureeMs)
	{
		if (Charges.TryGetValue(entity.Id, out Charge value) && value.Technique == technique)
		{
			value.DureeMs = dureeMs;
			return;
		}
		Lacher(entity.Id);
		Charges[entity.Id] = new Charge
		{
			Technique = technique,
			Debut = Timing.Global.Milliseconds,
			Angle = angle,
			DureeMs = dureeMs,
			Son = Audio.AddMapSound("kamehameha_charge.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity)
		};
	}

	private static void Lacher(Guid entityId)
	{
		if (Charges.Remove(entityId, out Charge value))
		{
			value.Son?.Stop();
		}
	}

	public static bool OnEchange(EchangePacket packet)
	{
		EchangeEvent echangeEvent = packet.Event;
		if (echangeEvent - 23 > EchangeEvent.Avantage)
		{
			return false;
		}
		if (!Globals.Entities.TryGetValue(packet.EntityId, out Entity value) || value == null || value.IsDisposed)
		{
			return true;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = MaitresOptions.AngleDepuisCentiemes(packet.Valeur);
		switch (packet.Event)
		{
		case EchangeEvent.Eblouissement:
		{
			FloatRect worldPos2 = value.WorldPos;
			Eclairs.Add(new Eclair
			{
				X = worldPos2.X + worldPos2.Width / 2f,
				Y = worldPos2.Bottom - 2f,
				Depart = milliseconds,
				Rayon = Math.Max(16, packet.Valeur)
			});
			Poses[value.Id] = new PoseApres
			{
				Technique = TechniqueDeMaitre.Eblouissement,
				Depart = milliseconds,
				Fin = milliseconds + 520
			};
			Audio.AddMapSound("eclair.wav", value.X, value.Y, value.MapId, loop: false, 0, 12, value);
			CombatFeedback.Callout(value.Id, "TAIYOKEN !", new Color(255, 255, 251, 232), 22);
			return true;
		}
		case EchangeEvent.Aveugle:
			Aveugles[value.Id] = milliseconds + Math.Max(0, packet.DureeMs);
			if (value == Globals.Me)
			{
				Globals.Me.ClearTarget();
			}
			return true;
		case EchangeEvent.KienzanForme:
			Commencer(value, TechniqueDeMaitre.Kienzan, num, packet.DureeMs);
			return true;
		case EchangeEvent.MakankoCharge:
			Commencer(value, TechniqueDeMaitre.Makankosappo, num, packet.DureeMs);
			return true;
		case EchangeEvent.Kienzan:
		{
			Lacher(value.Id);
			Poses[value.Id] = new PoseApres
			{
				Technique = TechniqueDeMaitre.Kienzan,
				Angle = num,
				Depart = milliseconds,
				Fin = milliseconds + Reglages.KienzanLancerMs
			};
			(float Cos, float Sin) tuple3 = Axe(num);
			float item = tuple3.Cos;
			float item2 = tuple3.Sin;
			(float X, float Y) tuple4 = Main(value, num);
			float item3 = tuple4.X;
			float item4 = tuple4.Y;
			FloatRect worldPos = value.WorldPos;
			Disques.Add(new Disque
			{
				X = item3,
				Y = item4,
				Cos = item,
				Sin = item2,
				Longueur = LongueurDepuisLaMain(worldPos, item3, item4, item, item2, packet.DureeMs),
				Sol = worldPos.Bottom - 2f - item4,
				Depart = milliseconds,
				Arrete = (packet.Index == 1)
			});
			Audio.AddMapSound("ki_depart.wav", value.X, value.Y, value.MapId, loop: false, 0, 12, value);
			CombatFeedback.Callout(value.Id, "KIENZAN !", new Color(255, 255, 246, 176), 22);
			return true;
		}
		case EchangeEvent.Makankosappo:
			Lacher(value.Id);
			Poses[value.Id] = new PoseApres
			{
				Technique = TechniqueDeMaitre.Makankosappo,
				Angle = num,
				Depart = milliseconds,
				Fin = milliseconds + Reglages.MakankoTenueMs
			};
			var (num2, num3) = Axe(num);
			var (num4, num5) = Main(value, num);
			Tirs.Add(new Tir
			{
				X = num4,
				Y = num5,
				Cos = num2,
				Sin = num3,
				Longueur = LongueurDepuisLaMain(value.WorldPos, num4, num5, num2, num3, packet.DureeMs),
				Depart = milliseconds,
				Arrete = (packet.Index == 1)
			});
			Audio.AddMapSound("ki_fatal.wav", value.X, value.Y, value.MapId, loop: false, 0, 12, value);
			CombatFeedback.Callout(value.Id, "MAKANKOSAPPO !", new Color(255, 243, 232, 255), 22);
			CombatFeedback.StartShake(2f, 160L, num2, num3);
			return true;
		case EchangeEvent.AttaqueAnnulee:
			Lacher(value.Id);
			return true;
		default:
			return true;
		}
	}

	private static (float Cos, float Sin) Axe(float degres)
	{
		float x = degres * (float)Math.PI / 180f;
		return (Cos: MathF.Cos(x), Sin: MathF.Sin(x));
	}

	private static (float X, float Y) Main(Entity entity, float degres)
	{
		FloatRect worldPos = entity.WorldPos;
		(float Cos, float Sin) tuple = Axe(degres);
		float item = tuple.Cos;
		float item2 = tuple.Sin;
		float num = 15.5f + item * 13.5f;
		float num2 = 31f + item2 * ((item2 > 0f) ? 7f : 17f);
		return (X: MathF.Round(worldPos.X + num * worldPos.Width / 32f), Y: MathF.Round(worldPos.Y + num2 * worldPos.Height / 48f));
	}

	private static (float X, float Y) MainLevee(Entity entity, float degres)
	{
		FloatRect worldPos = entity.WorldPos;
		float num = Rangee(degres) switch
		{
			0 => 1f, 
			1 => 4f, 
			2 => -6f, 
			_ => 0f, 
		};
		return (X: MathF.Round(worldPos.X + worldPos.Width / 2f + num), Y: MathF.Round(worldPos.Y + 9f * worldPos.Height / 48f));
	}

	private static float LongueurDepuisLaMain(FloatRect cadre, float mx, float my, float cos, float sin, int distance)
	{
		float num = cadre.X + cadre.Width / 2f;
		float num2 = cadre.Bottom - (float)Options.Instance.Map.TileHeight / 2f;
		return MathF.Max(0f, (num - mx) * cos + (num2 - my) * sin + (float)distance);
	}

	private static int Rangee(float degres)
	{
		var (num, num2) = Axe(degres);
		if (!(MathF.Abs(num) >= MathF.Abs(num2)))
		{
			if (!(num2 > 0f))
			{
				return 3;
			}
			return 0;
		}
		if (!(num > 0f))
		{
			return 1;
		}
		return 2;
	}

	public static bool TryPose(Entity entity, int row, ref IGameTexture texture, ref FloatRect source)
	{
		long milliseconds = Timing.Global.Milliseconds;
		string text;
		float degres;
		int value2;
		if (Charges.TryGetValue(entity.Id, out Charge value))
		{
			if (value.Technique != TechniqueDeMaitre.Kienzan)
			{
				text = "_spell.png";
				value2 = 0;
			}
			else
			{
				text = "_vol.png";
				value2 = 3;
			}
			degres = value.Angle;
		}
		else
		{
			if (!Poses.TryGetValue(entity.Id, out PoseApres value3) || value3.Fin <= milliseconds)
			{
				return false;
			}
			if (value3.Technique == TechniqueDeMaitre.Eblouissement)
			{
				value2 = (int)((milliseconds - value3.Depart) / 90 % 4);
				text = "_ki.png";
				degres = 90f;
			}
			else
			{
				text = "_spell.png";
				value2 = 0;
				degres = value3.Angle;
			}
		}
		string text2 = (string.IsNullOrWhiteSpace(entity.TransformedSprite) ? entity.Sprite : Transformation.TextureNameFor(entity.Sprite, entity.TransformedSprite));
		if (!string.IsNullOrWhiteSpace(text2))
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(text2) + text);
			if (texture2 != null)
			{
				int num = texture2.Height / Math.Max(1, Options.Instance.Sprites.Directions);
				int num2 = Math.Max(1, num * 2 / 3);
				int num3 = Math.Max(1, texture2.Width / num2);
				value2 = Math.Clamp(value2, 0, num3 - 1);
				texture = texture2;
				source = new FloatRect(value2 * num2, Rangee(degres) * num, num2, num);
				return true;
			}
		}
		return false;
	}

	public static FloatRect Pose(Entity entity, FloatRect rectangle)
	{
		if (!Poses.TryGetValue(entity.Id, out PoseApres value) || value.Technique != TechniqueDeMaitre.Makankosappo)
		{
			return rectangle;
		}
		long num = Timing.Global.Milliseconds - value.Depart;
		int num2 = Math.Max(1, Reglages.MakankoTenueMs);
		if (num < 0 || num > num2 + 200)
		{
			return rectangle;
		}
		float value2 = ((num < 90) ? ((float)num / 90f) : ((num < num2) ? 1f : (1f - (float)(num - num2) / 200f)));
		float num3 = MathF.Round((float)Reglages.MakankoReculPx * Math.Clamp(value2, 0f, 1f));
		var (num4, num5) = Axe(value.Angle);
		return new FloatRect(rectangle.X - MathF.Round(num4 * num3), rectangle.Y - MathF.Round(num5 * num3 * 0.6f), rectangle.Width, rectangle.Height);
	}

	public static void DrawGround()
	{
		if (Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Nettoyer(milliseconds);
		foreach (Eclair eclair in Eclairs)
		{
			DessinerLEtoile(eclair, milliseconds);
		}
		foreach (Disque disque in Disques)
		{
			var (num, num2, num3) = OuEstLeDisque(disque, milliseconds);
			if (num3 > 0f)
			{
				Rect(MathF.Round(num - 6f), MathF.Round(num2 + disque.Sol), 13f, 1f, new Color((int)(70f * num3), 20, 16, 6));
			}
		}
		foreach (var (key, charge2) in Charges)
		{
			if (charge2.Technique != TechniqueDeMaitre.Makankosappo || !Globals.Entities.TryGetValue(key, out Entity value) || value == null)
			{
				continue;
			}
			FloatRect worldPos = value.WorldPos;
			long num4 = milliseconds - charge2.Debut;
			bool flag = num4 >= charge2.DureeMs;
			float num5 = ((charge2.DureeMs <= 0) ? 1f : Math.Clamp((float)num4 / (float)charge2.DureeMs, 0f, 1f));
			float num6 = worldPos.X + worldPos.Width / 2f;
			float num7 = worldPos.Bottom - 1f;
			for (int i = 0; i < 44; i++)
			{
				float x = (float)i / 44f * (float)Math.PI * 2f;
				bool flag2 = (float)i / 44f <= num5;
				Rect(MathF.Round(num6 + MathF.Cos(x) * 15f), MathF.Round(num7 + MathF.Sin(x) * 5f), 1f, 1f, flag2 ? (flag ? CoeurSpirale : Violet) : new Color(120, 30, 12, 60));
			}
			if (value != Globals.Me)
			{
				continue;
			}
			(float Cos, float Sin) tuple2 = Axe(charge2.Angle);
			float item = tuple2.Cos;
			float item2 = tuple2.Sin;
			(float X, float Y) tuple3 = Main(value, charge2.Angle);
			float item3 = tuple3.X;
			float item4 = tuple3.Y;
			int num8 = (int)(milliseconds / 45 % 8);
			for (int j = 10; j < Reglages.MakankoPorteePx; j++)
			{
				if ((j + num8) % 8 < 3)
				{
					Rect(MathF.Round(item3 + item * (float)j), MathF.Round(item4 + item2 * (float)j), 1f, 1f, (j % 2 == 0) ? new Color(200, 176, 104, 255) : new Color(200, 255, 246, 176));
				}
			}
		}
	}

	public static void Draw()
	{
		if (Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Guid key;
		foreach (KeyValuePair<Guid, Charge> charge2 in Charges)
		{
			charge2.Deconstruct(out key, out var value);
			Guid key2 = key;
			Charge charge = value;
			if (!Globals.Entities.TryGetValue(key2, out Entity value2) || value2 == null)
			{
				continue;
			}
			if (charge.Technique == TechniqueDeMaitre.Kienzan)
			{
				float num = ((charge.DureeMs <= 0) ? 1f : Math.Clamp((float)(milliseconds - charge.Debut) / (float)charge.DureeMs, 0f, 1f));
				var (num2, num3) = MainLevee(value2, charge.Angle);
				DessinerLeDisque(num2, num3 - 2f * num, 2f + 7.5f * num, 0.8f + 2f * num, milliseconds, 1f);
				if (num < 1f)
				{
					for (int i = 0; i < 6; i++)
					{
						float x = Rand(i, (int)(milliseconds / 70)) * (float)Math.PI * 2f;
						float num4 = 12f - ((float)milliseconds / 8f + (float)i * 9f) % 12f;
						Rect(MathF.Round(num2 + MathF.Cos(x) * num4), MathF.Round(num3 + MathF.Sin(x) * num4 * 0.5f), 1f, 1f, (i % 2 == 0) ? Clair : Jaune);
					}
				}
			}
			else
			{
				DessinerLesEclairsDeLaCharge(value2, charge, milliseconds);
			}
		}
		foreach (Disque disque in Disques)
		{
			var (num5, num6, num7) = OuEstLeDisque(disque, milliseconds);
			if (!(num7 <= 0f))
			{
				for (int j = 1; j <= 5; j++)
				{
					Rect(MathF.Round(num5 - disque.Cos * (float)j * 2f), MathF.Round(num6 - disque.Sin * (float)j * 2f), 1f, 1f, new Color((int)(140f * num7 * (1f - (float)j / 6f)), 255, 222, 70));
				}
				DessinerLeDisque(num5, num6, 9.5f, 2.8f, milliseconds, num7);
			}
		}
		foreach (Tir tir in Tirs)
		{
			DessinerLeTir(tir, milliseconds);
		}
		foreach (var etincelle in Etincelles)
		{
			float item = etincelle.X;
			float item2 = etincelle.Y;
			float item3 = etincelle.Cos;
			float item4 = etincelle.Sin;
			long item5 = etincelle.A;
			long num8 = milliseconds - item5;
			if (num8 >= 0 && num8 <= 300)
			{
				for (int k = 0; k < 8; k++)
				{
					float x2 = MathF.Atan2(0f - item4, 0f - item3) + (Rand(k, 4) - 0.5f) * 2.4f;
					float num9 = 3f + (float)num8 / 300f * 14f * Rand(4, k);
					Rect(MathF.Round(item + MathF.Cos(x2) * num9), MathF.Round(item2 + MathF.Sin(x2) * num9), 1f, 1f, (k % 2 == 0) ? Clair : Jaune);
				}
			}
		}
		foreach (KeyValuePair<Guid, long> aveugle in Aveugles)
		{
			aveugle.Deconstruct(out key, out var value3);
			Guid key3 = key;
			long num10 = value3;
			if (num10 > milliseconds && Globals.Entities.TryGetValue(key3, out Entity value4) && value4 != null)
			{
				FloatRect worldPos = value4.WorldPos;
				float num11 = worldPos.X + worldPos.Width / 2f;
				float num12 = worldPos.Y + worldPos.Height * 0.36f;
				int num13 = (int)(milliseconds / 80);
				for (int l = 0; l < 4; l++)
				{
					float x3 = (float)l / 4f * (float)Math.PI + (Rand(l, num13) - 0.5f) * 0.5f;
					float num14 = 6f + Rand(num13, l) * 3f;
					Trait(num11 + MathF.Cos(x3) * 3f, num12 + MathF.Sin(x3) * 1.8f, num11 + MathF.Cos(x3) * num14, num12 + MathF.Sin(x3) * num14 * 0.6f, (l % 2 == 0) ? Blanc : Creme);
					Trait(num11 - MathF.Cos(x3) * 3f, num12 - MathF.Sin(x3) * 1.8f, num11 - MathF.Cos(x3) * num14, num12 - MathF.Sin(x3) * num14 * 0.6f, (l % 2 == 0) ? Creme : Blanc);
				}
			}
		}
	}

	public static void DrawAir()
	{
		if (Graphics.Renderer?.WhitePixel == null || Eclairs.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = 0f;
		foreach (Eclair eclair in Eclairs)
		{
			long num2 = milliseconds - eclair.Depart;
			if (num2 >= 0 && num2 < 260)
			{
				num = MathF.Max(num, (num2 < 80) ? 0.85f : (0.85f * (1f - (float)(num2 - 80) / 180f)));
			}
		}
		if (num > 0f)
		{
			FloatRect currentView = Graphics.CurrentView;
			Rect(currentView.X - 4f, currentView.Y - 4f, currentView.Width + 8f, currentView.Height + 8f, new Color((int)(255f * num), 255, 250, 232));
		}
	}

	private static (float X, float Y, float Force) OuEstLeDisque(Disque disque, long maintenant)
	{
		float num = (float)((maintenant - disque.Depart) * Reglages.KienzanVitessePx) / 1000f;
		bool flag = num >= disque.Longueur;
		float num2 = MathF.Min(num, disque.Longueur);
		if (!flag)
		{
			return (X: disque.X + disque.Cos * num2, Y: disque.Y + disque.Sin * num2, Force: 1f);
		}
		float num3 = (num - disque.Longueur) / (float)Math.Max(1, Reglages.KienzanVitessePx) * 1000f;
		return (X: disque.X + disque.Cos * num2, Y: disque.Y + disque.Sin * num2, Force: MathF.Max(0f, 1f - num3 / 150f));
	}

	private static void DessinerLeDisque(float x, float y, float rx, float ry, long maintenant, float force)
	{
		if (!(force <= 0f) && !(rx < 1f))
		{
			Ellipse(x, y, rx + 1.5f, ry + 1f, Avec(CerneDisque));
			Ellipse(x, y, rx, ry, Avec(Jaune));
			Ellipse(x, y, rx * 0.72f, ry * 0.65f, Avec(Clair));
			Rect(MathF.Round(x - rx * 0.35f), MathF.Round(y), MathF.Max(1f, MathF.Round(rx * 0.7f)), 1f, Avec(Blanc));
			float num = (float)maintenant * 0.025f;
			for (int i = 0; i < 14; i++)
			{
				float x2 = (float)i / 14f * (float)Math.PI * 2f + num;
				Rect(MathF.Round(x + MathF.Cos(x2) * (rx + 1.5f)), MathF.Round(y + MathF.Sin(x2) * (ry + 1f)), 1f, 1f, Avec((i % 2 == 0) ? Clair : Jaune));
			}
		}
		Color Avec(Color c)
		{
			return new Color((int)((float)(int)c.A * force), c.R, c.G, c.B);
		}
	}

	private static void DessinerLesEclairsDeLaCharge(Entity entity, Charge charge, long maintenant)
	{
		(float X, float Y) tuple = Main(entity, charge.Angle);
		float item = tuple.X;
		float item2 = tuple.Y;
		long num = maintenant - charge.Debut;
		bool flag = num >= charge.DureeMs;
		int num2 = (int)(maintenant / 55);
		for (int i = 0; i < (flag ? 5 : 3); i++)
		{
			float num3 = item;
			float num4 = item2;
			float num5 = Rand(i, num2) * (float)Math.PI * 2f;
			for (int j = 0; j < 3; j++)
			{
				float x = num5 + (Rand(j + i * 7, num2) - 0.5f) * 1.8f;
				float num6 = 2f + Rand(i, j + num2) * 3f;
				float num7 = num3 + MathF.Cos(x) * num6;
				float num8 = num4 + MathF.Sin(x) * num6;
				Trait(num3, num4, num7, num8, (j % 2 == 0) ? JauneSpirale : Violet);
				num3 = num7;
				num4 = num8;
			}
		}
		float num9 = ((charge.DureeMs <= 0) ? 1f : Math.Clamp((float)num / (float)charge.DureeMs, 0f, 1f));
		float num10 = 1f + num9 * 2f + (flag ? (MathF.Sin((float)maintenant / 40f) * 0.6f) : 0f);
		Disc(item, item2, num10 + 1f, VioletSombre);
		Disc(item, item2, num10, JauneSpirale);
		Disc(item, item2, num10 * 0.5f, CoeurSpirale);
	}

	private static void DessinerLeTir(Tir tir, long maintenant)
	{
		long num = maintenant - tir.Depart;
		int num2 = Math.Max(1, Reglages.MakankoTenueMs);
		if (num < 0 || num > num2 + 240)
		{
			return;
		}
		float f = ((num < num2) ? 1f : (1f - (float)(num - num2) / 240f));
		float num3 = Math.Min(1f, (float)num / 60f) * tir.Longueur;
		if (num3 < 1f)
		{
			return;
		}
		float degrees = MathF.Atan2(tir.Sin, tir.Cos) * 180f / (float)Math.PI;
		float cx = tir.X + tir.Cos * num3 / 2f;
		float cy = tir.Y + tir.Sin * num3 / 2f;
		RotatedRect(cx, cy, MathF.Round(num3), 4f, degrees, Avec(VioletSombre));
		RotatedRect(cx, cy, MathF.Round(num3), 2f, degrees, Avec(JauneSpirale));
		RotatedRect(cx, cy, MathF.Round(num3), 1f, degrees, Avec(CoeurSpirale));
		float num4 = 0f - tir.Sin;
		float cos = tir.Cos;
		for (float num5 = 0f; num5 <= num3; num5 += 1f)
		{
			float num6 = tir.X + tir.Cos * num5;
			float num7 = tir.Y + tir.Sin * num5;
			float num8 = num5 * 0.34f - (float)num * 0.05f;
			for (int i = 0; i < 2; i++)
			{
				float x = num8 + (float)i * (float)Math.PI;
				float num9 = MathF.Sin(x) * 4.6f;
				bool flag = MathF.Cos(x) > 0f;
				if (flag || MathF.Abs(num9) > 2.2f)
				{
					Rect(MathF.Round(num6 + num4 * num9), MathF.Round(num7 + cos * num9), 1f, 1f, Avec((!flag) ? VioletSombre : ((i == 0) ? Violet : JauneSpirale)));
				}
			}
		}
		if (num < 120)
		{
			float num10 = 3f + (1f - (float)num / 120f) * 3f;
			Disc(tir.X, tir.Y, num10 + 1f, Avec(VioletSombre));
			Disc(tir.X, tir.Y, num10, Avec(JauneSpirale));
			Disc(tir.X, tir.Y, num10 * 0.5f, Avec(CoeurSpirale));
		}
		float cx2 = tir.X + tir.Cos * num3;
		float cy2 = tir.Y + tir.Sin * num3;
		Disc(cx2, cy2, 2.5f, Avec(Violet));
		Disc(cx2, cy2, 1.4f, Avec(CoeurSpirale));
		Color Avec(Color c)
		{
			return new Color((int)((float)(int)c.A * f), c.R, c.G, c.B);
		}
	}

	private static void DessinerLEtoile(Eclair eclair, long maintenant)
	{
		long num = maintenant - eclair.Depart;
		if (num < 0)
		{
			return;
		}
		float num2 = Math.Min(1f, (float)num / 110f);
		float num3 = Math.Max(0f, 1f - Math.Max(0f, (float)num - 260f) / 520f);
		if (!(num3 <= 0f))
		{
			for (int i = 0; i < 12; i++)
			{
				float x = (float)i / 12f * (float)Math.PI * 2f + (Rand(i, 5) - 0.5f) * 0.25f;
				float num4 = (float)eclair.Rayon * ((i % 2 == 0) ? 1f : 0.65f) * (0.85f + Rand(5, i) * 0.15f) * num2;
				float num5 = ((i % 2 == 0) ? 6f : 4f);
				float num6 = MathF.Cos(x);
				float num7 = MathF.Sin(x) * 0.45f;
				float num8 = 0f - MathF.Sin(x);
				float num9 = MathF.Cos(x) * 0.45f;
				float num10 = eclair.X + num6 * 6f;
				float num11 = eclair.Y + num7 * 6f;
				FillTriangle(num10 + num8 * num5, num11 + num9 * num5, eclair.X + num6 * num4, eclair.Y + num7 * num4, num10 - num8 * num5, num11 - num9 * num5, new Color((int)(150f * num3), 255, 248, 222));
				FillTriangle(num10 + num8 * num5 * 0.5f, num11 + num9 * num5 * 0.5f, eclair.X + num6 * num4 * 0.6f, eclair.Y + num7 * num4 * 0.6f, num10 - num8 * num5 * 0.5f, num11 - num9 * num5 * 0.5f, new Color((int)(210f * num3), 255, 255, 255));
				Rect(MathF.Round(eclair.X + num6 * num4), MathF.Round(eclair.Y + num7 * num4), 1f, 1f, new Color((int)(200f * num3), OrPale.R, OrPale.G, OrPale.B));
			}
		}
	}

	private static void Nettoyer(long maintenant)
	{
		foreach (Disque disque in Disques)
		{
			float item = OuEstLeDisque(disque, maintenant).Force;
			if (item <= 0f && disque.Arrete)
			{
				disque.Arrete = false;
				Etincelles.Add((disque.X + disque.Cos * disque.Longueur, disque.Y + disque.Sin * disque.Longueur, disque.Cos, disque.Sin, maintenant));
			}
		}
		Disques.RemoveAll((Disque d) => OuEstLeDisque(d, maintenant).Force <= 0f && !d.Arrete);
		foreach (Tir item2 in Tirs.Where((Tir t) => t.Arrete && maintenant - t.Depart >= 60))
		{
			item2.Arrete = false;
			Etincelles.Add((item2.X + item2.Cos * item2.Longueur, item2.Y + item2.Sin * item2.Longueur, item2.Cos, item2.Sin, maintenant));
		}
		Tirs.RemoveAll((Tir t) => maintenant - t.Depart > Reglages.MakankoTenueMs + 260);
		Eclairs.RemoveAll((Eclair e) => maintenant - e.Depart > 900);
		Etincelles.RemoveAll(((float X, float Y, float Cos, float Sin, long A) e) => maintenant - e.A > 320);
		foreach (Guid item3 in (from a in Aveugles
			where a.Value <= maintenant
			select a.Key).ToList())
		{
			Aveugles.Remove(item3);
		}
		foreach (Guid item4 in (from p in Poses
			where p.Value.Fin + 400 <= maintenant
			select p.Key).ToList())
		{
			Poses.Remove(item4);
		}
		Entity value;
		foreach (Guid item5 in Charges.Keys.Where((Guid id) => !Globals.Entities.TryGetValue(id, out value) || value == null || value.IsDisposed).ToList())
		{
			Lacher(item5);
		}
	}

	private static void Rect(float x, float y, float width, float height, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, height), color);
			}
		}
	}

	private static void RotatedRect(float cx, float cy, float width, float height, float degrees, Color color)
	{
		if (!(width <= 0f) && !(height <= 0f) && color.A > 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(cx - width / 2f, cy - height / 2f, width, height), color, null, GameBlendModes.None, null, degrees);
			}
		}
	}

	private static void Ellipse(float cx, float cy, float rx, float ry, Color color)
	{
		int num = (int)MathF.Ceiling(ry);
		for (int i = -num; i <= num; i++)
		{
			float num2 = 1f - (float)(i * i) / MathF.Max(0.01f, ry * ry);
			if (!(num2 < 0f))
			{
				float num3 = MathF.Floor(rx * MathF.Sqrt(num2) + 0.35f);
				Rect(MathF.Round(cx) - num3, MathF.Round(cy) + (float)i, num3 * 2f + 1f, 1f, color);
			}
		}
	}

	private static void Disc(float cx, float cy, float radius, Color color)
	{
		Ellipse(cx, cy, radius, radius, color);
	}

	private static void Trait(float x0, float y0, float x1, float y1, Color color)
	{
		int num = Math.Max(1, (int)MathF.Ceiling(MathF.Max(MathF.Abs(x1 - x0), MathF.Abs(y1 - y0))));
		for (int i = 0; i <= num; i++)
		{
			Rect(MathF.Round(x0 + (x1 - x0) * (float)i / (float)num), MathF.Round(y0 + (y1 - y0) * (float)i / (float)num), 1f, 1f, color);
		}
	}

	private static void FillTriangle(float ax, float ay, float bx, float by, float cx, float cy, Color color)
	{
		int num = (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy)));
		int num2 = (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy)));
		for (int i = num; i <= num2; i++)
		{
			float y = (float)i + 0.5f;
			float gauche = float.MaxValue;
			float droite = float.MinValue;
			Croiser(ax, ay, bx, by, y, ref gauche, ref droite);
			Croiser(bx, by, cx, cy, y, ref gauche, ref droite);
			Croiser(cx, cy, ax, ay, y, ref gauche, ref droite);
			if (droite >= gauche)
			{
				float num3 = MathF.Round(gauche);
				Rect(num3, i, MathF.Max(1f, MathF.Round(droite) - num3), 1f, color);
			}
		}
	}

	private static void Croiser(float ax, float ay, float bx, float by, float y, ref float gauche, ref float droite)
	{
		if ((ay <= y && by > y) || (by <= y && ay > y))
		{
			float y2 = ax + (y - ay) * (bx - ax) / (by - ay);
			gauche = MathF.Min(gauche, y2);
			droite = MathF.Max(droite, y2);
		}
	}

	private static uint Hache(int a, int b)
	{
		uint num = (uint)(a * 374761393 + b * 668265263);
		num = (num ^ (num >> 13)) * 1274126177;
		return num ^ (num >> 16);
	}

	private static float Rand(int a, int b)
	{
		return (float)(Hache(a, b) % 10000) / 10000f;
	}
}
