using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class SandSteps
{
	private readonly record struct Print(Guid MapId, int LocalX, int LocalY, long Born, bool Horizontal, int Dir, bool Running, bool Flip);

	private sealed class Grain
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Hop;

		public long Born;

		public int LifeMs;

		public Color Color = new Color(255, 236, 214, 168);
	}

	private sealed class Puff
	{
		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long Born;

		public int LifeMs;

		public int Size;
	}

	private static readonly List<Print> Prints = new List<Print>();

	private static readonly List<Grain> Grains = new List<Grain>();

	private static readonly List<Puff> Puffs = new List<Puff>();

	private static readonly Random Noise = new Random();

	private const int MaxPrints = 360;

	private const float LifeMs = 12000f;

	private const float WindLifeMs = 3500f;

	private const float DesertLifeMs = 8000f;

	private static readonly Color[] GrainColors = new Color[3]
	{
		new Color(255, 236, 214, 168),
		new Color(255, 205, 160, 110),
		new Color(255, 250, 236, 200)
	};

	private static readonly (int R, int G, int B) Shade = (R: 58, G: 30, B: 8);

	private static readonly (int R, int G, int B) Hollow = (R: 88, G: 50, B: 18);

	private static readonly (int R, int G, int B) Rim = (R: 255, G: 248, B: 222);

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	public static void OnWalkFrame(Entity entity, int frame, bool running)
	{
		if (!(entity is Player) || !entity.IsMoving || entity.Dashing != null || Flight.IsAirborne(entity))
		{
			return;
		}
		Vector2 origin = entity.Origin;
		float num = origin.Y - 1f;
		if (!Meteo.EstSable(origin.X, num) || !MapInstance.TryGet(entity.MapId, out MapInstance instance))
		{
			return;
		}
		(int, int) tuple;
		switch (entity.DirectionFacing)
		{
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			tuple = (-1, 0);
			break;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			tuple = (1, 0);
			break;
		case Direction.Up:
			tuple = (0, -1);
			break;
		default:
			tuple = (0, 1);
			break;
		}
		(int, int) tuple2 = tuple;
		int item = tuple2.Item1;
		int item2 = tuple2.Item2;
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = item != 0;
		int num2 = ((frame % 2 != 0) ? 1 : (-1));
		float num3 = MathF.Round(flag ? (origin.X - (float)(item * 2)) : (origin.X + (float)(num2 * 3)));
		float num4 = MathF.Round(flag ? (num + (float)(num2 * 2)) : (num - (float)(item2 * 2)));
		if (Prints.Count >= 360)
		{
			Prints.RemoveAt(0);
		}
		Prints.Add(new Print(entity.MapId, (int)(num3 - (float)instance.X), (int)(num4 - (float)instance.Y), milliseconds, flag, flag ? item : item2, running, Noise.Next(2) == 0));
		if (frame != 1 && frame != 3)
		{
			return;
		}
		int num5 = (running ? (7 + Noise.Next(3)) : (3 + Noise.Next(2)));
		for (int i = 0; i < num5; i++)
		{
			if (Grains.Count >= 240)
			{
				break;
			}
			float num6 = Between(8f, 22f) * (running ? 1.7f : 1f);
			float num7 = Between(-7f, 7f);
			Grains.Add(new Grain
			{
				X = num3,
				Y = num4,
				Vx = (float)(-item) * num6 + (flag ? 0f : num7),
				Vy = (float)(-item2) * num6 * 0.6f + (flag ? (num7 * 0.4f) : 0f),
				Hop = Between(14f, 30f) * (running ? 1.35f : 1f),
				Born = milliseconds,
				LifeMs = 300 + Noise.Next(140),
				Color = GrainColors[Noise.Next(GrainColors.Length)]
			});
		}
		for (int j = 0; j < (running ? 5 : 3); j++)
		{
			if (Puffs.Count >= 120)
			{
				break;
			}
			Puffs.Add(new Puff
			{
				X = num3 + Between(-2f, 2f) - (float)item * (running ? Between(2f, 6f) : 0f),
				Y = num4 + Between(-1f, 1f),
				Vx = (float)(-item) * Between(3f, 9f) * (running ? 1.6f : 1f),
				Vy = (float)(-item2) * Between(2f, 6f) - Between(1f, 4f),
				Born = milliseconds,
				LifeMs = 360 + Noise.Next(160),
				Size = ((!running) ? 1 : 2)
			});
		}
	}

	public static void DrawGround()
	{
		if (Prints.Count == 0 && Grains.Count == 0 && Puffs.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		FloatRect worldViewport = Graphics.WorldViewport;
		float num = 12000f - 8500f * Math.Clamp(Meteo.VentDeSable / 0.4f, 0f, 1f);
		bool auDesert = Meteo.AuDesert;
		if (auDesert)
		{
			num = Math.Min(num, 8000f);
		}
		Guid guid = default(Guid);
		MapInstance mapInstance = null;
		for (int num2 = Prints.Count - 1; num2 >= 0; num2--)
		{
			Print print = Prints[num2];
			float num3 = (float)(milliseconds - print.Born) / num;
			if (num3 >= 1f)
			{
				Prints.RemoveAt(num2);
			}
			else
			{
				if (print.MapId != guid)
				{
					guid = print.MapId;
					mapInstance = (MapInstance.TryGet(print.MapId, out MapInstance instance) ? instance : null);
				}
				if (mapInstance != null)
				{
					float num4 = mapInstance.X + print.LocalX;
					float num5 = mapInstance.Y + print.LocalY;
					if (!(num4 < worldViewport.Left - 8f) && !(num4 > worldViewport.Right + 8f) && !(num5 < worldViewport.Top - 8f) && !(num5 > worldViewport.Bottom + 8f))
					{
						float num6 = (auDesert ? Math.Clamp(num3 / 0.75f, 0f, 1f) : 0f);
						DrawPrint(gameTexture, print, num4, num5, (num3 < 0.6f) ? 1f : (1f - (num3 - 0.6f) / 0.4f), num6 * num6 * (3f - 2f * num6));
						if (auDesert && num6 < 0.85f)
						{
							float num7 = (float)((milliseconds - print.Born) % 900) / 900f;
							Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(num4 - -6.9300003f * (1f - num7)), MathF.Round(num5 - 1f + num7), 1f, 1f), new Color(204, 250, 236, 206));
						}
					}
				}
			}
		}
		for (int num8 = Puffs.Count - 1; num8 >= 0; num8--)
		{
			Puff puff = Puffs[num8];
			long num9 = milliseconds - puff.Born;
			if (num9 >= puff.LifeMs)
			{
				Puffs.RemoveAt(num8);
			}
			else
			{
				float num10 = (float)num9 / (float)puff.LifeMs;
				float num11 = (float)num9 / 1000f;
				int num12 = puff.Size + (int)MathF.Round(num10 * 1.5f);
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(puff.X + puff.Vx * num11) - (float)(num12 >> 1), MathF.Round(puff.Y + puff.Vy * num11) - (float)(num12 >> 1), num12, num12), new Color((int)(66f * (1f - num10)), 240, 226, 190));
			}
		}
		for (int num13 = Grains.Count - 1; num13 >= 0; num13--)
		{
			Grain grain = Grains[num13];
			long num14 = milliseconds - grain.Born;
			float num15 = (float)num14 / 1000f;
			float num16 = grain.Hop * num15 - 110f * num15 * num15;
			if (num14 >= grain.LifeMs || (num16 < 0f && num15 > 0.05f))
			{
				Grains.RemoveAt(num13);
			}
			else
			{
				float num17 = (float)num14 / (float)grain.LifeMs;
				Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(grain.X + grain.Vx * num15), MathF.Round(grain.Y + grain.Vy * num15 - MathF.Max(0f, num16)), 1f, 1f), new Color((int)(242f * (1f - num17 * 0.5f)), grain.Color.R, grain.Color.G, grain.Color.B));
			}
		}
	}

	private static void DrawPrint(IGameTexture pixel, Print print, float x, float y, float fade, float fill = 0f)
	{
		fade *= 1f - fill * 0.5f;
		int num = Math.Max(2, (int)MathF.Round((float)(print.Running ? 6 : 5) * (1f - fill * 0.6f)));
		if (print.Horizontal)
		{
			float num2 = x - (float)(num / 2);
			for (int i = 0; i < num; i++)
			{
				bool flag = i == 0 || i == num - 1;
				if (!flag)
				{
					Dot(num2 + (float)i, y - 1f, Shade, 0.5f);
					Dot(num2 + (float)i, y + 1f, Rim, 0.5f);
				}
				Dot(num2 + (float)i, y, flag ? Shade : Hollow, flag ? 0.34f : 0.42f);
			}
			if (print.Running)
			{
				float num3 = ((print.Dir > 0) ? (num2 - 1f) : (num2 + (float)num));
				Dot(num3 - (float)print.Dir, y + (float)((!print.Flip) ? 1 : (-1)), Rim, 0.55f);
				Dot(num3 - (float)(print.Dir * 2), y + (float)(print.Flip ? 1 : 0), Rim, 0.4f);
				Dot(num3, y, Rim, 0.45f);
			}
			return;
		}
		float num4 = y - (float)(num / 2);
		for (int j = 0; j < num; j++)
		{
			bool flag2 = j == 0 || j == num - 1;
			Dot(x - 1f, num4 + (float)j, Shade, flag2 ? 0.25f : 0.42f);
			if (!flag2)
			{
				Dot(x, num4 + (float)j, Hollow, 0.42f);
			}
			Dot(x + 1f, num4 + (float)j, Shade, flag2 ? 0.15f : 0.28f);
		}
		Dot(x, num4 - 1f, Shade, 0.5f);
		Dot(x, num4 + (float)num, Rim, 0.55f);
		Dot(x - 1f, num4 + (float)num, Rim, 0.3f);
		if (print.Running)
		{
			float num5 = ((print.Dir > 0) ? (num4 - 1f) : (num4 + (float)num));
			Dot(x + (float)((!print.Flip) ? 1 : (-1)), num5 - (float)print.Dir, Rim, 0.5f);
			Dot(x, num5 - (float)(print.Dir * 2), Rim, 0.35f);
		}
		void Dot(float px, float py, (int R, int G, int B) color, float alpha)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(px, py, 1f, 1f), new Color((int)(255f * alpha * fade), color.R, color.G, color.B));
		}
	}
}
