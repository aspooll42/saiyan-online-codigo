using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public static class DesertLife
{
	private sealed class Stream
	{
		public float X;

		public float Y;

		public float Speed;

		public float Phase;

		public long Born;

		public long LastSampleAt;

		public int LifeMs;

		public readonly float[] Xs = new float[30];

		public readonly float[] Ys = new float[30];

		public int Count;

		public int Head;
	}

	private sealed class Grain
	{
		public int Kind;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Z;

		public float Vz;

		public long Born;

		public long LandedAt = -1L;

		public int LifeMs;

		public int Size = 1;

		public Color Color = Intersect.Color.White;
	}

	private sealed class Puff
	{
		public float X;

		public float Y;

		public float Vx;

		public float Radius;

		public long Born;
	}

	private sealed class Glint
	{
		public float X;

		public float Y;

		public long Born;

		public int LifeMs;

		public bool Cross;
	}

	private sealed class Tumbleweed
	{
		public float X;

		public float Y;

		public float Angle;

		public float Phase;

		public float Speed;

		public long StuckUntil;

		public long HopUntil;
	}

	private sealed class Vulture
	{
		public float Angle;

		public float Radius;

		public float Speed;

		public long FlapAt;
	}

	private sealed class Lizard
	{
		public float HomeX;

		public float HomeY;

		public float HideX;

		public float HideY;

		public float X;

		public float Y;

		public float FromX;

		public float FromY;

		public int Mode;

		public int Dir = 1;

		public long ModeAt;

		public long PushUpAt;

		public long CalmSince;

		public int DurationMs;
	}

	private sealed class Crater
	{
		public float X;

		public float Y;

		public float Rx;

		public float Ry;

		public long Born;

		public bool Big;

		public (float Dx, float Dy, Color Color, int Size, int GlintOffset)[] Glass = Array.Empty<(float, float, Color, int, int)>();
	}

	private const int TrailLength = 30;

	private static readonly List<Stream> Streams = new List<Stream>();

	private static readonly List<Grain> Grains = new List<Grain>();

	private static readonly List<Puff> Puffs = new List<Puff>();

	private static readonly List<Glint> Glints = new List<Glint>();

	private static readonly List<Tumbleweed> Tumbleweeds = new List<Tumbleweed>();

	private static readonly Dictionary<(Guid Map, int Index), Vulture[]> Vultures = new Dictionary<(Guid, int), Vulture[]>();

	private static readonly Dictionary<(Guid Map, int Index), Lizard> Lizards = new Dictionary<(Guid, int), Lizard>();

	private static readonly List<Crater> Craters = new List<Crater>();

	private static readonly List<(MapInstance Map, Meteo.DonneesDesert Desert)> Visible = new List<(MapInstance, Meteo.DonneesDesert)>();

	private static readonly List<(float X, float Y, bool Moving)> Walkers = new List<(float, float, bool)>();

	private static readonly Dictionary<Guid, long> LastKickAt = new Dictionary<Guid, long>();

	private static readonly Random Noise = new Random();

	private static readonly (float X, float Y, int Tint)[] Twigs = BuildTwigs();

	private static readonly SuiviGrille Grille = new SuiviGrille();

	private static bool _active;

	private static bool _broken;

	private static long _lastUpdate;

	private static long _nextTumbleweedAt;

	private static long _reportAt;

	private static readonly Color[] SprayColors = new Color[3]
	{
		new Color(255, 196, 164, 116),
		new Color(255, 252, 240, 214),
		new Color(255, 218, 188, 140)
	};

	private static readonly Color[] GlassColors = new Color[4]
	{
		new Color(255, 226, 184, 92),
		new Color(255, 160, 196, 128),
		new Color(255, 96, 118, 80),
		new Color(255, 236, 226, 170)
	};

	private static readonly Color[] TwigColors = new Color[3]
	{
		new Color(255, 170, 132, 80),
		new Color(255, 138, 102, 60),
		new Color(255, 204, 170, 112)
	};

	private static readonly string[][] LizardFrames = new string[2][]
	{
		new string[4] { "..l....l.", "tttbBBbhh", "...bbbbhe", ".l....l.." },
		new string[4] { ".l....l..", "tttbBBbhh", "...bbbbhe", "..l....l." }
	};

	private static readonly Dictionary<char, Color> LizardColors = new Dictionary<char, Color>
	{
		['t'] = new Color(255, 170, 128, 78),
		['b'] = new Color(255, 196, 152, 96),
		['B'] = new Color(255, 150, 108, 64),
		['h'] = new Color(255, 212, 168, 108),
		['e'] = new Color(255, 40, 28, 18),
		['l'] = new Color(255, 150, 110, 66)
	};

	private static readonly (int X, int Y)[] VultureOpen = BuildVulture(8);

	private static readonly (int X, int Y)[] VultureFlap = BuildVulture(5);

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	private static float Smooth(float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		return k * k * (3f - 2f * k);
	}

	private static (float X, float Y, int Tint)[] BuildTwigs()
	{
		Random random = new Random(761);
		List<(float, float, int)> list = new List<(float, float, int)>();
		for (int i = 0; i < 9; i++)
		{
			float num = (float)random.NextDouble() * 5f - 2.5f;
			float num2 = (float)random.NextDouble() * 5f - 2.5f;
			float num3 = 2f + (float)random.NextDouble() * 2.6f;
			float num4 = (float)random.NextDouble() * 6f;
			float num5 = 1.6f + (float)random.NextDouble() * 1.6f;
			for (float num6 = 0f; num6 < num5; num6 += 0.3f)
			{
				float num7 = num + MathF.Cos(num4 + num6) * num3;
				float num8 = num2 + MathF.Sin(num4 + num6) * num3;
				if (num7 * num7 + num8 * num8 <= 33.640003f)
				{
					list.Add((num7, num8, i % 3));
				}
			}
		}
		return list.ToArray();
	}

	private static (int X, int Y)[] BuildVulture(int span)
	{
		List<(int, int)> list = new List<(int, int)>();
		for (int i = -7; i <= 7; i++)
		{
			list.Add((i, 0));
		}
		list.AddRange(new global::_003C_003Ez__ReadOnlyArray<(int, int)>(new(int, int)[4]
		{
			(-8, -1),
			(-8, 1),
			(-9, -1),
			(-9, 1)
		}));
		for (int j = 1; j <= span; j++)
		{
			int num = (int)MathF.Round(-2f + (float)j * 0.25f);
			int num2 = (int)MathF.Round(3f - (float)j * 0.4f);
			for (int k = num; k <= num2; k++)
			{
				list.Add((k, j));
				list.Add((k, -j));
			}
			if (j >= span - 1)
			{
				list.Add((num - 1, j));
				list.Add((num - 1, -j));
			}
		}
		return list.ToArray();
	}

	public static void OnKiImpact(float x, float y, bool big)
	{
		if (_broken || !Meteo.EstSable(x, y))
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			for (int i = 0; i < (big ? 110 : 46); i++)
			{
				if (Grains.Count >= 700)
				{
					break;
				}
				float x2 = Between(0f, (float)Math.PI * 2f);
				float num = Between(30f, big ? 160 : 110);
				Grains.Add(new Grain
				{
					Kind = 1,
					X = x + Between(-3f, 3f),
					Y = y + Between(-2f, 2f),
					Vx = MathF.Cos(x2) * num,
					Vy = MathF.Sin(x2) * num * 0.55f,
					Vz = Between(60f, big ? 190 : 150),
					Born = milliseconds,
					LifeMs = 3200,
					Size = ((!(Noise.NextDouble() < 0.3)) ? 1 : 2),
					Color = SprayColors[i % SprayColors.Length]
				});
			}
			Crater crater = new Crater
			{
				X = x,
				Y = y,
				Rx = (big ? 30 : 18),
				Ry = (big ? 15 : 9),
				Born = milliseconds,
				Big = big
			};
			if (big)
			{
				crater.Glass = Enumerable.Range(0, 36).Select(delegate(int num3)
				{
					float x3 = Between(0f, (float)Math.PI * 2f);
					float num2 = MathF.Sqrt((float)Noise.NextDouble()) * 0.62f;
					return (MathF.Cos(x3) * num2 * 30f, MathF.Sin(x3) * num2 * 15f, GlassColors[num3 % GlassColors.Length], (!(Noise.NextDouble() < 0.35)) ? 1 : 2, Noise.Next(0, 1400));
				}).ToArray();
			}
			if (Craters.Count >= 12)
			{
				Craters.RemoveAt(0);
			}
			Craters.Add(crater);
			ScareLizards(x, y, big ? 400f : 160f);
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
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
			if (Graphics.Renderer?.WhitePixel == null)
			{
				return;
			}
			DrawCraters(milliseconds);
			if (!_active)
			{
				return;
			}
			DrawStreams(milliseconds);
			DrawVultures(milliseconds);
			foreach (Lizard value in Lizards.Values)
			{
				DrawLizard(value, milliseconds);
			}
			foreach (Tumbleweed tumbleweed in Tumbleweeds)
			{
				DrawTumbleweed(tumbleweed, milliseconds);
			}
			foreach (Puff puff in Puffs)
			{
				float num = (float)(milliseconds - puff.Born) / 1600f;
				float num2 = MathF.Round(puff.Radius * (1f + num));
				Dot(puff.X - num2, puff.Y - num2, num2 * 2f, num2 * 2f, new Color((int)(82f * (1f - num)), 236, 218, 186));
			}
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public static void DrawAir()
	{
		if (_broken || Graphics.Renderer?.WhitePixel == null || Grains.Count == 0)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			float night = 1f - Meteo.JourActuel;
			foreach (Grain grain in Grains)
			{
				long num = milliseconds - grain.Born;
				if (grain.Kind == 0)
				{
					float num2 = (float)num / (float)grain.LifeMs;
					float num3 = (float)num / 1000f;
					float x = grain.X + grain.Vx * (1f - MathF.Exp((0f - num3) * 0.6f)) / 0.6f;
					float y = grain.Y + grain.Vy * num3 + 6f * num3 * num3;
					Dot(x, y, 1f, 1f, Faded(GrainOn(x, y, night), 0.7f * (1f - num2)));
				}
				else if (grain.LandedAt < 0)
				{
					Dot(grain.X, grain.Y, 1f, 1f, new Color(64, 120, 90, 50));
					Dot(grain.X, grain.Y - grain.Z, grain.Size, grain.Size, grain.Color);
				}
				else
				{
					float alpha = 1f - Smooth((float)(milliseconds - grain.LandedAt - 1500) / 1000f);
					Dot(grain.X, grain.Y, grain.Size, 1f, Faded(grain.Color, alpha));
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
		if (_broken || Graphics.Renderer?.WhitePixel == null)
		{
			return;
		}
		try
		{
			long milliseconds = Timing.Global.Milliseconds;
			foreach (Crater crater in Craters)
			{
				long num = milliseconds - crater.Born;
				if (num > 10000)
				{
					continue;
				}
				(float, float, Color, int, int)[] glass = crater.Glass;
				for (int i = 0; i < glass.Length; i++)
				{
					(float, float, Color, int, int) tuple = glass[i];
					float item = tuple.Item1;
					float item2 = tuple.Item2;
					int item3 = tuple.Item5;
					long num2 = (milliseconds + item3) % 1400;
					if (num2 < 160)
					{
						Dot(crater.X + item, crater.Y + item2, 1f, 1f, new Color((int)(230f * MathF.Sin((float)Math.PI * (float)num2 / 160f)), 255, 255, 235), GameBlendModes.Add);
					}
				}
			}
			if (!_active)
			{
				return;
			}
			float num3 = 1f - Meteo.JourActuel;
			foreach (Glint glint in Glints)
			{
				long num4 = milliseconds - glint.Born;
				if (num4 >= 0)
				{
					float num5 = (float)num4 / (float)glint.LifeMs;
					float num6 = MathF.Sin((float)Math.PI * num5) * ((num3 > 0.5f) ? 0.75f : 0.9f);
					Color color = ((num3 > 0.5f) ? new Color((int)(255f * num6), 190, 210, 255) : new Color((int)(255f * num6), 255, 255, 240));
					if (num3 <= 0.5f)
					{
						Dot(glint.X + 1f, glint.Y + 1f, 1f, 1f, new Color((int)(90f * num6), 170, 136, 96));
					}
					Dot(glint.X, glint.Y, 1f, 1f, color, GameBlendModes.Add);
					if (glint.Cross && num5 > 0.3f && num5 < 0.7f)
					{
						Color color2 = Faded(color, num6 * 0.5f);
						Dot(glint.X - 1f, glint.Y, 1f, 1f, color2, GameBlendModes.Add);
						Dot(glint.X + 1f, glint.Y, 1f, 1f, color2, GameBlendModes.Add);
						Dot(glint.X, glint.Y - 1f, 1f, 1f, color2, GameBlendModes.Add);
						Dot(glint.X, glint.Y + 1f, 1f, 1f, color2, GameBlendModes.Add);
					}
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
		UpdateGrains(now, dt);
		Craters.RemoveAll((Crater crater) => now - crater.Born > 12000);
		Player me = Globals.Me;
		_active = me != null && Meteo.AuDesert;
		if (!_active)
		{
			ClearWorld();
			return;
		}
		if (!Grille.Continu())
		{
			ClearWorld();
			Grains.Clear();
			Craters.Clear();
		}
		Visible.Clear();
		foreach (MapInstance item in Meteo.CartesVisibles())
		{
			if (Meteo.ZoneDe(item) == Meteo.GenreZone.Desert)
			{
				Meteo.DonneesDesert donneesDesert = Meteo.DesertDe(item);
				if (donneesDesert != null)
				{
					Visible.Add((item, donneesDesert));
				}
			}
		}
		CollectWalkers(now);
		FloatRect worldViewport = Graphics.WorldViewport;
		float jourActuel = Meteo.JourActuel;
		float num = Math.Clamp(0.25f + 0.75f * Meteo.ForceDuVent + 0.5f * Meteo.VentDeSable, 0f, 1f);
		float num2 = 0.55f + 0.45f * MathF.Pow(MathF.Max(0f, MathF.Sin((float)now / 2600f)), 2f);
		float area = worldViewport.Width * worldViewport.Height / 129600f;
		UpdateStreams(now, dt, worldViewport, area, num * num2);
		UpdateCrests(now, dt, worldViewport, num * num2);
		UpdateGlints(now, worldViewport, area, jourActuel);
		UpdateTumbleweeds(now, dt, worldViewport, num, num2);
		UpdateVultures(now, dt, worldViewport, jourActuel > 0.3f && Meteo.VentDeSable < 0.5f);
		UpdateLizards(now, worldViewport, jourActuel > 0.35f && Meteo.VentDeSable < 0.5f);
		Puffs.RemoveAll((Puff puff) => now - puff.Born > 1600);
		foreach (Puff puff in Puffs)
		{
			puff.X += (-7.92f + puff.Vx) * dt;
			puff.Y -= 3f * dt;
			puff.Vx *= 1f - 2f * dt;
		}
		if (now < _reportAt)
		{
			return;
		}
		_reportAt = now + 60000;
		foreach (var (mapInstance, donneesDesert2) in Visible)
		{
			ApplicationContext.Context.Value?.Logger.LogInformation("Desert {Carte} : {Sables} points de sable, {Cretes} bords de mesa, {Carcasses} carcasses, {Cachettes} cachettes de lezards", mapInstance.Name, donneesDesert2.Sables.Length, donneesDesert2.Cretes.Length, donneesDesert2.Carcasses.Length, donneesDesert2.Cachettes.Length);
		}
	}

	private static void ClearWorld()
	{
		Streams.Clear();
		Glints.Clear();
		Tumbleweeds.Clear();
		Vultures.Clear();
		Lizards.Clear();
		Puffs.Clear();
		Visible.Clear();
		Grains.RemoveAll((Grain grain) => grain.Kind != 1);
	}

	private static void Fail(Exception exception)
	{
		_broken = true;
		ClearWorld();
		Grains.Clear();
		Craters.Clear();
		ApplicationContext.Context.Value?.Logger.LogError(exception, "Desert vivant coupe apres un incident");
	}

	private static void CollectWalkers(long now)
	{
		Walkers.Clear();
		if (Globals.Entities == null)
		{
			return;
		}
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (value2 == null || value2.IsHidden || (value2.Type != EntityType.Player && value2.Type != EntityType.GlobalEntity) || value2 is Critter)
			{
				continue;
			}
			Vector2 origin = value2.Origin;
			Walkers.Add((origin.X, origin.Y, value2.IsMoving));
			bool flag = Flight.IsAirborne(value2);
			if (!value2.IsMoving || value2.Dashing != null || (!flag && (!(value2 is Player) || !value2.IsSprinting)) || !Meteo.EstSable(origin.X, origin.Y - 1f) || (LastKickAt.TryGetValue(value2.Id, out var value) && now - value < 55))
			{
				continue;
			}
			LastKickAt[value2.Id] = now;
			(float, float) tuple;
			switch (value2.DirectionFacing)
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
			(float, float) tuple2 = tuple;
			float item = tuple2.Item1;
			float item2 = tuple2.Item2;
			for (int i = 0; i < 3; i++)
			{
				if (Grains.Count >= 700)
				{
					break;
				}
				Grains.Add(new Grain
				{
					Kind = 2,
					X = origin.X - item * 5f + Between(-2f, 2f),
					Y = origin.Y - 1f - item2 * 3f + Between(-1f, 1f),
					Vx = (0f - item) * Between(20f, 60f) + Between(-8f, 8f),
					Vy = (0f - item2) * Between(12f, 36f) + Between(-6f, 6f),
					Z = 1f,
					Vz = Between(20f, 60f),
					Born = now,
					LifeMs = 2600,
					Color = SprayColors[i % SprayColors.Length]
				});
			}
			if (Puffs.Count < 80)
			{
				Puffs.Add(new Puff
				{
					X = origin.X - item * 4f + Between(-2f, 2f),
					Y = origin.Y - 2f,
					Vx = (0f - item) * Between(10f, 30f),
					Radius = Between(1.5f, 3f),
					Born = now
				});
			}
		}
		if (LastKickAt.Count > 128)
		{
			LastKickAt.Clear();
		}
	}

	private static void UpdateGrains(long now, float dt)
	{
		for (int num = Grains.Count - 1; num >= 0; num--)
		{
			Grain grain = Grains[num];
			long num2 = now - grain.Born;
			if (num2 >= grain.LifeMs)
			{
				Grains.RemoveAt(num);
			}
			else if (grain.Kind != 0 && grain.LandedAt < 0)
			{
				grain.X += grain.Vx * dt;
				grain.Y += grain.Vy * dt;
				grain.Vz -= 420f * dt;
				grain.Z += grain.Vz * dt;
				if (grain.Z <= 0f && num2 > 60)
				{
					grain.Z = 0f;
					grain.LandedAt = now;
				}
			}
		}
	}

	private static void UpdateStreams(long now, float dt, FloatRect view, float area, float strength)
	{
		int num = (int)Math.Min(140f, area * (10f + 24f * strength));
		int num2 = 0;
		while (Streams.Count < num && num2 < 12 && Visible.Count > 0)
		{
			var (mapInstance, donneesDesert) = Visible[Noise.Next(Visible.Count)];
			if (donneesDesert.Sables.Length != 0)
			{
				int num3 = donneesDesert.Sables[Noise.Next(donneesDesert.Sables.Length)];
				int num4 = mapInstance.X + (num3 & 0xFFFF);
				int num5 = mapInstance.Y + (num3 >> 16);
				if (Inside(num4, num5, view, 0f))
				{
					Streams.Add(new Stream
					{
						X = num4,
						Y = num5,
						Speed = Between(46f, 72f),
						Phase = Between(0f, 6f),
						Born = now,
						LifeMs = Noise.Next(1400, 2600)
					});
				}
			}
			num2++;
		}
		for (int num6 = Streams.Count - 1; num6 >= 0; num6--)
		{
			Stream stream = Streams[num6];
			if (now - stream.Born > stream.LifeMs || !Inside(stream.X, stream.Y, view, 48f))
			{
				Streams.RemoveAt(num6);
			}
			else
			{
				float num7 = stream.Speed * (0.7f + 0.5f * strength);
				stream.X += num7 * (-0.99f + 0.25f * MathF.Sin(stream.Y / 23f + (float)now / 900f + stream.Phase)) * dt;
				stream.Y += num7 * (0.12f + 0.45f * MathF.Sin(stream.X / 31f - (float)now / 700f + stream.Phase * 2f)) * dt;
				if (now - stream.LastSampleAt >= 18)
				{
					stream.LastSampleAt = now;
					stream.Xs[stream.Head] = stream.X;
					stream.Ys[stream.Head] = stream.Y;
					stream.Head = (stream.Head + 1) % 30;
					stream.Count = Math.Min(30, stream.Count + 1);
				}
			}
		}
	}

	private static void UpdateCrests(long now, float dt, FloatRect view, float strength)
	{
		if (Noise.NextDouble() > (double)(dt * 1000f / 90f * strength))
		{
			return;
		}
		foreach (var (mapInstance, donneesDesert) in Visible)
		{
			if (donneesDesert.Cretes.Length == 0)
			{
				continue;
			}
			(short X, short Y) tuple2 = donneesDesert.Cretes[Noise.Next(donneesDesert.Cretes.Length)];
			short item = tuple2.X;
			short item2 = tuple2.Y;
			int num = mapInstance.X + item;
			int num2 = mapInstance.Y + item2;
			if (!Inside(num, num2, view, 0f))
			{
				continue;
			}
			for (int i = 0; i < 5; i++)
			{
				if (Grains.Count >= 700)
				{
					break;
				}
				Grains.Add(new Grain
				{
					Kind = 0,
					X = (float)num + Between(-2f, 2f),
					Y = (float)num2 + Between(-4f, 4f),
					Vx = -0.99f * Between(28f, 60f),
					Vy = Between(-6f, 8f),
					Born = now,
					LifeMs = Noise.Next(900, 1600)
				});
			}
		}
	}

	private static void UpdateGlints(long now, FloatRect view, float area, float day)
	{
		Glints.RemoveAll((Glint glint) => now - glint.Born > glint.LifeMs);
		bool flag = day < 0.5f;
		int num = (int)Math.Min(240f, area * (flag ? 80f : (40f * day)));
		int num2 = 0;
		while (Glints.Count < num && num2 < 16 && Visible.Count > 0)
		{
			var (mapInstance, donneesDesert) = Visible[Noise.Next(Visible.Count)];
			if (donneesDesert.Clairs.Length != 0)
			{
				int num3 = donneesDesert.Clairs[Noise.Next(donneesDesert.Clairs.Length)];
				int num4 = mapInstance.X + (num3 & 0xFFFF);
				int num5 = mapInstance.Y + (num3 >> 16);
				if (Inside(num4, num5, view, 0f))
				{
					Glints.Add(new Glint
					{
						X = num4,
						Y = num5,
						Born = now + Noise.Next(0, 800),
						LifeMs = (flag ? Noise.Next(700, 1500) : Noise.Next(250, 600)),
						Cross = (Noise.NextDouble() < (flag ? 0.12 : 0.05))
					});
				}
			}
			num2++;
		}
	}

	private static void UpdateTumbleweeds(long now, float dt, FloatRect view, float wind, float gust)
	{
		if (now >= _nextTumbleweedAt && Tumbleweeds.Count < 4 && Visible.Count > 0)
		{
			_nextTumbleweedAt = now + (long)(Between(2600f, 4800f) / (0.4f + wind));
			for (int i = 0; i < 8; i++)
			{
				float num = view.Right + 10f;
				float y = Between(view.Top + 24f, view.Bottom - 8f);
				if (Meteo.EstSable(view.Right - 4f, y) && !ObstacleAt(num - -19.8f, y))
				{
					Tumbleweeds.Add(new Tumbleweed
					{
						X = num,
						Y = y,
						Speed = Between(38f, 54f)
					});
					break;
				}
			}
		}
		int num2 = MathF.Sign(-0.99f);
		for (int num3 = Tumbleweeds.Count - 1; num3 >= 0; num3--)
		{
			Tumbleweed tumbleweed = Tumbleweeds[num3];
			float num4 = tumbleweed.Speed * (0.8f + 0.3f * gust);
			if (now < tumbleweed.HopUntil)
			{
				tumbleweed.X += (float)num2 * num4 * 1.3f * dt;
				tumbleweed.Angle += num4 * dt / 4f * (float)num2;
			}
			else if (now < tumbleweed.StuckUntil)
			{
				tumbleweed.Angle += MathF.Sin((float)now / 60f) * 0.06f;
				if (tumbleweed.StuckUntil - now < 20)
				{
					tumbleweed.HopUntil = now + 520;
				}
			}
			else if (ObstacleAt(tumbleweed.X + (float)(num2 * 7), tumbleweed.Y))
			{
				tumbleweed.StuckUntil = now + 650;
			}
			else
			{
				tumbleweed.X += (float)num2 * num4 * dt;
				tumbleweed.Phase += num4 * dt / 5f;
				tumbleweed.Angle += num4 * dt / 5.5f * (float)num2;
			}
			if (!Inside(tumbleweed.X, tumbleweed.Y, view, 30f))
			{
				Tumbleweeds.RemoveAt(num3);
			}
		}
	}

	private static void UpdateVultures(long now, float dt, FloatRect view, bool allowed)
	{
		if (!allowed)
		{
			Vultures.Clear();
			return;
		}
		foreach (var item3 in Visible)
		{
			MapInstance item = item3.Map;
			Meteo.DonneesDesert item2 = item3.Desert;
			for (int i = 0; i < item2.Carcasses.Length; i++)
			{
				(Guid, int) key = (item.Id, i);
				if (!Vultures.ContainsKey(key))
				{
					int count = 2 + Noise.Next(2);
					Vultures[key] = (from num2 in Enumerable.Range(0, count)
						select new Vulture
						{
							Angle = (float)num2 * 2.2f + Between(0f, 1f),
							Radius = 46 + num2 * 15,
							Speed = Between(0.38f, 0.56f),
							FlapAt = now + Noise.Next(2500, 5000)
						}).ToArray();
				}
			}
		}
		foreach (Vulture[] value in Vultures.Values)
		{
			Vulture[] array = value;
			foreach (Vulture vulture in array)
			{
				vulture.Angle += vulture.Speed * dt;
				if (now > vulture.FlapAt + 380)
				{
					vulture.FlapAt = now + Noise.Next(2500, 5000);
				}
			}
		}
	}

	private static void UpdateLizards(long now, FloatRect view, bool allowed)
	{
		if (!allowed)
		{
			Lizards.Clear();
			return;
		}
		foreach (var item7 in Visible)
		{
			MapInstance item = item7.Map;
			Meteo.DonneesDesert item2 = item7.Desert;
			for (int i = 0; i < item2.Cachettes.Length; i++)
			{
				(Guid, int) key = (item.Id, i);
				if (!Lizards.ContainsKey(key))
				{
					(short CacheX, short CacheY, short X, short Y) tuple = item2.Cachettes[i];
					short item3 = tuple.CacheX;
					short item4 = tuple.CacheY;
					short item5 = tuple.X;
					short item6 = tuple.Y;
					(int, int) tuple2 = (item.X + item5, item.Y + item6);
					Lizards[key] = new Lizard
					{
						HomeX = tuple2.Item1,
						HomeY = tuple2.Item2,
						HideX = item.X + item3,
						HideY = item.Y + item4,
						X = tuple2.Item1,
						Y = tuple2.Item2,
						Dir = ((tuple2.Item1 > item.X + item3) ? 1 : (-1)),
						PushUpAt = now + Noise.Next(1500, 3500)
					};
				}
			}
		}
		foreach (Lizard lizard in Lizards.Values)
		{
			float num = ((Walkers.Count == 0) ? float.MaxValue : Walkers.Min(((float X, float Y, bool Moving) w) => Distance(lizard.X - w.X, (lizard.Y - w.Y) * 1.3f)));
			switch (lizard.Mode)
			{
			case 0:
				if (now > lizard.PushUpAt + 320)
				{
					lizard.PushUpAt = now + Noise.Next(1500, 3500);
				}
				if (num < 58f)
				{
					Flee(lizard, now);
				}
				break;
			case 1:
			{
				float num3 = Math.Min(1f, (float)(now - lizard.ModeAt) / (float)lizard.DurationMs);
				float num4 = lizard.HideX - lizard.FromX;
				float num5 = lizard.HideY - lizard.FromY;
				float num6 = MathF.Max(1f, Distance(num4, num5));
				float num7 = MathF.Sin(num3 * (float)Math.PI * 3f) * 4f * (1f - num3);
				lizard.X = lizard.FromX + num4 * num3 - num5 / num6 * num7;
				lizard.Y = lizard.FromY + num5 * num3 + num4 / num6 * num7;
				if (num3 >= 1f)
				{
					lizard.Mode = 2;
					lizard.CalmSince = now;
				}
				break;
			}
			case 2:
				if (num < 85f)
				{
					lizard.CalmSince = now;
				}
				else if (now - lizard.CalmSince > 3200)
				{
					lizard.Mode = 3;
					lizard.ModeAt = now;
					lizard.Dir = ((lizard.HomeX > lizard.HideX) ? 1 : (-1));
				}
				break;
			default:
			{
				float num2 = Smooth((float)(now - lizard.ModeAt) / 900f);
				lizard.X = lizard.HideX + (lizard.HomeX - lizard.HideX) * num2;
				lizard.Y = lizard.HideY + (lizard.HomeY - lizard.HideY) * num2;
				if (num2 >= 1f)
				{
					lizard.Mode = 0;
					lizard.Dir = -lizard.Dir;
				}
				if (num < 58f)
				{
					Flee(lizard, now);
				}
				break;
			}
			}
		}
	}

	private static void Flee(Lizard lizard, long now)
	{
		lizard.Mode = 1;
		lizard.ModeAt = now;
		lizard.FromX = lizard.X;
		lizard.FromY = lizard.Y;
		lizard.DurationMs = (int)(Distance(lizard.HideX - lizard.X, lizard.HideY - lizard.Y) / 120f * 1000f) + 60;
		lizard.Dir = ((lizard.HideX > lizard.X) ? 1 : (-1));
	}

	private static void ScareLizards(float x, float y, float radius)
	{
		long milliseconds = Timing.Global.Milliseconds;
		foreach (Lizard value in Lizards.Values)
		{
			int mode = value.Mode;
			bool flag = ((mode == 0 || mode == 3) ? true : false);
			if (flag && Distance(value.X - x, value.Y - y) < radius)
			{
				Flee(value, milliseconds);
			}
		}
	}

	private static void DrawStreams(long now)
	{
		float num = 1f - Meteo.JourActuel;
		foreach (Stream stream in Streams)
		{
			float num2 = Math.Min(1f, Math.Min((float)(now - stream.Born) / 250f, (float)(stream.LifeMs - (now - stream.Born)) / 400f));
			if (num2 <= 0f || stream.Count == 0)
			{
				continue;
			}
			(MapInstance, Meteo.DonneesDesert)? tuple = Meteo.DesertEn(stream.X, stream.Y);
			if (!tuple.HasValue)
			{
				continue;
			}
			(MapInstance, Meteo.DonneesDesert) valueOrDefault = tuple.GetValueOrDefault();
			for (int i = 0; i < stream.Count; i++)
			{
				int num3 = (stream.Head - stream.Count + i + 30) % 30;
				float num4 = stream.Xs[num3];
				float num5 = stream.Ys[num3];
				int num6 = Meteo.BlocDesert(valueOrDefault.Item2, valueOrDefault.Item1, num4, num5);
				if (num6 >= 0 && num6 < valueOrDefault.Item2.Obstacles.Length && valueOrDefault.Item2.Obstacles[num6] != 1 && !(num4 < (float)valueOrDefault.Item1.X) && !(num5 < (float)valueOrDefault.Item1.Y))
				{
					float num7 = (float)(i + 1) / (float)stream.Count;
					Color color = Faded(ColorForLuma(valueOrDefault.Item2.Luma[num6], num), ((num > 0.5f) ? 0.55f : 0.8f) * num2 * num7);
					Dot(num4, num5, (i <= stream.Count - 4) ? 1 : 2, 1f, color);
					if (i % 2 == 0 && num7 > 0.3f && num7 < 0.9f)
					{
						Dot(num4 + 1f, num5 + 2f, 1f, 1f, Faded(color, ((num > 0.5f) ? 0.55f : 0.8f) * num2 * num7 * 0.55f));
					}
				}
			}
		}
	}

	private static Color ColorForLuma(byte luma, float night)
	{
		if (!(night > 0.5f))
		{
			if (luma <= 205)
			{
				return new Color(255, 252, 240, 214);
			}
			return new Color(255, 172, 136, 92);
		}
		return new Color(255, 206, 218, 246);
	}

	private static Color GrainOn(float x, float y, float night)
	{
		(MapInstance, Meteo.DonneesDesert)? tuple = Meteo.DesertEn(x, y);
		if (tuple.HasValue)
		{
			(MapInstance, Meteo.DonneesDesert) valueOrDefault = tuple.GetValueOrDefault();
			int num = Meteo.BlocDesert(valueOrDefault.Item2, valueOrDefault.Item1, x, y);
			return ColorForLuma((byte)((num >= 0 && num < valueOrDefault.Item2.Luma.Length) ? valueOrDefault.Item2.Luma[num] : 210), night);
		}
		return ColorForLuma(210, night);
	}

	private static void DrawCraters(long now)
	{
		foreach (Crater crater in Craters)
		{
			long num = now - crater.Born;
			float num2 = Smooth((float)(num - 1500) / 8500f);
			float num3 = crater.Rx * (1f - num2 * 0.9f);
			float num4 = crater.Ry * (1f - num2 * 0.9f);
			float num5 = (crater.Big ? (1f - (float)num / 3200f) : 0f);
			if (num5 > 0f)
			{
				Meteo.OnduleSol(crater.X - 34f, crater.Y - 40f, 68f, 48f, num5);
			}
			if (num3 >= 1.5f)
			{
				for (int i = (int)MathF.Floor(crater.Y - num4 * 1.4f); i <= (int)MathF.Ceiling(crater.Y + num4 * 1.4f); i++)
				{
					float num6 = ((num5 > 0f) ? MathF.Round(MathF.Sin((float)i * 0.7f + (float)now * 0.011f) * 1.4f * num5) : 0f);
					for (int j = (int)MathF.Floor(crater.X - num3 * 1.4f); j <= (int)MathF.Ceiling(crater.X + num3 * 1.4f); j++)
					{
						float num7 = ((float)j - crater.X) / num3;
						float num8 = ((float)i - crater.Y) / num4;
						float num9 = MathF.Sqrt(num7 * num7 + num8 * num8) + (float)((j * 7 + i * 13) & 4) * 0.0075f;
						Color color;
						if (num9 < 0.7f)
						{
							color = ((num8 < -0.15f) ? new Color((int)(255f * (0.36f - 0.1f * num9)), 90, 62, 32) : new Color(30, 90, 62, 32));
						}
						else if (num9 < 1f)
						{
							color = ((num8 < 0f) ? new Color(60, 255, 248, 226) : new Color(46, 110, 80, 44));
						}
						else
						{
							if (!(num9 < 1.4f) || ((j * 5 + i * 3) & 3) != 0)
							{
								continue;
							}
							color = new Color(40, 255, 246, 222);
						}
						Dot((float)j + num6, i, 1f, 1f, color);
					}
				}
				if (num > 1500 && num < 9000)
				{
					for (int k = 0; k < 3; k++)
					{
						float num10 = ((float)now / 500f + (float)k / 3f + crater.X) % 1f;
						float x = (float)k * 2.1f + crater.X;
						Dot(crater.X + MathF.Cos(x) * crater.Rx * (1f - num10) * 0.8f, crater.Y + MathF.Sin(x) * crater.Ry * (1f - num10) * 0.8f, 1f, 1f, new Color(204, 250, 236, 206));
					}
				}
			}
			float alpha = 1f - Smooth((float)(num - 9000) / 3000f);
			(float, float, Color, int, int)[] glass = crater.Glass;
			for (int l = 0; l < glass.Length; l++)
			{
				var (num11, num12, color2, num13, _) = glass[l];
				Dot(crater.X + num11, crater.Y + num12, num13, num13, Faded(color2, alpha));
			}
		}
	}

	private static void DrawVultures(long now)
	{
		if (Vultures.Count == 0)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		foreach (KeyValuePair<(Guid, int), Vulture[]> vulture2 in Vultures)
		{
			vulture2.Deconstruct(out var key, out var value);
			(Guid, int) tuple = key;
			Guid item = tuple.Item1;
			int item2 = tuple.Item2;
			Vulture[] array = value;
			if (!MapInstance.TryGet(item, out MapInstance instance))
			{
				continue;
			}
			Meteo.DonneesDesert donneesDesert = Meteo.DesertDe(instance);
			if (donneesDesert == null || item2 >= donneesDesert.Carcasses.Length)
			{
				continue;
			}
			(short X, short Y) tuple2 = donneesDesert.Carcasses[item2];
			short item3 = tuple2.X;
			short item4 = tuple2.Y;
			Vulture[] array2 = array;
			foreach (Vulture vulture in array2)
			{
				float num = (float)(instance.X + item3) + MathF.Cos(vulture.Angle) * vulture.Radius + 12f;
				float num2 = (float)(instance.Y + item4) + MathF.Sin(vulture.Angle) * vulture.Radius * 0.55f + 18f;
				if (!Inside(num, num2, worldViewport, 24f))
				{
					continue;
				}
				float x = MathF.Atan2(MathF.Cos(vulture.Angle) * 0.55f, 0f - MathF.Sin(vulture.Angle));
				float num3 = MathF.Cos(x);
				float num4 = MathF.Sin(x);
				float num5 = num3;
				(int, int)[] array3 = ((now >= vulture.FlapAt && now < vulture.FlapAt + 380) ? VultureFlap : VultureOpen);
				HashSet<int> hashSet = new HashSet<int>();
				(int, int)[] array4 = array3;
				for (int j = 0; j < array4.Length; j++)
				{
					(int, int) tuple3 = array4[j];
					int item5 = tuple3.Item1;
					int item6 = tuple3.Item2;
					int num6 = (int)MathF.Round(num + (float)item5 * num5 - (float)item6 * num4);
					int num7 = (int)MathF.Round(num2 + (float)item5 * num4 + (float)item6 * num5);
					if (hashSet.Add(num6 * 4096 + num7))
					{
						Dot(num6, num7, 1f, 1f, new Color(66, 40, 26, 14));
					}
				}
			}
		}
	}

	private static void DrawLizard(Lizard lizard, long now)
	{
		if (lizard.Mode == 2 || !Inside(lizard.X, lizard.Y, Graphics.WorldViewport, 12f))
		{
			return;
		}
		float num = ((lizard.Mode == 3) ? Smooth((float)(now - lizard.ModeAt) / 250f) : 1f);
		int mode = lizard.Mode;
		bool flag = ((mode == 1 || mode == 3) ? true : false);
		bool flag2 = flag;
		string[] array = LizardFrames[(int)(flag2 ? (now / 50 % 2) : 0)];
		int num2 = ((lizard.Mode == 0 && now >= lizard.PushUpAt && now < lizard.PushUpAt + 320 && (now - lizard.PushUpAt) / 80 % 2 == 0L) ? (-1) : 0);
		float num3 = MathF.Round(lizard.X);
		float num4 = MathF.Round(lizard.Y) + (float)num2;
		Dot(num3 - 4f, MathF.Round(lizard.Y) + 2f, 8f, 1f, new Color((int)(38f * num), 60, 40, 20));
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < array[i].Length; j++)
			{
				char c = array[i][j];
				if (c != '.' && LizardColors.TryGetValue(c, out Color value))
				{
					Dot(num3 + (float)((lizard.Dir > 0) ? (j - 5) : (4 - j)), num4 - 2f + (float)i, 1f, 1f, Faded(value, num));
				}
			}
		}
	}

	private static void DrawTumbleweed(Tumbleweed weed, long now)
	{
		float num = ((now < weed.HopUntil) ? (MathF.Sin((float)Math.PI * (1f - (float)(weed.HopUntil - now) / 520f)) * 14f) : ((now < weed.StuckUntil) ? 0f : (MathF.Abs(MathF.Sin(weed.Phase)) * 4f)));
		Dot(weed.X - 5f, weed.Y, 10f, 2f, new Color((int)(46f * (1f - num / 24f)), 60, 40, 20));
		float num2 = MathF.Cos(weed.Angle);
		float num3 = MathF.Sin(weed.Angle);
		float num4 = num2;
		HashSet<int> hashSet = new HashSet<int>();
		(float, float, int)[] twigs = Twigs;
		for (int i = 0; i < twigs.Length; i++)
		{
			(float, float, int) tuple = twigs[i];
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			int item3 = tuple.Item3;
			int num5 = (int)MathF.Round(weed.X + item * num4 - item2 * num3);
			int num6 = (int)MathF.Round(weed.Y - 6f - num + item * num3 + item2 * num4);
			if (hashSet.Add(num5 * 4096 + num6))
			{
				Dot(num5, num6, 1f, 1f, TwigColors[item3]);
			}
		}
	}

	private static bool ObstacleAt(float x, float y)
	{
		(MapInstance, Meteo.DonneesDesert)? tuple = Meteo.DesertEn(x, y);
		if (tuple.HasValue)
		{
			(MapInstance, Meteo.DonneesDesert) valueOrDefault = tuple.GetValueOrDefault();
			int num = Meteo.BlocDesert(valueOrDefault.Item2, valueOrDefault.Item1, x, y);
			if (num >= 0 && num < valueOrDefault.Item2.Obstacles.Length)
			{
				return valueOrDefault.Item2.Obstacles[num] == 1;
			}
			return false;
		}
		return false;
	}

	private static float Distance(float dx, float dy)
	{
		return MathF.Sqrt(dx * dx + dy * dy);
	}

	private static bool Inside(float x, float y, FloatRect view, float margin)
	{
		if (x >= view.Left - margin && x <= view.Right + margin && y >= view.Top - margin)
		{
			return y <= view.Bottom + margin;
		}
		return false;
	}

	private static Color Faded(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(255f * alpha, 0f, 255f), color.R, color.G, color.B);
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
