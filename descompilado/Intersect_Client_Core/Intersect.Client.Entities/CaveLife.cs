using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Lighting;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class CaveLife
{
	private sealed class Drop
	{
		public float X;

		public float Y0;

		public float Y1;

		public float BasinX;

		public float BasinY;

		public float BasinRx;

		public float BasinRy;

		public long Born;

		public bool Water;
	}

	private sealed class Ring
	{
		public float X;

		public float Y;

		public float BasinX;

		public float BasinY;

		public float BasinRx;

		public float BasinRy;

		public long Born;
	}

	private sealed class Wet
	{
		public float X;

		public float Y;

		public long Born;
	}

	private sealed class Bit
	{
		public int Kind;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Z0;

		public float Ground;

		public float Angle;

		public long Born;

		public long LandedAt = -1L;

		public int Size = 1;

		public Color Color = Intersect.Color.White;
	}

	private sealed class Bat
	{
		public float HomeX;

		public float HomeY;

		public float Phase;

		public float Side = 1f;

		public int Mode;

		public long ModeAt;

		public long StretchAt = -10000L;

		public long NextStretchAt;

		public int DelayMs;

		public int DurationMs;
	}

	private sealed class Colony
	{
		public readonly List<Bat> Bats = new List<Bat>();

		public float X;

		public float Y;

		public long CalmSince;
	}

	private sealed class Sparkle
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long Born;

		public int LifeMs;

		public Color Color = Intersect.Color.White;

		public bool White;
	}

	private sealed class Mote
	{
		public float Side;

		public float Forward;

		public float Vs;

		public float Vf;

		public float Phase;
	}

	private sealed class Opening
	{
		public Meteo.EntreeGrotte Entree;

		public float X;

		public float Y;

		public readonly List<Mote> Motes = new List<Mote>();
	}

	private const float FanReach = 88f;

	private const float FanHalfWidth = 125f;

	private const int FanTexture = 256;

	private static readonly List<Drop> Drops = new List<Drop>();

	private static readonly List<Ring> Rings = new List<Ring>();

	private static readonly List<Wet> Wets = new List<Wet>();

	private static readonly List<Bit> Bits = new List<Bit>();

	private static readonly List<Sparkle> Sparkles = new List<Sparkle>();

	private static readonly Dictionary<(Guid Map, int Index), Colony> Colonies = new Dictionary<(Guid, int), Colony>();

	private static readonly Dictionary<(Guid Map, int Index), Opening> Openings = new Dictionary<(Guid, int), Opening>();

	private static readonly Dictionary<(Guid Map, int Index), long> NextSparkleAt = new Dictionary<(Guid, int), long>();

	private static readonly List<(MapInstance Map, Meteo.DonneesGrotte Cave)> Visible = new List<(MapInstance, Meteo.DonneesGrotte)>();

	private static readonly List<(float X, float Y, bool Moving, bool Charging)> Walkers = new List<(float, float, bool, bool)>();

	private static readonly List<(float X, float Y, string[] Pose, float Alpha)> EyesToDraw = new List<(float, float, string[], float)>();

	private static readonly Dictionary<(int Dx, int Dy, int Variant), IGameTexture> Fans = new Dictionary<(int, int, int), IGameTexture>();

	private static readonly Random Noise = new Random();

	private static IGameTexture? _halo;

	private static readonly SuiviGrille Grille = new SuiviGrille();

	private static bool _active;

	private static bool _broken;

	private static long _nextDropAt;

	private static long _nextTrickleAt;

	private static readonly Color[] PebbleColors = new Color[3]
	{
		new Color(255, 122, 98, 72),
		new Color(255, 158, 128, 94),
		new Color(255, 92, 72, 54)
	};

	private static readonly Color[] OreColors = new Color[3]
	{
		new Color(255, 150, 150, 156),
		new Color(255, 110, 110, 118),
		new Color(255, 184, 184, 190)
	};

	private static readonly string[] BatHanging = new string[7] { "..k.k..", ".kBBBk.", "wBBBBBw", "wBBBBBw", ".wBBBw.", "..eHe..", "..h.h.." };

	private static readonly string[] BatStretch = new string[7] { "k..k.k..k", "wk.BBB.kw", "wwBBBBBww", "wwBBBBBww", "w..BBB..w", "...eHe...", "...h.h..." };

	private static readonly string[] BatWingsUp = new string[6] { "ww.........ww", "www.......www", ".wwww.B.wwww.", "...wwBBBww...", ".....eHe.....", ".....h.h....." };

	private static readonly string[] BatWingsDown = new string[6] { ".............", "......B......", ".wwwwBBBwwww.", "wwww.eHe.wwww", "ww...h.h...ww", "w...........w" };

	private static readonly Dictionary<char, Color> BatColors = new Dictionary<char, Color>
	{
		['k'] = new Color(255, 22, 16, 18),
		['w'] = new Color(255, 60, 44, 50),
		['B'] = new Color(255, 30, 22, 26),
		['H'] = new Color(255, 42, 32, 36),
		['h'] = new Color(255, 28, 20, 24),
		['e'] = new Color(255, 255, 196, 110)
	};

	private static readonly int[] Bayer = new int[16]
	{
		0, 8, 2, 10, 12, 4, 14, 6, 3, 11,
		1, 9, 15, 7, 13, 5
	};

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	private static float Smooth(float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		return k * k * (3f - 2f * k);
	}

	public static byte Breathe(MapInstance map, int index, byte intensity)
	{
		if (!_active || _broken || !Meteo.EstGrotte(map))
		{
			return intensity;
		}
		return (byte)Math.Clamp((float)(int)intensity * Breath(map, index, Timing.Global.Milliseconds), 0f, 255f);
	}

	private static float Breath(MapInstance map, int index, long now)
	{
		float num = (float)index * 1.7f + (float)(map.Id.GetHashCode() & 0xFF) * 0.1f;
		long num2 = (now + (long)index * 1300L) % 5200;
		return 0.78f + 0.22f * MathF.Sin((float)now / 1150f + num) + 0.35f * MathF.Exp((float)(-num2) / 380f);
	}

	public static void OnKiImpact(float x, float y, bool big)
	{
		if (!_active || _broken)
		{
			return;
		}
		try
		{
			FloatRect worldViewport = Graphics.WorldViewport;
			if (x < worldViewport.Left - 64f || x > worldViewport.Right + 64f || y < worldViewport.Top - 64f || y > worldViewport.Bottom + 64f)
			{
				return;
			}
			long milliseconds = Timing.Global.Milliseconds;
			CombatFeedback.StartShake(big ? 4f : 1.6f, big ? 520 : 240);
			int num = (big ? 18 : 4);
			for (int i = 0; i < num; i++)
			{
				float sx = (big ? (worldViewport.Left + 16f + ((float)(i % 9) + Between(0f, 1f)) * (worldViewport.Width - 32f) / 9f) : (x + Between(-90f, 60f)));
				float sy = (big ? Between(worldViewport.Top + 30f, worldViewport.Bottom - 8f) : (y + Between(-50f, 50f)));
				float num2 = (big ? ((i < 9) ? Between(0f, 500f) : Between(900f, 1700f)) : Between(0f, 300f));
				Ceiling(sx, sy, big, milliseconds + (long)num2);
			}
			int num3 = (big ? 7 : 2);
			foreach (var item3 in Visible)
			{
				MapInstance item = item3.Map;
				Meteo.DonneesGrotte item2 = item3.Cave;
				int[] cailloux = item2.Cailloux;
				foreach (int num4 in cailloux)
				{
					if (num3 <= 0 || Bits.Count >= 900)
					{
						break;
					}
					int num5 = item.X + (num4 & 0xFFFF);
					int num6 = item.Y + (num4 >> 16);
					if (!(MathF.Abs((float)num5 - x) > 90f) && !(MathF.Abs((float)num6 - y) > 90f) && !(Noise.NextDouble() > 0.12))
					{
						num3--;
						Bits.Add(new Bit
						{
							Kind = 6,
							X = num5,
							Y = num6,
							Vx = (float)((Noise.Next(2) != 0) ? 1 : (-1)) * Between(30f, 56f),
							Vy = Between(50f, 90f),
							Ground = (float)num6 + Between(18f, 34f),
							Born = milliseconds + (long)Between(0f, 180f),
							Color = OreColors[Noise.Next(OreColors.Length)]
						});
					}
				}
			}
			Scare(x, y, big ? 100000f : 220f);
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static void Ceiling(float sx, float sy, bool big, long start)
	{
		for (int i = 0; i < (big ? 26 : 18); i++)
		{
			if (Bits.Count >= 900)
			{
				break;
			}
			Bits.Add(new Bit
			{
				Kind = 4,
				X = sx + Between(-7f, 7f),
				Z0 = Between(36f, 80f),
				Ground = sy + Between(-3f, 3f),
				Born = start + (long)Between(0f, 700f),
				Vy = Between(34f, 62f),
				Vx = Between(-4f, 4f)
			});
		}
		for (int j = 0; j < (big ? 5 : 3); j++)
		{
			if (Bits.Count >= 900)
			{
				break;
			}
			Bits.Add(new Bit
			{
				Kind = 5,
				X = sx + Between(-4f, 4f),
				Z0 = Between(50f, 90f),
				Ground = sy + Between(-2f, 3f),
				Born = start + (long)Between(0f, 300f),
				Vx = Between(-8f, 8f),
				Size = Noise.Next(2) + 1,
				Color = PebbleColors[j % PebbleColors.Length]
			});
		}
	}

	private static void Scare(float x, float y, float radius)
	{
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Colony value in Colonies.Values)
		{
			if (Distance(value.X - x, value.Y - y) >= radius || !value.Bats.Any((Bat bat) => bat.Mode == 0))
			{
				continue;
			}
			float side = ((!(MathF.Abs(value.X - x) < 4f)) ? ((float)MathF.Sign(value.X - x)) : ((Noise.Next(2) == 0) ? (-1f) : 1f));
			int num = 0;
			foreach (Bat item in value.Bats.Where((Bat bat) => bat.Mode == 0))
			{
				item.Mode = 1;
				item.ModeAt = milliseconds;
				item.Side = side;
				item.DelayMs = num++ * Noise.Next(40, 90);
				item.DurationMs = Noise.Next(2600, 3300);
			}
			value.CalmSince = milliseconds;
		}
	}

	private static float Distance(float dx, float dy)
	{
		return MathF.Sqrt(dx * dx + dy * dy);
	}

	public static void DrawGround()
	{
		if (_broken)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			Update(milliseconds);
			if (!_active || Graphics.Renderer?.WhitePixel == null)
			{
				return;
			}
			(int, int, int, float) sky = Meteo.LumiereDuDehors();
			foreach (Opening value in Openings.Values)
			{
				DrawGlowTint(value, sky);
			}
			foreach (Wet wet in Wets)
			{
				float num = 0.3f * (1f - Smooth((float)(milliseconds - wet.Born - 1500) / 3700f));
				for (int i = -2; i <= 2; i++)
				{
					for (int j = -4; j <= 4; j++)
					{
						if ((float)(j * j) / 12.96f + (float)(i * i) / 2.89f < 1f)
						{
							Dot(wet.X + (float)j, wet.Y + (float)i, 1f, 1f, new Color((int)(255f * num), 16, 10, 8));
						}
					}
				}
			}
			foreach (Ring ring in Rings)
			{
				long num2 = milliseconds - ring.Born;
				if (num2 < 0)
				{
					continue;
				}
				float num3 = (float)num2 / 1000f;
				float num4 = 1.5f + num3 * 11f;
				float num5 = num4 * 0.45f;
				for (int k = 0; k < 28; k++)
				{
					float x = (float)k / 28f * ((float)Math.PI * 2f);
					float num6 = ring.X + MathF.Cos(x) * num4;
					float num7 = ring.Y + MathF.Sin(x) * num5;
					float num8 = (num6 - ring.BasinX) / ring.BasinRx;
					float num9 = (num7 - ring.BasinY) / ring.BasinRy;
					if (num8 * num8 + num9 * num9 < 1f)
					{
						Dot(num6, num7, 1f, 1f, new Color((int)(178f * (1f - num3)), 214, 238, 250));
					}
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public static void DrawAir()
	{
		EyesToDraw.Clear();
		if (_broken || !_active || Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			FloatRect worldViewport = Graphics.WorldViewport;
			foreach (Colony value in Colonies.Values)
			{
				foreach (Bat bat in value.Bats)
				{
					DrawBat(bat, milliseconds, worldViewport);
				}
			}
			foreach (Drop drop in Drops)
			{
				long num = milliseconds - drop.Born - 1400;
				if (num < 0)
				{
					float num2 = (float)(milliseconds - drop.Born) / 1400f;
					Dot(drop.X, drop.Y0, 1f, 1f, new Color((int)(255f * (0.35f + 0.55f * num2)), 176, 208, 226));
					if (num2 > 0.65f)
					{
						Dot(drop.X, drop.Y0 + 1f, 1f, 1f, new Color((int)(178f * num2), 176, 208, 226));
					}
				}
				else
				{
					float num3 = (float)num / 1000f;
					Dot(drop.X, drop.Y0 + 300f * num3 * num3 - 1f, 1f, 2f, new Color(216, 196, 222, 236));
				}
			}
			foreach (Bit bit in Bits)
			{
				DrawBit(bit, milliseconds, afterDark: false);
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public static void DrawAfterDark()
	{
		if (_broken || !_active || Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			(int, int, int, float) sky = Meteo.LumiereDuDehors();
			foreach (Opening value in Openings.Values)
			{
				DrawFan(value, sky, milliseconds);
			}
			DrawCrystals(milliseconds);
			foreach (Bit bit in Bits)
			{
				if (bit.Kind == 4)
				{
					DrawBit(bit, milliseconds, afterDark: true);
				}
			}
			foreach (var item5 in EyesToDraw)
			{
				float item = item5.X;
				float item2 = item5.Y;
				string[] item3 = item5.Pose;
				float item4 = item5.Alpha;
				int length = item3[0].Length;
				for (int i = 0; i < item3.Length; i++)
				{
					for (int j = 0; j < length; j++)
					{
						if (item3[i][j] == 'e')
						{
							Dot(item - (float)(length >> 1) + (float)j, item2 + (float)i, 1f, 1f, new Color((int)(204f * item4), 255, 180, 80), GameBlendModes.Add);
						}
					}
				}
			}
			foreach (Drop drop in Drops)
			{
				long num = milliseconds - drop.Born;
				if (num > 700 && num < 1400)
				{
					Dot(drop.X, drop.Y0, 1f, 1f, new Color((int)(128f * MathF.Sin((float)(num - 700) / 700f * (float)Math.PI)), 255, 255, 255), GameBlendModes.Add);
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static void Update(long now)
	{
		Player me = Globals.Me;
		MapInstance mapInstance = me?.MapInstance as MapInstance;
		_active = me != null && mapInstance != null && Meteo.EstGrotte(mapInstance);
		if (!_active)
		{
			Clear();
			return;
		}
		if (!Grille.Continu())
		{
			Clear();
		}
		Visible.Clear();
		foreach (MapInstance item in Meteo.CartesVisibles())
		{
			if (Meteo.EstGrotte(item))
			{
				Meteo.DonneesGrotte donneesGrotte = Meteo.GrotteDe(item);
				if (donneesGrotte != null)
				{
					Visible.Add((item, donneesGrotte));
				}
			}
		}
		CollectWalkers();
		FloatRect worldViewport = Graphics.WorldViewport;
		RegisterCaves(now);
		UpdateBats(now);
		UpdateDrops(now, worldViewport);
		UpdateTrickles(now, worldViewport);
		UpdateBits(now);
		UpdateMotes(now);
	}

	private static void Clear()
	{
		Drops.Clear();
		Rings.Clear();
		Wets.Clear();
		Bits.Clear();
		Sparkles.Clear();
		Colonies.Clear();
		Openings.Clear();
		NextSparkleAt.Clear();
		Visible.Clear();
		EyesToDraw.Clear();
	}

	private static void Fail(Exception exception)
	{
		_broken = true;
		Clear();
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Vie des grottes coupee apres un incident");
	}

	private static void CollectWalkers()
	{
		Walkers.Clear();
		if (Globals.Entities == null)
		{
			return;
		}
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && !value.IsHidden && (value.Type == EntityType.Player || value.Type == EntityType.GlobalEntity) && !(value is Critter))
			{
				Vector2 origin = value.Origin;
				Walkers.Add((origin.X, origin.Y, value.IsMoving, value.IsCharging || value.IsTransforming));
			}
		}
	}

	private static void RegisterCaves(long now)
	{
		foreach (var item3 in Visible)
		{
			MapInstance item = item3.Map;
			Meteo.DonneesGrotte item2 = item3.Cave;
			for (int i = 0; i < item2.Colonies.Length; i++)
			{
				if (!Colonies.ContainsKey((item.Id, i)))
				{
					Colony colony = new Colony
					{
						CalmSince = now
					};
					(short, short)[] array = item2.Colonies[i];
					for (int j = 0; j < array.Length; j++)
					{
						var (num, num2) = array[j];
						colony.Bats.Add(new Bat
						{
							HomeX = item.X + num,
							HomeY = item.Y + num2,
							Phase = Between(0f, 6f),
							NextStretchAt = now + Noise.Next(1500, 5000)
						});
					}
					colony.X = colony.Bats.Average((Bat bat) => bat.HomeX);
					colony.Y = colony.Bats.Average((Bat bat) => bat.HomeY);
					Colonies[(item.Id, i)] = colony;
				}
			}
			for (int num3 = 0; num3 < item2.Entrees.Length; num3++)
			{
				if (!Openings.ContainsKey((item.Id, num3)))
				{
					Meteo.EntreeGrotte entreeGrotte = item2.Entrees[num3];
					Opening opening = new Opening
					{
						Entree = entreeGrotte,
						X = item.X + entreeGrotte.X,
						Y = item.Y + entreeGrotte.Y
					};
					for (int num4 = 0; num4 < 30; num4++)
					{
						opening.Motes.Add(NewMote());
					}
					Openings[(item.Id, num3)] = opening;
				}
			}
		}
	}

	private static void UpdateBats(long now)
	{
		foreach (Colony value in Colonies.Values)
		{
			bool flag = value.Bats.All((Bat bat) => bat.Mode == 0);
			bool flag2 = value.Bats.All((Bat bat) => bat.Mode == 2);
			foreach (var walker in Walkers)
			{
				float item = walker.X;
				float item2 = walker.Y;
				bool item3 = walker.Charging;
				float num = Distance(item - value.X, item2 - 24f - value.Y);
				if (num < 200f)
				{
					value.CalmSince = now;
				}
				if (!flag || !(num < (float)(item3 ? 230 : 150)))
				{
					continue;
				}
				Scare(value.X, value.Y + 1f, 4f);
				foreach (Bat bat in value.Bats)
				{
					bat.Side = ((MathF.Abs(value.X - item) < 4f) ? bat.Side : ((float)MathF.Sign(value.X - item)));
				}
				flag = false;
			}
			if (flag2 && now - value.CalmSince > 4000)
			{
				int num2 = 0;
				foreach (Bat bat2 in value.Bats)
				{
					bat2.Mode = 3;
					bat2.ModeAt = now;
					bat2.DelayMs = num2++ * Noise.Next(60, 140);
					bat2.DurationMs = Noise.Next(2800, 3300);
				}
			}
			foreach (Bat bat3 in value.Bats)
			{
				float num3 = (float)(now - bat3.ModeAt - bat3.DelayMs) / (float)bat3.DurationMs;
				if (bat3.Mode == 1 && num3 >= 1f)
				{
					bat3.Mode = 2;
				}
				else if (bat3.Mode == 3 && num3 >= 1f)
				{
					bat3.Mode = 0;
					bat3.NextStretchAt = now + Noise.Next(2500, 6000);
				}
				else if (bat3.Mode == 0 && now >= bat3.NextStretchAt)
				{
					bat3.StretchAt = now;
					bat3.NextStretchAt = now + Noise.Next(2500, 6000);
				}
			}
		}
	}

	private static void UpdateDrops(long now, FloatRect view)
	{
		if (now >= _nextDropAt && Drops.Count < 16)
		{
			_nextDropAt = now + Noise.Next(900, 2000);
			List<(float, float, float, float)> list = new List<(float, float, float, float)>();
			List<(float, float)> list2 = new List<(float, float)>();
			foreach (var item7 in Visible)
			{
				MapInstance item = item7.Map;
				Meteo.DonneesGrotte item2 = item7.Cave;
				(short, short, byte, byte)[] bassins = item2.Bassins;
				for (int i = 0; i < bassins.Length; i++)
				{
					var (num, num2, b, b2) = bassins[i];
					if (Inside(item.X + num, item.Y + num2, view, 16f))
					{
						list.Add((item.X + num, item.Y + num2, (int)b, (int)b2));
					}
				}
				(short, short)[] gouttes = item2.Gouttes;
				for (int j = 0; j < gouttes.Length; j++)
				{
					var (num3, num4) = gouttes[j];
					if (Inside(item.X + num3, item.Y + num4, view, 16f))
					{
						list2.Add((item.X + num3, item.Y + num4));
					}
				}
			}
			if (list.Count > 0 && (list2.Count == 0 || Noise.NextDouble() < 0.45))
			{
				(float, float, float, float) tuple3 = list[Noise.Next(list.Count)];
				float item3 = tuple3.Item1;
				float item4 = tuple3.Item2;
				float item5 = tuple3.Item3;
				float item6 = tuple3.Item4;
				float x = item3 + Between((0f - item5) * 0.6f, item5 * 0.6f);
				float x2 = item4 + Between((0f - item6) * 0.4f, item6 * 0.4f);
				Drops.Add(new Drop
				{
					X = MathF.Round(x),
					Y0 = MathF.Round(x2) - 16f,
					Y1 = MathF.Round(x2),
					Born = now,
					Water = true,
					BasinX = item3,
					BasinY = item4,
					BasinRx = item5,
					BasinRy = item6
				});
			}
			else if (list2.Count > 0)
			{
				var (x3, num5) = list2[Noise.Next(list2.Count)];
				Drops.Add(new Drop
				{
					X = x3,
					Y0 = num5 - 14f,
					Y1 = num5,
					Born = now
				});
			}
		}
		for (int num6 = Drops.Count - 1; num6 >= 0; num6--)
		{
			Drop drop = Drops[num6];
			float num7 = MathF.Sqrt(2f * (drop.Y1 - drop.Y0) / 600f) * 1000f;
			if (!((float)(now - drop.Born - 1400) < num7))
			{
				Drops.RemoveAt(num6);
				if (drop.Water)
				{
					Rings.Add(new Ring
					{
						X = drop.X,
						Y = drop.Y1,
						Born = now,
						BasinX = drop.BasinX,
						BasinY = drop.BasinY,
						BasinRx = drop.BasinRx,
						BasinRy = drop.BasinRy
					});
					Rings.Add(new Ring
					{
						X = drop.X,
						Y = drop.Y1,
						Born = now + 260,
						BasinX = drop.BasinX,
						BasinY = drop.BasinY,
						BasinRx = drop.BasinRx,
						BasinRy = drop.BasinRy
					});
				}
				else
				{
					Wet wet = Wets.FirstOrDefault((Wet w) => MathF.Abs(w.X - drop.X) < 3f && MathF.Abs(w.Y - drop.Y1) < 3f);
					if (wet != null)
					{
						wet.Born = Math.Max(wet.Born, now - 800);
					}
					else if (Wets.Count < 40)
					{
						Wets.Add(new Wet
						{
							X = drop.X,
							Y = drop.Y1,
							Born = now
						});
					}
					for (int num8 = 0; num8 < 6; num8++)
					{
						if (Bits.Count >= 900)
						{
							break;
						}
						Bits.Add(new Bit
						{
							Kind = 0,
							X = drop.X,
							Y = drop.Y1,
							Vx = Between(-26f, 26f),
							Vy = 0f - Between(16f, 42f),
							Born = now
						});
					}
				}
			}
		}
		Rings.RemoveAll((Ring ring) => now - ring.Born > 1000);
		Wets.RemoveAll((Wet wet2) => now - wet2.Born > 5200);
	}

	private static void UpdateTrickles(long now, FloatRect view)
	{
		if (now < _nextTrickleAt)
		{
			return;
		}
		_nextTrickleAt = now + Noise.Next(3500, 6000);
		List<(float, float, float)> list = new List<(float, float, float)>();
		foreach (var item6 in Visible)
		{
			MapInstance item = item6.Map;
			Meteo.DonneesGrotte item2 = item6.Cave;
			(short, short, short)[] parois = item2.Parois;
			for (int i = 0; i < parois.Length; i++)
			{
				var (num, num2, num3) = parois[i];
				if (Inside(item.X + num, (float)item.Y + (float)(num2 + num3) / 2f, view, 0f))
				{
					list.Add((item.X + num, item.Y + num2, item.Y + num3));
				}
			}
		}
		if (list.Count != 0 && Bits.Count <= 700)
		{
			(float, float, float) tuple2 = list[Noise.Next(list.Count)];
			float item3 = tuple2.Item1;
			float item4 = tuple2.Item2;
			float item5 = tuple2.Item3;
			float num4 = item4 + Between(4f, Math.Max(5f, (item5 - item4) * 0.3f));
			for (int j = 0; j < 5; j++)
			{
				Bits.Add(new Bit
				{
					Kind = 1,
					X = item3 + Between(-2f, 2f),
					Y = num4 + Between(0f, 6f),
					Ground = item5 + Between(-1f, 2f),
					Vy = Between(0f, 10f),
					Vx = Between(-7f, 7f),
					Born = now + j * Noise.Next(70, 150),
					Size = ((!(Noise.NextDouble() < 0.3)) ? 1 : 2),
					Color = PebbleColors[j % PebbleColors.Length]
				});
			}
			for (int k = 0; k < 16; k++)
			{
				Bits.Add(new Bit
				{
					Kind = 2,
					X = item3 + Between(-1.5f, 1.5f),
					Y = num4 + Between(0f, 4f),
					Ground = item5,
					Vx = Between(-4f, 4f),
					Born = now + k * 42
				});
			}
		}
	}

	private static void UpdateBits(long now)
	{
		for (int num = Bits.Count - 1; num >= 0; num--)
		{
			Bit bit = Bits[num];
			long num2 = now - bit.Born;
			if (num2 >= 0)
			{
				float num3 = (float)num2 / 1000f;
				if (bit.Kind switch
				{
					0 => num2 > 320, 
					1 => bit.LandedAt >= 0 && now - bit.LandedAt > 2600, 
					2 => num3 > 1.3f || bit.Y + 36f * num3 + 70f * num3 * num3 >= bit.Ground, 
					3 => num2 > 650, 
					4 => num2 > 3600, 
					5 => num2 > 4200, 
					_ => num3 > 3.2f, 
				})
				{
					Bits.RemoveAt(num);
				}
				else if (bit.Kind == 1 && bit.LandedAt < 0 && bit.Y + bit.Vy * num3 + 170f * num3 * num3 >= bit.Ground)
				{
					bit.LandedAt = now;
					for (int i = 0; i < 6; i++)
					{
						if (Bits.Count >= 900)
						{
							break;
						}
						Bits.Add(new Bit
						{
							Kind = 3,
							X = bit.X,
							Y = bit.Ground,
							Angle = (float)i / 6f * ((float)Math.PI * 2f) + Between(0f, 0.5f),
							Born = now
						});
					}
				}
			}
		}
	}

	private static void DrawBat(Bat bat, long now, FloatRect view)
	{
		float num = 1f;
		float num2 = (float)(now - bat.ModeAt - bat.DelayMs) / (float)bat.DurationMs;
		string[] array;
		float num3;
		float num4;
		switch (bat.Mode)
		{
		default:
			return;
		case 0:
		{
			float homeX2 = bat.HomeX;
			float homeY2 = bat.HomeY;
			array = ((now - bat.StretchAt < 320) ? BatStretch : BatHanging);
			num3 = homeY2;
			num4 = homeX2;
			break;
		}
		case 1:
			if (num2 < 0f)
			{
				float homeX = bat.HomeX;
				float homeY = bat.HomeY;
				array = BatHanging;
				num3 = homeY;
				num4 = homeX;
				break;
			}
			goto case 3;
		case 3:
		{
			if (num2 < 0f)
			{
				return;
			}
			num2 = Math.Min(1f, num2);
			float side = bat.Side;
			(float, float)[] points = ((bat.Mode != 1) ? new(float, float)[6]
			{
				(side * 340f, -120f),
				(side * 260f, -30f),
				(side * 150f, 70f),
				(side * 60f, 60f),
				(side * 18f, 32f),
				(0f, 4f)
			} : new(float, float)[6]
			{
				(0f, 4f),
				(side * 26f, 44f),
				(side * 110f, 72f),
				(side * 220f, 34f),
				(side * 300f, -40f),
				(side * 340f, -120f)
			});
			(float X, float Y) tuple = Spline(points, num2);
			float item = tuple.X;
			float item2 = tuple.Y;
			float num5 = ((bat.Mode == 3) ? (1f - Smooth((num2 - 0.6f) / 0.4f)) : Smooth(num2 / 0.15f));
			num4 = bat.HomeX + item + MathF.Sin(num2 * 17f + bat.Phase) * 9f * num5;
			num3 = bat.HomeY + item2 + MathF.Cos(num2 * 13f + bat.Phase * 2f) * 6f * num5;
			num = ((bat.Mode == 1) ? (1f - Smooth((num2 - 0.85f) / 0.15f)) : Smooth(num2 / 0.15f));
			array = ((bat.Mode == 3 && num2 > 0.93f) ? BatHanging : (((now + (long)(bat.Phase * 100f)) / 70 % 2 == 0L) ? BatWingsUp : BatWingsDown));
			if (array != BatHanging)
			{
				Dot(num4 - 2f, num3 + 22f, 5f, 1f, new Color((int)(40f * num), 0, 0, 0));
			}
			break;
		}
		case 2:
			return;
		}
		if (num <= 0.01f || !Inside(num4, num3, view, 16f))
		{
			return;
		}
		int length = array[0].Length;
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < length; j++)
			{
				char c = array[i][j];
				if (c != '.' && BatColors.TryGetValue(c, out Color value))
				{
					Dot(num4 - (float)(length >> 1) + (float)j, num3 + (float)i, 1f, 1f, (num >= 1f) ? value : new Color((int)(255f * num), value.R, value.G, value.B));
				}
			}
		}
		EyesToDraw.Add((num4, num3, array, num));
	}

	private static (float X, float Y) Spline((float X, float Y)[] points, float k)
	{
		int num = points.Length - 1;
		float num2 = Math.Clamp(k * (float)num, 0f, (float)num - 0.0001f);
		int num3 = (int)num2;
		float u = num2 - (float)num3;
		(float, float) tuple = points[Math.Max(0, num3 - 1)];
		(float, float) tuple2 = points[num3];
		(float, float) tuple3 = points[num3 + 1];
		(float, float) tuple4 = points[Math.Min(num, num3 + 2)];
		return (X: Cr(tuple.Item1, tuple2.Item1, tuple3.Item1, tuple4.Item1), Y: Cr(tuple.Item2, tuple2.Item2, tuple3.Item2, tuple4.Item2));
		float Cr(float a, float b, float c, float d)
		{
			return 0.5f * (2f * b + (0f - a + c) * u + (2f * a - 5f * b + 4f * c - d) * u * u + (0f - a + 3f * b - 3f * c + d) * u * u * u);
		}
	}

	private static void DrawBit(Bit bit, long now, bool afterDark)
	{
		long num = now - bit.Born;
		if (num < 0 || bit.Kind == 4 != afterDark)
		{
			return;
		}
		float num2 = (float)num / 1000f;
		switch (bit.Kind)
		{
		case 0:
			Dot(bit.X + bit.Vx * num2, bit.Y + bit.Vy * num2 + 260f * num2 * num2, 1f, 1f, new Color((int)(204f * (1f - (float)num / 320f)), 206, 230, 242));
			return;
		case 1:
		{
			if (bit.LandedAt < 0)
			{
				Dot(bit.X, bit.Y + bit.Vy * num2 + 170f * num2 * num2, bit.Size, bit.Size, bit.Color);
				return;
			}
			float num3 = (float)(now - bit.LandedAt) / 1000f;
			float num4 = bit.Vx * (1f - MathF.Exp((0f - num3) * 6f));
			float num5 = MathF.Abs(MathF.Sin(num3 * 13f)) * 3f * MathF.Exp((0f - num3) * 6f);
			float num6 = 1f - Smooth((float)(now - bit.LandedAt - 1800) / 800f);
			Dot(bit.X + num4, bit.Ground - num5, bit.Size, bit.Size, new Color((int)(255f * num6), bit.Color.R, bit.Color.G, bit.Color.B));
			return;
		}
		case 2:
			Dot(bit.X + bit.Vx * num2, bit.Y + 36f * num2 + 70f * num2 * num2, 1f, 1f, new Color((int)(66f * (1f - num2 / 1.3f)), 206, 186, 150));
			return;
		case 3:
		{
			float num14 = (float)num / 650f;
			Dot(bit.X + MathF.Cos(bit.Angle) * 5f * num14, bit.Y - 1f + MathF.Sin(bit.Angle) * 2f * num14 - 2f * num14, 1f, 1f, new Color((int)(77f * (1f - num14)), 206, 186, 150));
			return;
		}
		case 4:
		{
			float num7 = bit.Z0 - bit.Vy * num2;
			float x = bit.X + bit.Vx * num2;
			if (num7 > 0f)
			{
				Dot(x, bit.Ground - num7, 1f, 3f, new Color(140, 184, 164, 130));
				return;
			}
			float num8 = (0f - num7) / bit.Vy;
			Dot(x, bit.Ground, 2f, 1f, new Color((int)Math.Clamp(115f * (1f - num8 / 1.2f), 0f, 255f), 170, 152, 120));
			return;
		}
		case 5:
		{
			float num9 = MathF.Sqrt(2f * bit.Z0 / 420f);
			if (num2 < num9)
			{
				float num10 = bit.Z0 - 210f * num2 * num2;
				Dot(bit.X, bit.Ground, 1f, 1f, new Color((int)(64f * (1f - num10 / 90f)), 0, 0, 0));
				Dot(bit.X, bit.Ground - num10, bit.Size, bit.Size, bit.Color);
			}
			else
			{
				float num11 = num2 - num9;
				float num12 = MathF.Abs(MathF.Sin(num11 * 12f)) * 3f * MathF.Exp((0f - num11) * 6f);
				float num13 = 1f - Smooth((num11 - 1.8f) / 0.8f);
				Dot(bit.X + bit.Vx * (1f - MathF.Exp((0f - num11) * 5f)), bit.Ground - num12, bit.Size, bit.Size, new Color((int)(255f * num13), bit.Color.R, bit.Color.G, bit.Color.B));
			}
			return;
		}
		}
		float num15 = bit.Vy / 160f;
		float num16 = bit.Vy * num2 - 160f * num2 * num2;
		float x2 = bit.X + bit.Vx * Math.Min(num2, num15);
		float num17 = bit.Y + (bit.Ground - bit.Y) * Math.Min(1f, num2 / num15);
		if (num16 > 0f || num2 < 0.1f)
		{
			Dot(x2, num17 - Math.Max(0f, num16), 2f, 2f, bit.Color);
			return;
		}
		float num18 = 1f - Smooth((num2 - 2.2f) / 1f);
		Dot(x2, bit.Ground, 2f, 1f, new Color((int)(255f * num18), bit.Color.R, bit.Color.G, bit.Color.B));
	}

	private static void DrawGlowTint(Opening opening, (int R, int G, int B, float Force) sky)
	{
		Meteo.EntreeGrotte entree = opening.Entree;
		if (entree.L <= 0 || entree.H <= 0 || !(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return;
		}
		float num = Multiplier(sky.R);
		float num2 = Multiplier(sky.G);
		float num3 = Multiplier(sky.B);
		int num4 = (int)(num * 40f) | ((int)(num2 * 40f) << 8) | ((int)(num3 * 40f) << 16);
		if (num4 != entree.VoileCle || entree.Voile == null)
		{
			entree.VoileCle = num4;
			Meteo.EntreeGrotte entreeGrotte = entree;
			if (entreeGrotte.RgbaVoile == null)
			{
				entreeGrotte.RgbaVoile = new byte[entree.L * entree.H * 4];
			}
			for (int i = 0; i < entree.Poids.Length; i++)
			{
				float num5 = (float)(int)entree.Poids[i] / 255f;
				entree.RgbaVoile[i * 4] = (byte)Math.Clamp(255f * (1f + (num - 1f) * num5), 0f, 255f);
				entree.RgbaVoile[i * 4 + 1] = (byte)Math.Clamp(255f * (1f + (num2 - 1f) * num5), 0f, 255f);
				entree.RgbaVoile[i * 4 + 2] = (byte)Math.Clamp(255f * (1f + (num3 - 1f) * num5), 0f, 255f);
				entree.RgbaVoile[i * 4 + 3] = byte.MaxValue;
			}
			if (entree.Voile == null || !MonoRenderer.RemplacerPixels(entree.Voile, entree.RgbaVoile))
			{
				entree.Voile?.Dispose();
				entree.Voile = monoRenderer.CreerTexturePixels($"grotte/lueur/{opening.X}/{opening.Y}", entree.L, entree.H, entree.RgbaVoile);
			}
		}
		float x = opening.X - (float)entree.X + (float)entree.X0;
		float y = opening.Y - (float)entree.Y + (float)entree.Y0;
		Graphics.DrawGameTexture(entree.Voile, new FloatRect(0f, 0f, entree.L, entree.H), new FloatRect(x, y, entree.L, entree.H), Color.White, null, GameBlendModes.Multiply);
		float Multiplier(int channel)
		{
			return 1f + ((float)channel / 255f * (0.3f + 0.7f * sky.Force) - 1f) * 0.85f;
		}
	}

	private static void DrawFan(Opening opening, (int R, int G, int B, float Force) sky, long now)
	{
		Meteo.EntreeGrotte entree = opening.Entree;
		if (!Inside(opening.X, opening.Y, Graphics.WorldViewport, 256f))
		{
			return;
		}
		float num = 0.84f + 0.1f * MathF.Sin((float)now / 2300f) + 0.06f * MathF.Sin((float)now / 830f);
		float num2 = 0.5f + 0.5f * MathF.Sin((float)now / 2600f);
		float num3 = Math.Clamp(sky.Force * num, 0f, 1f);
		for (int i = 0; i < 2; i++)
		{
			IGameTexture gameTexture = FanOf(entree.Dx, entree.Dy, i);
			float num4 = ((i == 0) ? (1f - num2) : num2);
			if (gameTexture != null && !(num3 * num4 < 0.01f))
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 256f, 256f), new FloatRect(opening.X - 128f, opening.Y - 128f, 256f, 256f), new Color((int)(255f * num3 * num4), sky.R, sky.G, sky.B), null, GameBlendModes.Add);
			}
		}
		int r = Math.Min(255, sky.R + 30);
		int g = Math.Min(255, sky.G + 30);
		int b = Math.Min(255, sky.B + 30);
		foreach (Mote mote in opening.Motes)
		{
			float x = opening.X + (float)(-entree.Dy) * mote.Side + (float)entree.Dx * mote.Forward;
			float y = opening.Y + (float)entree.Dx * mote.Side + (float)entree.Dy * mote.Forward;
			float num5 = FanStrength(mote.Side, mote.Forward) * num;
			float num6 = 0.45f + 0.55f * MathF.Max(0f, MathF.Sin((float)now / 380f + mote.Phase * 5f));
			float num7 = Math.Clamp(num5 * 1.8f, 0f, 1f) * num6 * sky.Force;
			if (num7 > 0.02f)
			{
				Dot(x, y, 1f, 1f, new Color((int)(255f * num7), r, g, b), GameBlendModes.Add);
			}
		}
	}

	private static void UpdateMotes(long now)
	{
		foreach (Opening value in Openings.Values)
		{
			Meteo.EntreeGrotte entree = value.Entree;
			foreach (Mote mote5 in value.Motes)
			{
				mote5.Vs += (MathF.Sin((float)now / 1700f + mote5.Phase) * 5f - mote5.Vs) * (1f / 60f) * 2f;
				mote5.Vf += (2f + MathF.Sin((float)now / 2300f + mote5.Phase * 2f) * 4f - mote5.Vf) * (1f / 60f) * 2f;
				float num = value.X + (float)(-entree.Dy) * mote5.Side + (float)entree.Dx * mote5.Forward;
				float num2 = value.Y + (float)entree.Dx * mote5.Side + (float)entree.Dy * mote5.Forward;
				foreach (var walker in Walkers)
				{
					float item = walker.X;
					float item2 = walker.Y;
					bool item3 = walker.Moving;
					float num3 = num - item;
					float num4 = (num2 - (item2 - 20f)) * 0.8f;
					float num5 = Distance(num3, num4);
					if (item3 && num5 < 22f)
					{
						float num6 = (float)(-entree.Dy) * num3 + (float)entree.Dx * num4;
						float num7 = (float)entree.Dx * num3 + (float)entree.Dy * num4;
						mote5.Vs += num6 / (num5 + 1f) * 90f * (1f / 60f);
						mote5.Vf += num7 / (num5 + 1f) * 60f * (1f / 60f);
					}
				}
				mote5.Side += mote5.Vs * (1f / 60f);
				mote5.Forward += mote5.Vf * (1f / 60f);
				if (mote5.Forward < 0f || Distance(mote5.Side / 125f, mote5.Forward / 88f) > 0.92f)
				{
					Mote mote = NewMote();
					Mote mote2 = mote5;
					Mote mote3 = mote5;
					Mote mote4 = mote5;
					float side = mote.Side;
					float forward = mote.Forward;
					mote5.Side = side;
					mote2.Forward = forward;
					mote3.Vs = 0f;
					mote4.Vf = 0f;
				}
			}
		}
	}

	private static Mote NewMote()
	{
		float x = Between(-1.1f, 1.1f);
		float num = Between(0.05f, 0.8f);
		return new Mote
		{
			Side = MathF.Sin(x) * num * 125f,
			Forward = MathF.Cos(x) * num * 88f,
			Phase = Between(0f, 6f)
		};
	}

	private static float FanStrength(float side, float forward)
	{
		if (forward < -2f)
		{
			return 0f;
		}
		float num = Distance(side / 125f, forward / 88f);
		if (num >= 1f)
		{
			return 0f;
		}
		float x = MathF.Atan2(side, forward + 8f);
		return MathF.Pow(1f - num, 1.3f) * Smooth((1.3f - MathF.Abs(x)) / 0.55f);
	}

	private static IGameTexture? FanOf(int dx, int dy, int variant)
	{
		if (Fans.TryGetValue((dx, dy, variant), out IGameTexture value))
		{
			return value;
		}
		if (!(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return null;
		}
		byte[] array = new byte[262144];
		float num = 128f;
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				float num2 = (float)j + 0.5f - num;
				float num3 = (float)i + 0.5f - num;
				float num4 = (float)(-dy) * num2 + (float)dx * num3;
				float num5 = (float)dx * num2 + (float)dy * num3;
				int num6 = (i * 256 + j) * 4;
				int num7 = num6 + 1;
				byte b;
				array[num6 + 2] = (b = byte.MaxValue);
				array[num6] = (array[num7] = b);
				float num8 = FanStrength(num4, num5);
				if (!(num8 <= 0f))
				{
					float num9 = MathF.Atan2(num4, num5 + 8f);
					float num10 = ((variant == 0) ? (0.72f + 0.28f * MathF.Sin(num9 * 11f) * MathF.Sin(num9 * 5f)) : (0.72f + 0.28f * MathF.Sin(num9 * 11f + 1.6f) * MathF.Sin(num9 * 5f - 2.3f)));
					float num11 = Math.Min(1f, MathF.Floor(num8 * num10 * 6f + (float)Bayer[(i & 3) * 4 + (j & 3)] / 16f) / 6f);
					array[num6 + 3] = (byte)Math.Clamp(num11 * 0.45f * 255f, 0f, 255f);
				}
			}
		}
		IGameTexture gameTexture = monoRenderer.CreerTexturePixels($"grotte/eventail/{dx}/{dy}/{variant}", 256, 256, array);
		Fans[(dx, dy, variant)] = gameTexture;
		return gameTexture;
	}

	private static void DrawCrystals(long now)
	{
		if (_halo == null && Graphics.Renderer is MonoRenderer monoRenderer)
		{
			_halo = monoRenderer.CreerTexturePixels("grotte/halo", 128, 128, KiLight.BuildTexture());
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		foreach (var item2 in Visible)
		{
			MapInstance item = item2.Map;
			for (int i = 0; i < item.Lights.Count; i++)
			{
				LightDescriptor lightDescriptor = item.Lights[i];
				if (lightDescriptor.Intensity < 60 || lightDescriptor.Size < 40)
				{
					continue;
				}
				float num = (float)(item.X + lightDescriptor.TileX * tileWidth + lightDescriptor.OffsetX) + (float)tileWidth / 2f;
				float num2 = (float)(item.Y + lightDescriptor.TileY * tileHeight + lightDescriptor.OffsetY) + (float)tileHeight / 2f;
				if (!Inside(num, num2, worldViewport, 32f))
				{
					continue;
				}
				float num3 = Breath(item, i, now);
				Color renderColor = new Color((int)Math.Clamp(178.5f * num3 * (float)(int)lightDescriptor.Intensity / 255f, 0f, 255f), lightDescriptor.Color.R + (255 - lightDescriptor.Color.R) / 4, Math.Max(110, lightDescriptor.Color.G + (255 - lightDescriptor.Color.G) / 4), lightDescriptor.Color.B + (255 - lightDescriptor.Color.B) / 4);
				if (_halo != null)
				{
					float num4 = 22f + 4f * num3;
					Graphics.DrawGameTexture(_halo, new FloatRect(0f, 0f, 128f, 128f), new FloatRect(num - num4, num2 - 3f - num4, num4 * 2f, num4 * 2f), renderColor, null, GameBlendModes.Add);
				}
				(Guid, int) key = (item.Id, i);
				if (!NextSparkleAt.TryGetValue(key, out var value) || now >= value)
				{
					NextSparkleAt[key] = now + Noise.Next(110, 220);
					if (Sparkles.Count < 200)
					{
						bool flag = Noise.NextDouble() < 0.4;
						Sparkles.Add(new Sparkle
						{
							X = num + Between(-5f, 5f),
							Y = num2 - 3f + Between(-3f, 2f),
							Vx = Between(-4f, 4f),
							Vy = 0f - Between(9f, 20f),
							Born = now,
							LifeMs = Noise.Next(1000, 1700),
							White = flag,
							Color = (flag ? Color.White : new Color(255, lightDescriptor.Color.R + (255 - lightDescriptor.Color.R) / 2, lightDescriptor.Color.G + (255 - lightDescriptor.Color.G) / 2, lightDescriptor.Color.B + (255 - lightDescriptor.Color.B) / 2))
						});
					}
				}
			}
		}
		for (int num5 = Sparkles.Count - 1; num5 >= 0; num5--)
		{
			Sparkle sparkle = Sparkles[num5];
			long num6 = now - sparkle.Born;
			if (num6 >= sparkle.LifeMs)
			{
				Sparkles.RemoveAt(num5);
			}
			else
			{
				float num7 = (float)num6 / (float)sparkle.LifeMs;
				float num8 = (float)num6 / 1000f;
				float num9 = sparkle.X + sparkle.Vx * num8;
				float num10 = sparkle.Y + sparkle.Vy * num8;
				Color color = new Color((int)(242f * (1f - num7)), sparkle.Color.R, sparkle.Color.G, sparkle.Color.B);
				Dot(num9, num10, 1f, 1f, color, GameBlendModes.Add);
				if (sparkle.White && num6 < 140)
				{
					Color color2 = new Color(153, 255, 255, 255);
					Dot(num9 - 1f, num10, 1f, 1f, color2, GameBlendModes.Add);
					Dot(num9 + 1f, num10, 1f, 1f, color2, GameBlendModes.Add);
					Dot(num9, num10 - 1f, 1f, 1f, color2, GameBlendModes.Add);
					Dot(num9, num10 + 1f, 1f, 1f, color2, GameBlendModes.Add);
				}
			}
		}
	}

	private static bool Inside(float x, float y, FloatRect view, float margin)
	{
		if (x >= view.Left - margin && x <= view.Right + margin && y >= view.Top - margin)
		{
			return y <= view.Bottom + margin;
		}
		return false;
	}

	private static void Dot(float x, float y, float width, float height, Color color, GameBlendModes mode = GameBlendModes.None)
	{
		if (color.A != 0)
		{
			IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(y), width, height), color, null, mode);
			}
		}
	}
}
