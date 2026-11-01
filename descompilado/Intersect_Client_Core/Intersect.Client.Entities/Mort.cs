using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class Mort
{
	private sealed class Etat
	{
		public long Debut;

		public int Rangee = -1;

		public Guid Carte;

		public int X;

		public int Y;

		public bool VieNulle;

		public long RetourA;

		public bool SolJoue;

		public bool AureoleJouee;

		public bool Ecrite;

		public bool Tenue;

		public long FlaqueDebut;
	}

	private const long ArretMs = 110L;

	private const long RalentiMs = 1030L;

	private const float Ralenti = 0.35f;

	private const long ReculFinMs = 290L;

	private const float Recul = 3f;

	private const long ATerreMs = 290L;

	private const long APlatMs = 570L;

	private const long SouffleMs = 1140L;

	private const long GrisDeMs = 540L;

	private const long GrisAMs = 1640L;

	private const long AureoleMs = 1640L;

	private const long AmeDeMs = 2040L;

	private const long AmeMonteMs = 2140L;

	private const long AmeFinMs = 3640L;

	private const float Montee = 34f;

	private const long BlancDeMs = 3140L;

	private const long BlancAMs = 3840L;

	private const long BlancTenuMs = 250L;

	private const long BlancLeveMs = 700L;

	private const long SansRetourMs = 6000L;

	private const long CorpsDeMs = 3640L;

	private const long CorpsAMs = 4140L;

	private const long FinAutreMs = 4600L;

	private const long TenueMs = 700L;

	private const long TenueMaxMs = 120000L;

	private static readonly Dictionary<Guid, Etat> Etats = new Dictionary<Guid, Etat>();

	private static IGameTexture? _aureole;

	private static bool _aureoleCherchee;

	private static readonly Dictionary<(string, int, int, int, int), (int Y, float X)> Sommets = new Dictionary<(string, int, int, int, int), (int, float)>();

	private static bool Oubliee(Entity entity, Etat etat, long maintenant)
	{
		if (!etat.Tenue || (!Repose(entity, etat) && maintenant - etat.Debut < 120000))
		{
			return false;
		}
		Terminer(entity);
		return true;
	}

	private static long Age(Etat etat, long maintenant)
	{
		if (!etat.Tenue)
		{
			return maintenant - etat.Debut;
		}
		return Math.Min(maintenant - etat.Debut, 700L);
	}

	public static void Commencer(Entity entity, byte etape = 0)
	{
		if (!(entity is Player) || entity.IsDisposed)
		{
			return;
		}
		if (etape == 2)
		{
			if (Etats.TryGetValue(entity.Id, out Etat value) && value.Tenue)
			{
				value.Debut = Timing.Global.Milliseconds - 700;
				value.Tenue = false;
			}
			return;
		}
		Nettoyer();
		long milliseconds = Timing.Global.Milliseconds;
		Etats[entity.Id] = new Etat
		{
			Debut = milliseconds,
			Carte = entity.MapId,
			X = entity.X,
			Y = entity.Y,
			VieNulle = (entity.Vital[0] <= 0),
			Ecrite = (etape == 1),
			Tenue = (etape == 1)
		};
		entity.DyingAt = milliseconds;
		entity.DeathDurationMs = 4200L;
		if (entity == Globals.Me)
		{
			CombatFeedback.HitStop(110L);
			CombatFeedback.SlowMotion(1140L, 0.35f);
			CombatFeedback.StartShake(3f, 320L);
			Audio.AddGameSound("mort_coup", loop: false);
		}
		else
		{
			Audio.AddMapSound("mort_coup.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 12, entity);
		}
	}

	public static bool Gisant(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value))
		{
			return !Revenu(entity, value, Timing.Global.Milliseconds);
		}
		return false;
	}

	public static bool AGenoux(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value) && !Revenu(entity, value, Timing.Global.Milliseconds))
		{
			long num = Age(value, Timing.Global.Milliseconds);
			if (num >= 290)
			{
				return num < 570;
			}
			return false;
		}
		return false;
	}

	public static bool Couche(Entity entity)
	{
		if (Etats.Count > 0 && Etats.TryGetValue(entity.Id, out Etat value) && !Revenu(entity, value, Timing.Global.Milliseconds) && Age(value, Timing.Global.Milliseconds) >= 290)
		{
			return PlancheDeChute(entity) != null;
		}
		return false;
	}

	public static int Blanc(Entity entity)
	{
		if (!Etats.TryGetValue(entity.Id, out Etat value))
		{
			return 0;
		}
		long num = Age(value, Timing.Global.Milliseconds);
		return (int)(255f * (1f - Lisse(60f, 300f, num)));
	}

	public static bool TryPose(Entity entity, int row, ref IGameTexture texture, ref FloatRect source)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value))
		{
			return false;
		}
		if (entity != Globals.Me && !entity.IsDying)
		{
			Terminer(entity);
			return false;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (Oubliee(entity, value, milliseconds))
		{
			return false;
		}
		if (value.Rangee < 0)
		{
			value.Rangee = row;
		}
		if (Revenu(entity, value, milliseconds))
		{
			return false;
		}
		long num = Age(value, milliseconds);
		if (!value.SolJoue && num >= 570)
		{
			value.SolJoue = true;
			Chute(entity);
		}
		if (!value.AureoleJouee && num >= 1640)
		{
			value.AureoleJouee = true;
			if (entity == Globals.Me)
			{
				Audio.AddGameSound("mort_aureole", loop: false);
			}
			else
			{
				Audio.AddMapSound("mort_aureole.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 10, entity);
			}
		}
		if (num >= 290)
		{
			IGameTexture gameTexture = PlancheDeChute(entity);
			if (gameTexture != null)
			{
				int num2 = ((num >= 570) ? ((num < 1140) ? 1 : 2) : 0);
				float num3 = (float)gameTexture.Width / 3f;
				float num4 = (float)gameTexture.Height / (float)Math.Max(1, Options.Instance.Sprites.Directions);
				texture = gameTexture;
				source = new FloatRect((float)num2 * num3, (float)Math.Clamp(value.Rangee, 0, 3) * num4, num3, num4);
				return true;
			}
		}
		return false;
	}

	private static void Chute(Entity entity)
	{
		Echanges.Poussiere(entity, 4, 1.2f);
		if (entity == Globals.Me)
		{
			CombatFeedback.StartShake(1.2f, 90L, 0f, 1f);
			Audio.AddGameSound("mort_sol", loop: false);
		}
		else
		{
			Audio.AddMapSound("mort_sol.wav", entity.X, entity.Y, entity.MapId, loop: false, 0, 10, entity);
		}
	}

	public static FloatRect Pose(Entity entity, FloatRect destination)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value) || (value.RetourA > 0 && Timing.Global.Milliseconds >= value.RetourA))
		{
			return destination;
		}
		float num = Lisse(110f, 290f, Age(value, Timing.Global.Milliseconds));
		var (num2, num3) = value.Rangee switch
		{
			1 => (3f, 0f), 
			2 => (-3f, 0f), 
			3 => (0f, 3f), 
			_ => (0f, -3f), 
		};
		return new FloatRect(destination.X + MathF.Round(num2 * num), destination.Y + MathF.Round(num3 * num), destination.Width, destination.Height);
	}

	public static int Alpha(Entity entity, int alpha)
	{
		if (!Etats.TryGetValue(entity.Id, out Etat value) || entity == Globals.Me)
		{
			return alpha;
		}
		long num = Age(value, Timing.Global.Milliseconds);
		if (num >= 4600)
		{
			Terminer(entity);
			return alpha;
		}
		return (int)((float)alpha * (1f - Lisse(3640f, 4140f, num)));
	}

	public static void DessinerAme(Entity entity, IGameTexture? planche, FloatRect source, FloatRect corps, int alpha)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value) || planche == null || alpha <= 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		long num = Age(value, milliseconds);
		if (num < 1640 || (value.RetourA > 0 && milliseconds >= value.RetourA) || PlancheDeChute(entity) == null)
		{
			return;
		}
		float num2 = ((entity == Globals.Me) ? 1f : (1f - Lisse(3640f, 4140f, num)));
		if (num2 <= 0f)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		float num3 = corps.X + corps.Width / 2f;
		float num4 = corps.Y + corps.Height;
		float num5 = Sortie(Lisse(2140f, 3640f, num)) * 34f;
		float num6 = ((num < 2040) ? 0f : MathF.Min(Lisse(2040f, 2340f, num), 1f - Lisse(3040f, 3640f, num)));
		IGameTexture gameTexture2 = PlancheDe(entity, "_idle.png") ?? planche;
		FloatRect source2 = new FloatRect(0f, 0f, source.Width, source.Height);
		float num7 = MathF.Round(num3 - source.Width / 2f);
		float num8 = MathF.Round(num4 - source.Height - num5);
		if (num6 > 0f)
		{
			IGameTexture gameTexture3 = KiCharge.MaskOf(gameTexture2, source2, 1);
			if (gameTexture3 != null)
			{
				Graphics.DrawGameTexture(gameTexture3, new FloatRect(0f, 0f, gameTexture3.Width, gameTexture3.Height), new FloatRect(num7 - 1f, num8 - 1f, gameTexture3.Width, gameTexture3.Height), Teinte(255, 255, 255, 0.28f * num6 * num2));
			}
			IGameTexture gameTexture4 = KiCharge.MaskOf(gameTexture2, source2, 0);
			if (gameTexture4 != null)
			{
				Graphics.DrawGameTexture(gameTexture4, new FloatRect(0f, 0f, gameTexture4.Width, gameTexture4.Height), new FloatRect(num7, num8, gameTexture4.Width, gameTexture4.Height), Teinte(255, 255, 255, 0.72f * num6 * num2));
			}
			if (gameTexture != null)
			{
				for (int i = 0; i < 14; i++)
				{
					float num9 = 2040f + Hasard(i + 400) * 1300f;
					float num10 = 700f + Hasard(i + 420) * 500f;
					float num11 = (float)num - num9;
					if (!(num11 < 0f) && !(num11 > num10))
					{
						float x = num3 + (Hasard(i + 440) * 2f - 1f) * 12f + MathF.Sin(num11 / 180f + (float)i) * 1.5f;
						float x2 = num4 - 6f - num11 * (0.016f + Hasard(i + 460) * 0.01f);
						Color renderColor = ((i % 2 == 0) ? Teinte(207, 230, 255, 0.9f * (1f - num11 / num10) * num6 * num2) : Teinte(255, 255, 255, 0.9f * (1f - num11 / num10) * num6 * num2));
						Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(x2), 1f, 1f), renderColor);
					}
				}
			}
		}
		IGameTexture gameTexture5 = Aureole();
		if (gameTexture5 == null)
		{
			return;
		}
		(int, float) tuple = Sommet(planche, source);
		(int, float) tuple2 = Sommet(gameTexture2, source2);
		float num12 = Lisse(2040f, 2340f, num);
		float num13 = corps.X + tuple.Item2 + (num7 + tuple2.Item2 - corps.X - tuple.Item2) * num12;
		float num14 = corps.Y + (float)tuple.Item1 + (num8 + (float)tuple2.Item1 - corps.Y - (float)tuple.Item1) * num12;
		float num15 = MathF.Round(MathF.Sin((float)num / 420f));
		float num16 = Lisse(1640f, 1940f, num);
		float num17 = ((num < 2040) ? 1f : (1f - Lisse(3040f, 3640f, num)));
		Graphics.DrawGameTexture(gameTexture5, new FloatRect(0f, 0f, gameTexture5.Width, gameTexture5.Height), new FloatRect(MathF.Round(num13 - (float)gameTexture5.Width / 2f), MathF.Round(num14 - (float)gameTexture5.Height - 2f + num15), gameTexture5.Width, gameTexture5.Height), Teinte(255, 255, 255, num16 * num17 * num2));
		if (gameTexture != null && num < 2090)
		{
			float num18 = (float)(num - 1640) / 450f;
			for (int j = 0; j < 4; j++)
			{
				float x3 = (float)j * (float)Math.PI / 2f + 0.6f;
				float num19 = 4f + num18 * 6f;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num13 + MathF.Cos(x3) * num19), MathF.Round(num14 - 4f + MathF.Sin(x3) * num19 * 0.6f), 1f, 1f), Teinte(255, 246, 200, (1f - num18) * 0.9f * num2));
			}
		}
	}

	public static void DessinerVoile()
	{
		Player me = Globals.Me;
		if (me == null || Etats.Count == 0 || !Etats.TryGetValue(me.Id, out Etat value))
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.RetourA > 0 && milliseconds >= value.RetourA)
		{
			return;
		}
		float num = Lisse(540f, 1640f, Age(value, milliseconds));
		if (!(num <= 0f))
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), worldViewport, Teinte(112, 112, 118, 0.5f * num));
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), worldViewport, Teinte(0, 0, 0, 0.18f * num));
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "pv_bord_cote.png");
			if (texture != null)
			{
				LowHealth.DrawSoftEdges(texture, worldViewport, MathF.Min(worldViewport.Width, worldViewport.Height) * 0.35f, Teinte(0, 0, 0, 0.6f * num));
			}
		}
	}

	public static void DessinerBlanc()
	{
		Player me = Globals.Me;
		if (me == null || Etats.Count == 0 || !Etats.TryGetValue(me.Id, out Etat value))
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (Oubliee(me, value, milliseconds))
		{
			return;
		}
		long num = Age(value, milliseconds);
		if (value.RetourA == 0L && (Repose(me, value) || num >= 6000))
		{
			value.RetourA = Math.Max(milliseconds, value.Debut + 3840);
		}
		float num2;
		if (value.RetourA == 0L || milliseconds < value.RetourA)
		{
			num2 = Lisse(3140f, 3840f, num);
		}
		else
		{
			if (me.DyingAt != 0L)
			{
				me.DyingAt = 0L;
			}
			float num3 = ((Globals.NeedsMaps && milliseconds - value.RetourA < 3000) ? 0f : Lisse(value.RetourA + 250, value.RetourA + 250 + 700, milliseconds));
			num2 = 1f - num3;
			if (num3 >= 1f)
			{
				Etats.Remove(me.Id);
				return;
			}
		}
		if (num2 > 0f)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), Graphics.CurrentView, Teinte(255, 255, 255, num2));
			}
		}
	}

	private static bool Revenu(Entity entity, Etat etat, long maintenant)
	{
		if (entity == Globals.Me && etat.RetourA > 0)
		{
			return maintenant >= etat.RetourA;
		}
		return false;
	}

	private static bool Repose(Entity entity, Etat etat)
	{
		if (!(entity.MapId != etat.Carte) && Math.Abs(entity.X - etat.X) + Math.Abs(entity.Y - etat.Y) <= 1)
		{
			if (etat.VieNulle)
			{
				return entity.Vital[0] > 0;
			}
			return false;
		}
		return true;
	}

	public static void DessinerFlaque(Entity entity, FloatRect corps)
	{
		if (Etats.Count == 0 || !Etats.TryGetValue(entity.Id, out Etat value) || !value.Ecrite)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		if (value.RetourA > 0 && milliseconds >= value.RetourA)
		{
			return;
		}
		if (value.FlaqueDebut == 0L)
		{
			if (Age(value, milliseconds) < 570)
			{
				return;
			}
			value.FlaqueDebut = milliseconds;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, "flaque_de_sang.png");
		if (texture != null)
		{
			float num = Sortie(Lisse(value.FlaqueDebut, value.FlaqueDebut + 2600, milliseconds));
			float num2 = MathF.Round((float)texture.Width * (0.3f + 0.7f * num));
			float num3 = MathF.Round((float)texture.Height * (0.3f + 0.7f * num));
			float num4 = corps.X + corps.Width / 2f;
			float num5 = corps.Y + corps.Height - 7f;
			Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(MathF.Round(num4 - num2 / 2f), MathF.Round(num5 - num3 / 2f), num2, num3), Teinte(255, 255, 255, 0.95f));
		}
	}

	private static void Terminer(Entity entity)
	{
		Etats.Remove(entity.Id);
		entity.DyingAt = 0L;
		entity.DeathDurationMs = 0L;
	}

	private static void Nettoyer()
	{
		if (Etats.Count == 0)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		Guid[] array = Etats.Keys.ToArray();
		foreach (Guid key in array)
		{
			if (!Globals.Entities.TryGetValue(key, out Entity value) || value == null || value.IsDisposed || Age(Etats[key], milliseconds) > 15000)
			{
				Etats.Remove(key);
			}
		}
	}

	private static IGameTexture? PlancheDeChute(Entity entity)
	{
		return PlancheDe(entity, "_mort.png");
	}

	private static IGameTexture? PlancheDe(Entity entity, string suffixe)
	{
		string text = (string.IsNullOrWhiteSpace(entity.TransformedSprite) ? entity.Sprite : Transformation.TextureNameFor(entity.Sprite, entity.TransformedSprite));
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return Globals.ContentManager.GetTexture(TextureType.Entity, Path.GetFileNameWithoutExtension(text) + suffixe);
	}

	private static IGameTexture? Aureole()
	{
		if (!_aureoleCherchee)
		{
			_aureoleCherchee = true;
			_aureole = Globals.ContentManager?.GetTexture(TextureType.Misc, "aureole.png");
		}
		return _aureole;
	}

	private static (int Y, float X) Sommet(IGameTexture planche, FloatRect source)
	{
		(string, int, int, int, int) tuple = (planche.Name ?? string.Empty, (int)source.X, (int)source.Y, (int)source.Width, (int)source.Height);
		if (Sommets.TryGetValue(tuple, out (int, float) value))
		{
			return value;
		}
		(int, float) tuple2 = (0, source.Width / 2f);
		(string, int, int, int, int) tuple3 = tuple;
		int item = tuple3.Item2;
		int item2 = tuple3.Item3;
		int item3 = tuple3.Item4;
		int item4 = tuple3.Item5;
		if (item >= 0 && item2 >= 0 && item3 > 0 && item4 > 0 && item + item3 <= planche.Width && item2 + item4 <= planche.Height && PixelsPlanche.Lire(planche, out Color[] pixels) && pixels.Length >= planche.Width * planche.Height)
		{
			for (int i = 0; i < item4; i++)
			{
				float num = 0f;
				int num2 = 0;
				for (int j = 0; j < item3; j++)
				{
					if (pixels[(item2 + i) * planche.Width + item + j].A >= 128)
					{
						num += (float)j;
						num2++;
					}
				}
				if (num2 > 0)
				{
					tuple2 = (i, num / (float)num2);
					break;
				}
			}
		}
		Sommets[tuple] = tuple2;
		return tuple2;
	}

	private static float Lisse(float a, float b, float v)
	{
		float num = Math.Clamp((v - a) / (b - a), 0f, 1f);
		return num * num * (3f - 2f * num);
	}

	private static float Sortie(float k)
	{
		return 1f - (1f - k) * (1f - k);
	}

	private static float Hasard(int n)
	{
		float num = MathF.Sin((float)n * 127.1f + 311.7f) * 43758.547f;
		return num - MathF.Floor(num);
	}

	private static Color Teinte(int r, int g, int b, float alpha)
	{
		return new Color((int)Math.Clamp(alpha * 255f, 0f, 255f), r, g, b);
	}
}
