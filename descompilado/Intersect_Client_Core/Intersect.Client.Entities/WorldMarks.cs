using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class WorldMarks
{
	private sealed class Mote
	{
		public int Kind;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public float Ground;

		public long Born;

		public int LifeMs;

		public int Size;

		public Color Color = new Color(255, 222, 206, 170);
	}

	private sealed class Smoke
	{
		public float X;

		public float Y;

		public long Born;

		public long NextAt;
	}

	private static readonly List<Mote> Motes = new List<Mote>();

	private static readonly List<Smoke> Smokes = new List<Smoke>();

	private static readonly Random Noise = new Random();

	private static readonly Color[] ClodColors = new Color[3]
	{
		new Color(255, 98, 72, 44),
		new Color(255, 70, 96, 20),
		new Color(255, 120, 88, 54)
	};

	private static float Between(float min, float max)
	{
		return min + (float)Noise.NextDouble() * (max - min);
	}

	public static void OnKiImpact(float x, float y, bool big)
	{
		long milliseconds = Timing.Global.Milliseconds;
		GrassReact.Burn(x, y, big);
		Vent.Secouer(x, y, big ? 1.6f : 1f);
		Wildlife.Scare(x, y, big ? 130f : 90f);
		CaveLife.OnKiImpact(x, y, big);
		DesertLife.OnKiImpact(x, y, big);
		CityLife.OnKiImpact(x, y, big);
		if (!big)
		{
			if (Smokes.Count < 12)
			{
				Smokes.Add(new Smoke
				{
					X = x,
					Y = y,
					Born = milliseconds,
					NextAt = milliseconds + 150
				});
			}
			return;
		}
		for (int i = 0; i < 70; i++)
		{
			if (Motes.Count >= 400)
			{
				break;
			}
			float x2 = Between(0f, (float)Math.PI * 2f);
			float num = Between(15f, 55f);
			Motes.Add(new Mote
			{
				Kind = 0,
				X = x + MathF.Cos(x2) * Between(0f, 10f),
				Y = y,
				Vx = MathF.Cos(x2) * num,
				Vy = 0f - Between(20f, 60f),
				Ground = y + MathF.Sin(x2) * Between(4f, 26f),
				Born = milliseconds,
				LifeMs = (int)Between(2600f, 3800f),
				Size = Noise.Next(2, 4)
			});
		}
		for (int j = 0; j < 14; j++)
		{
			if (Motes.Count >= 400)
			{
				break;
			}
			Motes.Add(new Mote
			{
				Kind = 1,
				X = x,
				Y = y - 2f,
				Vx = Between(-70f, 70f),
				Vy = 0f - Between(40f, 90f),
				Ground = y + Between(-10f, 14f),
				Born = milliseconds,
				LifeMs = 2600,
				Size = 2,
				Color = ClodColors[Noise.Next(ClodColors.Length)]
			});
		}
	}

	public static void DrawAir()
	{
		if (Motes.Count == 0 && Smokes.Count == 0)
		{
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Smokes.Count - 1; num >= 0; num--)
		{
			Smoke smoke = Smokes[num];
			if (milliseconds - smoke.Born > 2600)
			{
				Smokes.RemoveAt(num);
			}
			else if (milliseconds >= smoke.NextAt && Motes.Count < 400)
			{
				smoke.NextAt = milliseconds + 180;
				Motes.Add(new Mote
				{
					Kind = 2,
					X = smoke.X + Between(-2f, 2f),
					Y = smoke.Y - 4f,
					Born = milliseconds,
					LifeMs = 900,
					Size = 2,
					Color = new Color(255, 150, 150, 140)
				});
			}
		}
		for (int num2 = Motes.Count - 1; num2 >= 0; num2--)
		{
			Mote mote = Motes[num2];
			long num3 = milliseconds - mote.Born;
			if (num3 >= mote.LifeMs)
			{
				Motes.RemoveAt(num2);
				continue;
			}
			float num4 = (float)num3 / (float)mote.LifeMs;
			float num5 = (float)num3 / 1000f;
			int size = mote.Size;
			float x;
			float alpha;
			float num6;
			switch (mote.Kind)
			{
			case 0:
			{
				float num7 = MathF.Min(num5, 0.6f);
				x = mote.X + mote.Vx * (1f - MathF.Exp((0f - num5) * 1.4f)) / 1.4f;
				float num8 = Math.Clamp((num5 - 0.4f) / 2.6f, 0f, 1f);
				num8 = num8 * num8 * (3f - 2f * num8);
				num6 = mote.Y + mote.Vy * num7 * (1f - num7 / 1.2f) + MathF.Max(0f, num5 - 0.6f) * 10f + (mote.Ground - mote.Y) * num8;
				alpha = 0.42f * (1f - num4 * num4);
				break;
			}
			case 1:
				x = mote.X + mote.Vx * num5;
				num6 = mote.Y + mote.Vy * num5 + 110f * num5 * num5;
				if (num6 >= mote.Ground && num5 > 0.15f)
				{
					num6 = mote.Ground;
					size = 1;
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(num6), 2f, 1f), Faded(mote.Color, 1f - num4));
					continue;
				}
				alpha = 1f;
				break;
			default:
				x = mote.X + MathF.Sin(num4 * 5f + (float)(mote.Born % 7)) * 3f;
				num6 = mote.Y - num4 * 26f;
				alpha = 0.35f * (1f - num4);
				break;
			}
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(x), MathF.Round(num6), size, size), Faded(mote.Color, alpha));
		}
	}

	private static Color Faded(Color color, float alpha)
	{
		return new Color((int)Math.Clamp(255f * alpha, 0f, 255f), color.R, color.G, color.B);
	}
}
