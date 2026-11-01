using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class CityLife
{
	private sealed class Pigeon
	{
		public float HomeX;

		public float HomeY;

		public float X;

		public float Y;

		public float Z;

		public float RoofX;

		public float RoofY;

		public float FromX;

		public float FromY;

		public float Phase;

		public int Mode;

		public int Dir = 1;

		public long ModeAt;

		public long CalmSince;

		public long NextStepAt;
	}

	private sealed class Car
	{
		public float X;

		public float Y;

		public float Speed;

		public float Length;
	}

	private sealed class Puff
	{
		public float X;

		public float Y;

		public float Phase;

		public long Born;

		public int LifeMs;
	}

	private sealed class Hen
	{
		public float HomeX;

		public float HomeY;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public int Mode;

		public int Dir = 1;

		public long ModeAt;

		public long ScratchUntil;

		public long NextActAt;

		public bool White;
	}

	private sealed class Dust
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long Born;
	}

	private static readonly Dictionary<(Guid Map, int Index), Pigeon[]> Flocks = new Dictionary<(Guid, int), Pigeon[]>();

	private static readonly Dictionary<(Guid Map, int Index), Hen[]> Coops = new Dictionary<(Guid, int), Hen[]>();

	private static readonly Dictionary<(Guid Map, int Index), long> NextPuffAt = new Dictionary<(Guid, int), long>();

	private static readonly Dictionary<(Guid Map, int Index), (long Until, long Next)> Flickers = new Dictionary<(Guid, int), (long, long)>();

	private static readonly List<Car> Cars = new List<Car>();

	private static readonly List<Puff> Puffs = new List<Puff>();

	private static readonly List<Dust> Dusts = new List<Dust>();

	private static readonly List<(MapInstance Map, Meteo.DonneesVille Ville)> Visible = new List<(MapInstance, Meteo.DonneesVille)>();

	private static readonly List<(float X, float Y, bool Moving)> Walkers = new List<(float, float, bool)>();

	private static readonly Random Noise = new Random();

	private static readonly IGameTexture?[] SmokeDiscs = new IGameTexture[10];

	private static readonly SuiviGrille Grille = new SuiviGrille();

	private static bool _active;

	private static bool _broken;

	private static long _lastUpdate;

	private static long _nextCarAt;

	private static long _reportAt;

	private static readonly string[] PigeonStand = new string[7] { ".....HH..", "....HHHb.", "....NNN..", "TWWBBBBN.", "TTWWBBBB.", "..WWBBB..", "....p.p.." };

	private static readonly string[] PigeonPeck = new string[7] { ".........", ".........", ".........", "TWWBBBN..", "TTWWBBBHH", "..WWBBBHb", "....p.p.." };

	private static readonly string[] PigeonWingsUp = new string[7] { "WWW......", ".WWW.....", "..WWW.HH.", "TTBBBBHHb", "..BBBBN..", ".........", "........." };

	private static readonly string[] PigeonWingsDown = new string[7] { ".........", ".........", "......HH.", "TTBBBBHHb", "..WWWBN..", ".WWW.....", "WWW......" };

	private static readonly Dictionary<char, Color> PigeonColors = new Dictionary<char, Color>
	{
		['H'] = new Color(255, 96, 100, 118),
		['b'] = new Color(255, 40, 40, 44),
		['N'] = new Color(255, 104, 150, 128),
		['B'] = new Color(255, 156, 158, 170),
		['W'] = new Color(255, 118, 120, 134),
		['T'] = new Color(255, 70, 72, 84),
		['p'] = new Color(255, 206, 96, 96)
	};

	private static readonly string[] HenStand = new string[8] { "......rr.", ".....HHHe", ".....HHH>", "T..WWWHH.", "TTWWWWWW.", ".TWWWWWW.", "..WWWWW..", "...l..l.." };

	private static readonly string[] HenPeck = new string[8] { ".........", ".........", ".........", "T..WWW...", "TTWWWWWrr", ".TWWWWWHe", "..WWWWWH>", "...l..l.." };

	private static readonly string[] HenFlap = new string[8] { "AA....rr.", ".AA..HHHe", "..AWWHHH>", "TTWWWWWW.", ".TWWWWWW.", "..WWWWW..", "...l..l..", "........." };

	private static readonly Dictionary<char, Color> WhiteHen = new Dictionary<char, Color>
	{
		['r'] = new Color(255, 214, 50, 44),
		['H'] = new Color(255, 246, 244, 236),
		['e'] = new Color(255, 20, 16, 14),
		['>'] = new Color(255, 236, 176, 60),
		['W'] = new Color(255, 236, 232, 222),
		['T'] = new Color(255, 206, 202, 190),
		['l'] = new Color(255, 220, 150, 60),
		['A'] = new Color(255, 250, 250, 244)
	};

	private static readonly Dictionary<char, Color> RedHen = new Dictionary<char, Color>
	{
		['r'] = new Color(255, 214, 50, 44),
		['H'] = new Color(255, 168, 96, 44),
		['e'] = new Color(255, 20, 16, 14),
		['>'] = new Color(255, 236, 176, 60),
		['W'] = new Color(255, 150, 84, 40),
		['T'] = new Color(255, 96, 52, 26),
		['l'] = new Color(255, 220, 150, 60),
		['A'] = new Color(255, 196, 120, 60)
	};

	private static readonly int[] Bayer = new int[16]
	{
		0, 8, 2, 10, 12, 4, 14, 6, 3, 11,
		1, 9, 15, 7, 13, 5
	};

	private static readonly Dictionary<IGameTexture, (Meteo.Vitre2[] Windows, IGameTexture? Mask)> HouseWindows = new Dictionary<IGameTexture, (Meteo.Vitre2[], IGameTexture)>();

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	private static float Smooth(float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		return k * k * (3f - 2f * k);
	}

	private static float Distance(float dx, float dy)
	{
		return MathF.Sqrt(dx * dx + dy * dy);
	}

	public static void OnKiImpact(float x, float y, bool big)
	{
		if (!_active || _broken)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		float num = (big ? 400f : 200f);
		foreach (Pigeon[] value in Flocks.Values)
		{
			Pigeon[] array = value;
			foreach (Pigeon pigeon in array)
			{
				int mode = pigeon.Mode;
				bool flag = ((mode == 0 || mode == 3) ? true : false);
				if (flag && Distance(pigeon.X - x, pigeon.Y - y) < num)
				{
					TakeOff(pigeon, milliseconds);
				}
			}
		}
		foreach (Hen[] value2 in Coops.Values)
		{
			Hen[] array2 = value2;
			foreach (Hen hen in array2)
			{
				if (hen.Mode != 1 && Distance(hen.X - x, hen.Y - y) < num)
				{
					Bolt(hen, x, y, milliseconds);
				}
			}
		}
	}

	public static void AddLights()
	{
		if (!_active || _broken)
		{
			return;
		}
		try
		{
			float lampesActuelles = Meteo.LampesActuelles;
			if (lampesActuelles < 0.1f)
			{
				return;
			}
			float heureActuelle = Meteo.HeureActuelle;
			FloatRect worldViewport = Graphics.WorldViewport;
			Color color = Color.FromArgb(255, 255, 186, 100);
			foreach (var item6 in Visible)
			{
				MapInstance item = item6.Map;
				Meteo.DonneesVille item2 = item6.Ville;
				Meteo.Vitre2[] vitres = item2.Vitres;
				for (int i = 0; i < vitres.Length; i++)
				{
					Meteo.Vitre2 window = vitres[i];
					if (!Fallen(item, window.X0, window.Y0, window.L, window.H))
					{
						float num = WindowOn(window, lampesActuelles, heureActuelle);
						float num2 = (float)(item.X + window.X0) + (float)window.L / 2f;
						int num3 = item.Y + window.Y0 + window.H;
						if (num > 0.02f && Inside(num2, num3, worldViewport, 40f))
						{
							Graphics.AddLight((int)num2, (int)((float)num3 - (float)window.H / 2f), 14 + window.L / 2, (byte)(90f * num), 0f, color);
							Graphics.AddLight((int)num2, num3 + 12, 26 + window.L / 2, (byte)(120f * num), 0f, color);
						}
					}
				}
				for (int j = 0; j < item2.Enseignes.Length; j++)
				{
					(short, short, short, short, byte, byte, byte) tuple = item2.Enseignes[j];
					float num4 = SignOn(item, j, lampesActuelles);
					float num5 = (float)(item.X + tuple.Item1) + (float)tuple.Item3 / 2f;
					float num6 = (float)(item.Y + tuple.Item2) + (float)tuple.Item4 / 2f;
					if (num4 > 0.02f && Inside(num5, num6, worldViewport, 60f) && !Fallen(item, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4))
					{
						Graphics.AddLight((int)num5, (int)num6, (int)((float)Math.Max(tuple.Item3, tuple.Item4) * 1.3f), (byte)(150f * num4), 0f, Color.FromArgb(255, tuple.Item5, tuple.Item6, tuple.Item7));
						Graphics.AddLight((int)num5, (int)(num6 + 20f), 50, (byte)(140f * num4), 0f, Color.FromArgb(255, tuple.Item5, tuple.Item6, tuple.Item7));
					}
				}
			}
			foreach (var item7 in HouseWindowsInView(worldViewport))
			{
				Meteo.Vitre2 item3 = item7.Window;
				float item4 = item7.X;
				float item5 = item7.Y;
				float num7 = WindowOn(item3, lampesActuelles, heureActuelle);
				if (num7 > 0.02f)
				{
					float num8 = item4 + (float)item3.X0 + (float)item3.L / 2f;
					float num9 = item5 + (float)item3.Y0 + (float)item3.H;
					Graphics.AddLight((int)num8, (int)(num9 - (float)item3.H / 2f), 14 + item3.L / 2, (byte)(90f * num7), 0f, color);
					Graphics.AddLight((int)num8, (int)(num9 + 12f), 26 + item3.L / 2, (byte)(120f * num7), 0f, color);
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static bool Fallen(MapInstance map, int x0, int y0, int width, int height)
	{
		return Casse.TombeIci(map, x0, y0, width, height);
	}

	private static float WindowOn(Meteo.Vitre2 window, float lamps, float hour)
	{
		float num = Smooth((lamps - window.Seuil) / 0.12f);
		if (window.Extinction > 0f && hour < 5.5f)
		{
			num *= 1f - Smooth((hour - window.Extinction) / 0.25f);
		}
		return num;
	}

	private static float SignOn(MapInstance map, int index, float lamps)
	{
		float num = Smooth((lamps - 0.3f) / 0.2f);
		if (num <= 0f)
		{
			return 0f;
		}
		long milliseconds = Timing.Global.Milliseconds;
		(Guid, int) key = (map.Id, index);
		if (!Flickers.TryGetValue(key, out (long, long) value))
		{
			value = (0L, milliseconds + Noise.Next(1500, 5000));
		}
		if (milliseconds >= value.Item2)
		{
			value = (milliseconds + 260, milliseconds + Noise.Next(2000, 6000));
		}
		Flickers[key] = value;
		if (milliseconds >= value.Item1 || (value.Item1 - milliseconds) / 60 % 2 != 0L)
		{
			return num;
		}
		return num * 0.15f;
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
			float dt = ((_lastUpdate == 0L) ? 0.016f : Math.Clamp((float)(milliseconds - _lastUpdate) / 1000f, 0f, 0.05f));
			_lastUpdate = milliseconds;
			Update(milliseconds, dt);
			if (!_active || Graphics.Renderer?.WhitePixel == null)
			{
				return;
			}
			DrawWindows(upper: false);
			foreach (Car car in Cars)
			{
				DrawCarShadow(car);
			}
			if (Meteo.LampesActuelles < 0.6f && !Meteo.MauvaisTemps)
			{
				foreach (Pigeon[] value in Flocks.Values)
				{
					Pigeon[] array = value;
					foreach (Pigeon pigeon in array)
					{
						if (pigeon.Mode == 0)
						{
							Dot(pigeon.X - 3f, pigeon.Y + 1f, 6f, 1f, new Color(38, 0, 0, 0));
							DrawSprite(((milliseconds + (long)(pigeon.Phase * 1000f)) / 260 % 5 < 2) ? PigeonPeck : PigeonStand, PigeonColors, pigeon.X, pigeon.Y, pigeon.Dir);
						}
					}
				}
			}
			foreach (Dust dust in Dusts)
			{
				float num = (float)(milliseconds - dust.Born) / 1000f;
				Dot(dust.X + dust.Vx * num, dust.Y + dust.Vy * num + 60f * num * num, 1f, 1f, new Color((int)(150f * (1f - (float)(milliseconds - dust.Born) / 500f)), 150, 116, 70));
			}
			if (!(Meteo.LampesActuelles < 0.5f))
			{
				return;
			}
			foreach (Hen[] value2 in Coops.Values)
			{
				Hen[] array2 = value2;
				foreach (Hen hen in array2)
				{
					Dot(hen.X - 3f, hen.Y + 1f, 6f, 1f, new Color(46, 0, 0, 0));
					string[] shape = ((hen.Mode == 1) ? HenFlap : ((milliseconds < hen.ScratchUntil || (milliseconds + (long)(hen.HomeX * 13f)) / 300 % 4 == 0L) ? HenPeck : HenStand));
					float num2 = ((hen.Mode == 1) ? (MathF.Abs(MathF.Sin((float)(milliseconds - hen.ModeAt) / 80f)) * 3f) : 0f);
					DrawSprite(shape, hen.White ? WhiteHen : RedHen, hen.X, hen.Y - num2, hen.Dir);
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
		if (_broken || !_active || Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		try
		{
			DrawWindows(upper: true);
			long milliseconds = Timing.Global.Milliseconds;
			if (Meteo.LampesActuelles < 0.6f && !Meteo.MauvaisTemps)
			{
				foreach (Pigeon[] value in Flocks.Values)
				{
					Pigeon[] array = value;
					foreach (Pigeon pigeon in array)
					{
						if (pigeon.Mode != 0)
						{
							if (pigeon.Mode != 2)
							{
								Dot(pigeon.X - 3f, pigeon.Y + pigeon.Z * 0.3f + 1f, 6f, 1f, new Color(26, 0, 0, 0));
							}
							string[] shape = ((pigeon.Mode == 2) ? PigeonStand : (((milliseconds - pigeon.ModeAt) / 70 % 2 == 0L) ? PigeonWingsUp : PigeonWingsDown));
							DrawSprite(shape, PigeonColors, pigeon.X, pigeon.Y - pigeon.Z, pigeon.Dir);
						}
					}
				}
			}
			bool flag = Meteo.LampesActuelles > 0.5f;
			foreach (Puff puff in Puffs)
			{
				float num = (float)(milliseconds - puff.Born) / (float)puff.LifeMs;
				float num2 = (float)(milliseconds - puff.Born) / 1000f;
				float num3 = 0.6f + Meteo.ForceDuVent;
				float x = puff.X + -0.99f * (6f * num2 + 9f * num2 * num2) * num3 + MathF.Sin(num2 * 2.2f + puff.Phase) * 2f;
				float x2 = puff.Y - 16f * num2 + 2f * num2 * num2;
				int num4 = Math.Clamp((int)MathF.Round(2f + 7f * num), 2, 9);
				float num5 = (flag ? 0.3f : 0.55f) * ((num < 0.08f) ? (num / 0.08f) : (1f - Smooth((num - 0.3f) / 0.7f)));
				IGameTexture gameTexture = SmokeDisc(num4);
				if (gameTexture != null && num5 > 0.01f)
				{
					Color renderColor = (flag ? new Color((int)(255f * num5), 150, 156, 170) : new Color((int)(255f * num5), 232, 232, 228));
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(MathF.Round(x) - (float)num4, MathF.Round(x2) - (float)num4, gameTexture.Width, gameTexture.Height), renderColor);
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public static void DrawAfterDark()
	{
		if (_broken || !_active || Meteo.LampesActuelles <= 0.6f)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			float lampesActuelles = Meteo.LampesActuelles;
			float heureActuelle = Meteo.HeureActuelle;
			FloatRect worldViewport = Graphics.WorldViewport;
			List<(float, float, float)> list = new List<(float, float, float)>();
			foreach (var item6 in Visible)
			{
				MapInstance item = item6.Map;
				Meteo.DonneesVille item2 = item6.Ville;
				Meteo.Vitre2[] vitres = item2.Vitres;
				for (int i = 0; i < vitres.Length; i++)
				{
					Meteo.Vitre2 window = vitres[i];
					float num = (float)(item.X + window.X0) + (float)window.L / 2f;
					float num2 = (float)(item.Y + window.Y0) + (float)window.H / 2f;
					float num3 = WindowOn(window, lampesActuelles, heureActuelle);
					if (num3 > 0.3f && Inside(num, num2, worldViewport, 0f) && !Fallen(item, window.X0, window.Y0, window.L, window.H))
					{
						list.Add((num, num2, num3));
					}
				}
				for (int j = 0; j < item2.Enseignes.Length; j++)
				{
					(short, short, short, short, byte, byte, byte) tuple = item2.Enseignes[j];
					float num4 = (float)(item.X + tuple.Item1) + (float)tuple.Item3 / 2f;
					float num5 = (float)(item.Y + tuple.Item2) + (float)tuple.Item4 / 2f;
					if (Inside(num4, num5, worldViewport, 0f) && !Fallen(item, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4))
					{
						list.Add((num4, num5, 1f));
					}
				}
			}
			for (int k = 0; k < Math.Min(8, list.Count); k++)
			{
				(float, float, float) tuple2 = list[k * 7919 % list.Count];
				float item3 = tuple2.Item1;
				float item4 = tuple2.Item2;
				float item5 = tuple2.Item3;
				float num6 = (float)milliseconds / 380f + (float)k * 1.7f;
				Dot(item3 + MathF.Cos(num6 * 1.7f) * 7f + MathF.Sin(num6 * 3.1f) * 3f, item4 + MathF.Sin(num6 * 1.3f) * 5f, 1f, 1f, new Color((int)(204f * item5), 240, 236, 220));
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static void DrawWindows(bool upper)
	{
		long milliseconds = Timing.Global.Milliseconds;
		float lampesActuelles = Meteo.LampesActuelles;
		float heureActuelle = Meteo.HeureActuelle;
		FloatRect worldViewport = Graphics.WorldViewport;
		float sweep = ((lampesActuelles < 0.3f && !Meteo.MauvaisTemps) ? ((float)milliseconds / 9f % 420f - 60f) : float.NaN);
		foreach (var item3 in Visible)
		{
			MapInstance item = item3.Map;
			Meteo.DonneesVille item2 = item3.Ville;
			IGameTexture texVitres = item2.TexVitres;
			if (texVitres == null)
			{
				continue;
			}
			Meteo.Vitre2[] vitres = item2.Vitres;
			for (int i = 0; i < vitres.Length; i++)
			{
				Meteo.Vitre2 window = vitres[i];
				if (window.Haut == upper && !Fallen(item, window.X0, window.Y0, window.L, window.H))
				{
					DrawWindow(texVitres, window, item.X + window.X0, item.Y + window.Y0, window.X0, window.Y0, lampesActuelles, heureActuelle, milliseconds, sweep, worldViewport);
				}
			}
		}
	}

	private static void DrawWindow(IGameTexture mask, Meteo.Vitre2 window, float x, float y, int sourceX, int sourceY, float lamps, float hour, long now, float sweep, FloatRect view)
	{
		if (!Inside(x + (float)window.L / 2f, y + (float)window.H / 2f, view, 60f))
		{
			return;
		}
		float num = WindowOn(window, lamps, hour);
		if (num > 0.02f)
		{
			float num2 = 0.93f + 0.07f * MathF.Sin((float)now / 90f + (float)window.X0) * MathF.Sin((float)now / 270f + (float)window.Y0);
			Graphics.DrawGameTexture(mask, new FloatRect(sourceX, sourceY, window.L, window.H), new FloatRect(x, y, window.L, window.H), new Color((int)(216.75f * num * num2), 255, 205, 122));
		}
		else if (!float.IsNaN(sweep))
		{
			float num3 = view.Left + sweep * view.Width / 480f - (y + (float)window.H / 2f - view.Top) * 0.6f;
			int num4 = (int)(num3 - x);
			if (num4 >= 0 && num4 < window.L - 1)
			{
				int num5 = Math.Min(3, window.L - num4);
				Graphics.DrawGameTexture(mask, new FloatRect(sourceX + num4, sourceY, num5, window.H), new FloatRect(x + (float)num4, y, num5, window.H), new Color(140, 255, 255, 255), null, GameBlendModes.Add);
			}
		}
	}

	public static void DrawLightMap()
	{
		if (!_active || _broken)
		{
			return;
		}
		try
		{
			float lampesActuelles = Meteo.LampesActuelles;
			if (lampesActuelles < 0.1f)
			{
				return;
			}
			float heureActuelle = Meteo.HeureActuelle;
			FloatRect worldViewport = Graphics.WorldViewport;
			foreach (var item7 in Visible)
			{
				MapInstance item = item7.Map;
				Meteo.DonneesVille item2 = item7.Ville;
				IGameTexture texVitres = item2.TexVitres;
				if (texVitres != null)
				{
					Meteo.Vitre2[] vitres = item2.Vitres;
					for (int i = 0; i < vitres.Length; i++)
					{
						Meteo.Vitre2 window = vitres[i];
						float num = WindowOn(window, lampesActuelles, heureActuelle);
						float num2 = item.X + window.X0;
						float num3 = item.Y + window.Y0;
						if (num > 0.02f && Inside(num2 + (float)window.L / 2f, num3 + (float)window.H / 2f, worldViewport, 40f) && !Fallen(item, window.X0, window.Y0, window.L, window.H))
						{
							Graphics.AddLightTexture(texVitres, new FloatRect(window.X0, window.Y0, window.L, window.H), new FloatRect(num2, num3, window.L, window.H), new Color((int)(255f * num), 255, 236, 200));
						}
					}
				}
				IGameTexture texEnseignes = item2.TexEnseignes;
				if (texEnseignes == null)
				{
					continue;
				}
				for (int j = 0; j < item2.Enseignes.Length; j++)
				{
					(short, short, short, short, byte, byte, byte) tuple = item2.Enseignes[j];
					float num4 = SignOn(item, j, lampesActuelles);
					float num5 = item.X + tuple.Item1;
					float num6 = item.Y + tuple.Item2;
					if (num4 > 0.02f && Inside(num5 + (float)tuple.Item3 / 2f, num6 + (float)tuple.Item4 / 2f, worldViewport, 40f) && !Fallen(item, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4))
					{
						Graphics.AddLightTexture(texEnseignes, new FloatRect(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4), new FloatRect(num5, num6, tuple.Item3, tuple.Item4), new Color((int)(255f * num4), 255, 255, 255));
					}
				}
			}
			foreach (var item8 in HouseWindowsInView(worldViewport))
			{
				IGameTexture item3 = item8.Mask;
				Meteo.Vitre2 item4 = item8.Window;
				float item5 = item8.X;
				float item6 = item8.Y;
				float num7 = WindowOn(item4, lampesActuelles, heureActuelle);
				if (num7 > 0.02f)
				{
					Graphics.AddLightTexture(item3, new FloatRect(item4.X0, item4.Y0, item4.L, item4.H), new FloatRect(item5 + (float)item4.X0, item6 + (float)item4.Y0, item4.L, item4.H), new Color((int)(255f * num7), 255, 236, 200));
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static (Meteo.Vitre2[] Windows, IGameTexture? Mask) WindowsOfHouse(IGameTexture house)
	{
		if (HouseWindows.TryGetValue(house, out (Meteo.Vitre2[], IGameTexture) value))
		{
			return value;
		}
		(Meteo.Vitre2[], IGameTexture) tuple = (Array.Empty<Meteo.Vitre2>(), null);
		(Meteo.Vitre2[], byte[], int, int)? tuple2 = Meteo.FenetresDImage(house);
		if (tuple2.HasValue)
		{
			(Meteo.Vitre2[], byte[], int, int) valueOrDefault = tuple2.GetValueOrDefault();
			if (valueOrDefault.Item1.Length != 0 && Graphics.Renderer is MonoRenderer monoRenderer)
			{
				tuple = (valueOrDefault.Item1, monoRenderer.CreerTexturePixels("ville/maison/" + house.Name, valueOrDefault.Item3, valueOrDefault.Item4, valueOrDefault.Item2));
			}
		}
		HouseWindows[house] = tuple;
		return tuple;
	}

	private static IEnumerable<(IGameTexture Mask, Meteo.Vitre2 Window, float X, float Y)> HouseWindowsInView(FloatRect view)
	{
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		foreach (MapInstance map in Meteo.CartesVisibles())
		{
			foreach (Houses.Plot item2 in Houses.PlotsOf(map))
			{
				CapsuleDescriptor capsule = item2.Capsule;
				if (capsule == null)
				{
					continue;
				}
				IGameTexture gameTexture = HouseWorld.CapsuleTexture(capsule);
				if (gameTexture == null)
				{
					continue;
				}
				int num = map.Y + (item2.Footprint.Y + item2.Footprint.Height) * tileHeight;
				float left = MathF.Round((float)map.X + ((float)item2.Footprint.X + (float)item2.Footprint.Width / 2f) * (float)tileWidth - (float)gameTexture.Width / 2f);
				int top = num - gameTexture.Height;
				if (left > view.Right || left + (float)gameTexture.Width < view.Left || (float)top > view.Bottom || (float)num < view.Top)
				{
					continue;
				}
				var (array, mask) = WindowsOfHouse(gameTexture);
				if (mask != null)
				{
					Meteo.Vitre2[] array2 = array;
					foreach (Meteo.Vitre2 item in array2)
					{
						yield return (Mask: mask, Window: item, X: left, Y: top);
					}
				}
			}
		}
	}

	public static void DrawHouseWindows(IGameTexture house, float x, float y)
	{
		if (_broken || !_active)
		{
			return;
		}
		try
		{
			var (array, gameTexture) = WindowsOfHouse(house);
			if (gameTexture != null)
			{
				long milliseconds = Timing.Global.Milliseconds;
				float lampesActuelles = Meteo.LampesActuelles;
				float sweep = ((lampesActuelles < 0.3f && !Meteo.MauvaisTemps) ? ((float)milliseconds / 9f % 420f - 60f) : float.NaN);
				Meteo.Vitre2[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					Meteo.Vitre2 window = array2[i];
					DrawWindow(gameTexture, window, x + (float)window.X0, y + (float)window.Y0, window.X0, window.Y0, lampesActuelles, Meteo.HeureActuelle, milliseconds, sweep, Graphics.WorldViewport);
				}
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	private static void Update(long now, float dt)
	{
		Player me = Globals.Me;
		_active = me != null && Meteo.Dehors;
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
			Meteo.GenreZone genreZone = Meteo.ZoneDe(item);
			if ((genreZone <= Meteo.GenreZone.Ile || genreZone == Meteo.GenreZone.Foret) ? true : false)
			{
				Meteo.DonneesVille donneesVille = Meteo.VilleDe(item);
				if (donneesVille != null)
				{
					Visible.Add((item, donneesVille));
				}
			}
		}
		if (Visible.Count == 0)
		{
			return;
		}
		CollectWalkers();
		FloatRect worldViewport = Graphics.WorldViewport;
		float lampesActuelles = Meteo.LampesActuelles;
		UpdatePigeons(now, lampesActuelles < 0.6f && !Meteo.MauvaisTemps);
		UpdateCars(now, dt, worldViewport, lampesActuelles < 0.7f);
		UpdateSmoke(now, worldViewport, lampesActuelles);
		UpdateHens(now, dt, lampesActuelles < 0.5f);
		Dusts.RemoveAll((Dust dust) => now - dust.Born > 500);
		if (now < _reportAt)
		{
			return;
		}
		_reportAt = now + 60000;
		foreach (var (mapInstance, donneesVille2) in Visible)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Ville {Carte} : urbaine {Urbaine}, {Routes} routes, {Vitres} fenetres, {Enseignes} enseignes, {Tuyaux} tuyaux, {Pigeons} places de pigeons, {Poules} basses-cours", mapInstance.Name, donneesVille2.Urbaine, donneesVille2.Routes.Length, donneesVille2.Vitres.Length, donneesVille2.Enseignes.Length, donneesVille2.Tuyaux.Length, donneesVille2.Pigeons.Length, donneesVille2.Poules.Length);
		}
	}

	private static void Clear()
	{
		Flocks.Clear();
		Coops.Clear();
		NextPuffAt.Clear();
		Cars.Clear();
		Puffs.Clear();
		Dusts.Clear();
		Visible.Clear();
	}

	private static void Fail(Exception exception)
	{
		_broken = true;
		Clear();
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Villes vivantes coupees apres un incident");
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
			if (value != null && !value.IsHidden && (value.Type == EntityType.Player || value.Type == EntityType.GlobalEntity) && !(value is Critter) && !Flight.IsAirborne(value))
			{
				Vector2 origin = value.Origin;
				Walkers.Add((origin.X, origin.Y, value.IsMoving));
			}
		}
	}

	private static void UpdatePigeons(long now, bool allowed)
	{
		if (!allowed)
		{
			Flocks.Clear();
			return;
		}
		foreach (var item2 in Visible)
		{
			MapInstance map = item2.Map;
			Meteo.DonneesVille item = item2.Ville;
			for (int i = 0; i < item.Pigeons.Length; i++)
			{
				(Guid, int) key = (map.Id, i);
				if (!Flocks.ContainsKey(key))
				{
					var (ax, ay) = item.Pigeons[i];
					Flocks[key] = Enumerable.Range(0, 5 + Noise.Next(2)).Select((Func<int, Pigeon>)delegate
					{
						float num6 = (float)(map.X + ax) + Between(-50f, 50f);
						float num7 = (float)(map.Y + ay) + Between(-10f, 10f);
						return new Pigeon
						{
							HomeX = num6,
							HomeY = num7,
							X = num6,
							Y = num7,
							Dir = ((Noise.Next(2) != 0) ? 1 : (-1)),
							Phase = Between(0f, 6f),
							RoofX = num6 + Between(-30f, 30f),
							RoofY = Math.Max(map.Y + 8, num7 - Between(60f, 110f)),
							NextStepAt = now + Noise.Next(800, 2400)
						};
					}).ToArray();
				}
			}
		}
		foreach (Pigeon[] value in Flocks.Values)
		{
			Pigeon[] array = value;
			foreach (Pigeon pigeon in array)
			{
				float num2 = ((Walkers.Count == 0) ? float.MaxValue : Walkers.Min(((float X, float Y, bool Moving) w) => Distance(pigeon.X - w.X, (pigeon.Y - w.Y) * 1.4f)));
				float num3 = ((Walkers.Count == 0) ? float.MaxValue : Walkers.Min(((float X, float Y, bool Moving) w) => Distance(pigeon.HomeX - w.X, (pigeon.HomeY - w.Y) * 1.4f)));
				switch (pigeon.Mode)
				{
				case 0:
					if (num2 < 58f)
					{
						TakeOff(pigeon, now);
					}
					else if (now >= pigeon.NextStepAt)
					{
						pigeon.NextStepAt = now + Noise.Next(800, 2400);
						pigeon.X += pigeon.Dir * 2;
						if (Noise.NextDouble() < 0.3)
						{
							pigeon.Dir = -pigeon.Dir;
						}
					}
					break;
				case 1:
				{
					float num5 = Math.Min(1f, (float)(now - pigeon.ModeAt) / 900f);
					pigeon.X = pigeon.FromX + (pigeon.RoofX - pigeon.FromX) * Smooth(num5);
					pigeon.Y = pigeon.FromY + (pigeon.RoofY - pigeon.FromY) * Smooth(num5);
					pigeon.Z = MathF.Sin((float)Math.PI * num5) * 26f;
					if (num5 >= 1f)
					{
						pigeon.Mode = 2;
						pigeon.Z = 0f;
						pigeon.CalmSince = now;
					}
					break;
				}
				case 2:
					if (num3 < 90f)
					{
						pigeon.CalmSince = now;
					}
					else if (now - pigeon.CalmSince > 2600)
					{
						pigeon.Mode = 3;
						pigeon.ModeAt = now;
						pigeon.Dir = ((pigeon.HomeX > pigeon.X) ? 1 : (-1));
					}
					break;
				default:
				{
					float num4 = Math.Min(1f, (float)(now - pigeon.ModeAt) / 1100f);
					pigeon.X = pigeon.RoofX + (pigeon.HomeX - pigeon.RoofX) * Smooth(num4);
					pigeon.Y = pigeon.RoofY + (pigeon.HomeY - pigeon.RoofY) * Smooth(num4);
					pigeon.Z = MathF.Sin((float)Math.PI * num4) * 18f;
					if (num4 >= 1f)
					{
						pigeon.Mode = 0;
						pigeon.Z = 0f;
					}
					break;
				}
				}
			}
		}
	}

	private static void TakeOff(Pigeon pigeon, long now)
	{
		pigeon.Mode = 1;
		pigeon.ModeAt = now;
		pigeon.FromX = pigeon.X;
		pigeon.FromY = pigeon.Y;
		pigeon.Dir = ((pigeon.RoofX > pigeon.X) ? 1 : (-1));
	}

	private static void UpdateCars(long now, float dt, FloatRect view, bool allowed)
	{
		if (allowed && now >= _nextCarAt)
		{
			_nextCarAt = now + Noise.Next(1800, 3800);
			List<float> list = new List<float>();
			foreach (var item3 in Visible)
			{
				MapInstance item = item3.Map;
				Meteo.DonneesVille item2 = item3.Ville;
				(short, short, bool)[] routes = item2.Routes;
				for (int i = 0; i < routes.Length; i++)
				{
					(short, short, bool) tuple = routes[i];
					var (num, num2, _) = tuple;
					if (tuple.Item3 && (float)(item.Y + num2) >= view.Top && (float)(item.Y + num) <= view.Bottom)
					{
						list.Add((float)(item.Y + num) + (float)(num2 - num) * 0.3f);
						list.Add((float)(item.Y + num) + (float)(num2 - num) * 0.72f);
					}
				}
			}
			if (list.Count > 0 && Cars.Count < 6)
			{
				int num3 = Noise.Next(list.Count);
				float y = list[num3];
				bool flag = num3 % 2 == 1;
				Cars.Add(new Car
				{
					X = (flag ? (view.Left - 40f) : (view.Right + 40f)),
					Y = y,
					Speed = (float)(flag ? 1 : (-1)) * Between(150f, 210f),
					Length = Between(24f, 30f)
				});
			}
		}
		for (int num4 = Cars.Count - 1; num4 >= 0; num4--)
		{
			Car car = Cars[num4];
			car.X += car.Speed * dt;
			if (car.X < view.Left - 80f || car.X > view.Right + 80f)
			{
				Cars.RemoveAt(num4);
			}
		}
	}

	private static void UpdateSmoke(long now, FloatRect view, float lamps)
	{
		float heureActuelle = Meteo.HeureActuelle;
		float num = 1f + 0.8f * (Meteo.Cloche(heureActuelle, 6.5f, 2f) + Meteo.Cloche(heureActuelle, 19.5f, 2.5f));
		long num2 = (long)((float)((lamps > 0.5f) ? 260 : 140) / num);
		foreach (var item5 in Visible)
		{
			MapInstance item = item5.Map;
			Meteo.DonneesVille item2 = item5.Ville;
			for (int i = 0; i < item2.Tuyaux.Length; i++)
			{
				(short X, short Y) tuple = item2.Tuyaux[i];
				short item3 = tuple.X;
				short item4 = tuple.Y;
				float num3 = item.X + item3;
				float y = item.Y + item4;
				if (!Inside(num3, y, view, 80f))
				{
					continue;
				}
				(Guid, int) key = (item.Id, i);
				if (!NextPuffAt.TryGetValue(key, out var value) || now >= value)
				{
					NextPuffAt[key] = now + num2;
					if (Puffs.Count < 160)
					{
						Puffs.Add(new Puff
						{
							X = num3 + Between(-1f, 1f),
							Y = y,
							Born = now,
							LifeMs = Noise.Next(3000, 3800),
							Phase = Between(0f, 6f)
						});
					}
				}
			}
		}
		Puffs.RemoveAll((Puff puff) => now - puff.Born > puff.LifeMs);
	}

	private static void UpdateHens(long now, float dt, bool allowed)
	{
		if (!allowed)
		{
			Coops.Clear();
			return;
		}
		foreach (var item2 in Visible)
		{
			MapInstance map = item2.Map;
			Meteo.DonneesVille item = item2.Ville;
			for (int i = 0; i < item.Poules.Length; i++)
			{
				(Guid, int) key = (map.Id, i);
				if (!Coops.ContainsKey(key))
				{
					var (ax, ay) = item.Poules[i];
					Coops[key] = Enumerable.Range(0, 3 + Noise.Next(2)).Select(delegate(int num9)
					{
						float num7 = (float)(map.X + ax) + Between(-30f, 30f);
						float num8 = (float)(map.Y + ay) + Between(-16f, 16f);
						return new Hen
						{
							HomeX = num7,
							HomeY = num8,
							X = num7,
							Y = num8,
							White = (num9 % 2 == 0),
							Dir = ((Noise.Next(2) != 0) ? 1 : (-1)),
							NextActAt = now + Noise.Next(600, 2000)
						};
					}).ToArray();
				}
			}
		}
		foreach (Hen[] value in Coops.Values)
		{
			Hen[] array = value;
			foreach (Hen hen in array)
			{
				(float, float, float) tuple2 = ((Walkers.Count == 0) ? (float.MaxValue, 0f, 0f) : Walkers.Select(((float X, float Y, bool Moving) w) => (Distance(hen.X - w.X, (hen.Y - w.Y) * 1.3f), X: w.X, Y: w.Y)).Min());
				switch (hen.Mode)
				{
				case 0:
					if (tuple2.Item1 < 50f)
					{
						Bolt(hen, tuple2.Item2, tuple2.Item3, now);
					}
					else
					{
						if (now < hen.NextActAt)
						{
							continue;
						}
						hen.NextActAt = now + Noise.Next(900, 2400);
						if (Noise.NextDouble() < 0.45)
						{
							hen.ScratchUntil = now + 400;
							for (int num2 = 0; num2 < 4; num2++)
							{
								if (Dusts.Count >= 60)
								{
									break;
								}
								Dusts.Add(new Dust
								{
									X = hen.X - (float)(hen.Dir * 3),
									Y = hen.Y,
									Vx = (float)(-hen.Dir) * Between(10f, 30f),
									Vy = 0f - Between(6f, 20f),
									Born = now
								});
							}
						}
						else
						{
							hen.X += hen.Dir * 3;
							if (Noise.NextDouble() < 0.4)
							{
								hen.Dir = -hen.Dir;
							}
						}
					}
					continue;
				case 1:
					hen.X += hen.Vx * dt;
					hen.Y += hen.Vy * dt;
					hen.Vx *= 1f - 2.2f * dt;
					hen.Vy *= 1f - 2.2f * dt;
					if (now - hen.ModeAt > 900)
					{
						hen.Mode = 2;
						hen.ModeAt = now;
					}
					continue;
				}
				float num3 = ((Walkers.Count == 0) ? float.MaxValue : Walkers.Min(((float X, float Y, bool Moving) w) => Distance(hen.HomeX - w.X, (hen.HomeY - w.Y) * 1.3f)));
				if (num3 > 80f && now - hen.ModeAt > 1500)
				{
					float num4 = hen.HomeX - hen.X;
					float num5 = hen.HomeY - hen.Y;
					float num6 = Distance(num4, num5);
					if (num6 < 1.5f)
					{
						hen.Mode = 0;
						continue;
					}
					hen.X += num4 / num6 * 28f * dt;
					hen.Y += num5 / num6 * 28f * dt;
					hen.Dir = ((num4 > 0f) ? 1 : (-1));
				}
			}
		}
	}

	private static void Bolt(Hen hen, float fromX, float fromY, long now)
	{
		hen.Mode = 1;
		hen.ModeAt = now;
		float x = MathF.Atan2(hen.Y - fromY, hen.X - fromX) + Between(-0.5f, 0.5f);
		hen.Vx = MathF.Cos(x) * 95f;
		hen.Vy = MathF.Sin(x) * 60f;
		hen.Dir = ((hen.Vx > 0f) ? 1 : (-1));
	}

	private static void DrawCarShadow(Car car)
	{
		float num = car.Length / 2f;
		for (int i = -4; i <= 4; i++)
		{
			float num2 = num * MathF.Sqrt(1f - (float)(i * i) / 20.25f);
			Dot(car.X - num2, car.Y + (float)i, num2 * 2f, 1f, new Color((Math.Abs(i) >= 4) ? 20 : 51, 30, 40, 60));
		}
	}

	private static void DrawSprite(string[] shape, Dictionary<char, Color> colors, float x, float y, int dir)
	{
		int length = shape[0].Length;
		float num = MathF.Round(x);
		float num2 = MathF.Round(y) - (float)shape.Length + 1f;
		for (int i = 0; i < shape.Length; i++)
		{
			for (int j = 0; j < length; j++)
			{
				char c = shape[i][j];
				if (c != '.' && colors.TryGetValue(c, out Color value))
				{
					Dot(num + (float)((dir > 0) ? (j - (length >> 1)) : ((length >> 1) - j)), num2 + (float)i, 1f, 1f, value);
				}
			}
		}
	}

	private static IGameTexture? SmokeDisc(int radius)
	{
		IGameTexture gameTexture = SmokeDiscs[radius];
		if (gameTexture != null)
		{
			return gameTexture;
		}
		if (!(Graphics.Renderer is MonoRenderer monoRenderer))
		{
			return null;
		}
		int num = radius * 2 + 1;
		byte[] array = new byte[num * num * 4];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num2 = Distance(j - radius, i - radius) / (float)radius;
				if (!(num2 > 1f) && !((float)Bayer[(i & 3) * 4 + (j & 3)] / 16f > (1f - num2) * 1.2f))
				{
					int num3 = (i * num + j) * 4;
					int num4 = num3 + 1;
					int num5 = num3 + 2;
					byte b;
					array[num3 + 3] = (b = byte.MaxValue);
					array[num3] = (array[num4] = (array[num5] = b));
				}
			}
		}
		SmokeDiscs[radius] = monoRenderer.CreerTexturePixels($"ville/fumee/{radius}", num, num, array);
		return SmokeDiscs[radius];
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
