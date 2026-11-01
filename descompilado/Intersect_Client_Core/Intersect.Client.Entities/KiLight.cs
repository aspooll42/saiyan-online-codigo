using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class KiLight
{
	public readonly record struct Light(float X, float Y, float Radius, float Intensity, Color Color);

	private sealed class Flash
	{
		public float X;

		public float Y;

		public long Born;

		public Color Color = Intersect.Color.White;

		public bool Small;
	}

	private static readonly List<Light> Lights = new List<Light>();

	private static readonly List<Flash> Flashes = new List<Flash>();

	private static readonly List<(float X, float Y, Color Color, float Strength)> Balls = new List<(float, float, Color, float)>();

	private const long FlashMs = 500L;

	private const long EmberMs = 2600L;

	public const int TextureSize = 128;

	private static readonly Color EmberColor = new Color(255, 255, 140, 64);

	public static void AddFlash(float x, float y, Color color, bool small = false)
	{
		if (Flashes.Count >= 24)
		{
			Flashes.RemoveAt(0);
		}
		Flashes.Add(new Flash
		{
			X = x,
			Y = y,
			Born = Timing.Global.Milliseconds,
			Color = color,
			Small = small
		});
	}

	public static List<Light> Collect()
	{
		Lights.Clear();
		long milliseconds = Timing.Global.Milliseconds;
		FloatRect worldViewport = Graphics.WorldViewport;
		if (Globals.Entities != null)
		{
			foreach (Entity value in Globals.Entities.Values)
			{
				if (value == null || value.IsHidden)
				{
					continue;
				}
				Vector2 center = value.Center;
				if (!(center.X < worldViewport.Left - 96f) && !(center.X > worldViewport.Right + 96f) && !(center.Y < worldViewport.Top - 96f) && !(center.Y > worldViewport.Bottom + 96f))
				{
					Color color = Transformation.KiColorOf(value.TransformedSprite);
					if (value.IsCharging)
					{
						float num = 0.85f + 0.15f * MathF.Sin((float)milliseconds / 90f + (float)(value.Id.GetHashCode() % 7));
						Lights.Add(new Light(center.X, center.Y + 8f, 60f * num, 1.05f, color));
					}
					else if (value.IsTransforming)
					{
						Lights.Add(new Light(center.X, center.Y + 8f, 76f, 1.2f, color));
					}
					else if (!string.IsNullOrEmpty(value.TransformedSprite))
					{
						Lights.Add(new Light(center.X, center.Y + 8f, 44f, 0.7f, color));
					}
					(float, float, Color)? tuple = Echanges.LueurDe(value);
					if (tuple.HasValue)
					{
						(float, float, Color) valueOrDefault = tuple.GetValueOrDefault();
						Lights.Add(new Light(center.X, center.Y + 4f, valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3));
					}
				}
			}
		}
		Casse.AjouterLueurs(Lights);
		Balls.Clear();
		KiBlast.AddLiveLights(Balls);
		foreach (var (x, y, color2, intensity) in Balls)
		{
			Lights.Add(new Light(x, y, 40f, intensity, color2));
		}
		for (int num2 = Flashes.Count - 1; num2 >= 0; num2--)
		{
			Flash flash = Flashes[num2];
			long num3 = milliseconds - flash.Born;
			if (num3 >= 2600)
			{
				Flashes.RemoveAt(num2);
			}
			else
			{
				if (num3 < 500)
				{
					Lights.Add(new Light(flash.X, flash.Y, flash.Small ? 48f : 96f, (flash.Small ? 0.9f : 1.5f) * (1f - (float)num3 / 500f), flash.Color));
				}
				if (!flash.Small)
				{
					float num4 = 0.8f + 0.2f * MathF.Sin((float)num3 / 70f);
					Lights.Add(new Light(flash.X, flash.Y + 10f, 16f, 0.6f * (1f - (float)num3 / 2600f) * num4, EmberColor));
				}
			}
		}
		return Lights;
	}

	public static byte[] BuildTexture()
	{
		int[] array = new int[16]
		{
			0, 8, 2, 10, 12, 4, 14, 6, 3, 11,
			1, 9, 15, 7, 13, 5
		};
		byte[] array2 = new byte[65536];
		float num = 64f;
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num2 = MathF.Sqrt(((float)j + 0.5f - num) * ((float)j + 0.5f - num) + ((float)i + 0.5f - num) * ((float)i + 0.5f - num)) / num;
				int num3 = (i * 128 + j) * 4;
				int num4 = num3 + 1;
				byte b;
				array2[num3 + 2] = (b = byte.MaxValue);
				array2[num3] = (array2[num4] = b);
				if (!(num2 >= 1f))
				{
					float num5 = (1f - num2) * (1f - num2);
					float num6 = MathF.Floor(num5 * 5f + (float)array[(i & 3) * 4 + (j & 3)] / 16f) / 5f;
					array2[num3 + 3] = (byte)Math.Clamp(num6 * 255f, 0f, 255f);
				}
			}
		}
		return array2;
	}
}
