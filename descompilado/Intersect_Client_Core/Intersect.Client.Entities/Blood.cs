using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class Blood
{
	private sealed class Drop
	{
		public Guid MapId;

		public float X;

		public float Y;

		public float Vx;

		public float Vy;

		public long StartedAt;

		public float GroundY;

		public int Size;

		public bool Trail;
	}

	private readonly record struct Stain(Guid MapId, float LocalX, float LocalY, int Size, long LandedAt, bool Trail = false);

	private readonly record struct Burst(float X, float Y, float DirectionX, float DirectionY, bool Heavy, long StartedAt);

	private const float Gravity = 555f;

	private const long BurstFrameMs = 60L;

	private const long StainMs = 4000L;

	private const long StainHoldMs = 600L;

	private const long SplashMs = 60L;

	private const long DropLifeMs = 1500L;

	public const float HeavyHealthRatio = 0.15f;

	private const float TrailHealthRatio = 0.4f;

	private const long TrailStainMs = 12000L;

	private const long TrailStainHoldMs = 7200L;

	private const long TrailDryMs = 5500L;

	private static readonly Color TrailFresh = new Color(255, 150, 16, 26);

	private static readonly Color TrailFreshDark = new Color(255, 96, 10, 16);

	private static readonly Color TrailDry = new Color(255, 92, 26, 22);

	private static readonly Color TrailDryDark = new Color(255, 58, 16, 14);

	private static readonly Color Dark = new Color(255, 92, 8, 14);

	private static readonly Color Mid = new Color(255, 170, 18, 26);

	private static readonly Color Bright = new Color(255, 222, 40, 40);

	private static readonly Color Gleam = new Color(255, 255, 130, 118);

	private static readonly Color StainDark = new Color(255, 64, 6, 10);

	private static readonly Color StainMid = new Color(255, 108, 12, 18);

	private static readonly (float Angle, int Length)[] NormalRays = new(float, int)[5]
	{
		(-40f, 3),
		(-10f, 4),
		(18f, 3),
		(42f, 2),
		(-80f, 2)
	};

	private static readonly (float Angle, int Length)[] HeavyRays = new(float, int)[10]
	{
		(-60f, 5),
		(-35f, 7),
		(-15f, 8),
		(0f, 6),
		(15f, 7),
		(35f, 5),
		(60f, 4),
		(-95f, 3),
		(190f, 2),
		(-150f, 2)
	};

	private static readonly List<Drop> Drops = new List<Drop>();

	private static readonly List<Stain> Stains = new List<Stain>();

	private static readonly List<Burst> Bursts = new List<Burst>();

	private static readonly Dictionary<Guid, long> NextDrip = new Dictionary<Guid, long>();

	private static readonly Random Noise = new Random();

	public static bool Enabled => Globals.Database?.ShowBlood ?? true;

	public static bool Bleeds(Entity entity)
	{
		if (entity.Type != EntityType.Player)
		{
			if (entity.Type == EntityType.GlobalEntity && entity.Bleeds)
			{
				return !(entity is Critter);
			}
			return false;
		}
		return true;
	}

	public static void OnHit(Entity target, bool heavy)
	{
		if (Bleeds(target))
		{
			Gicler(target, heavy);
		}
	}

	public static void Gicler(Entity target, bool heavy)
	{
		if (!Enabled)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		FloatRect bodyWorldPos = target.BodyWorldPos;
		float num = target.WorldPos.Y + target.WorldPos.Height;
		float num2 = target.HitRecoilX;
		float hitRecoilY = target.HitRecoilY;
		if (num2 == 0f && hitRecoilY == 0f)
		{
			float num3;
			switch (target.DirectionFacing)
			{
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				num3 = 1f;
				break;
			case Direction.Right:
			case Direction.UpRight:
			case Direction.DownRight:
				num3 = -1f;
				break;
			default:
				num3 = ((Noise.Next(2) == 0) ? (-1f) : 1f);
				break;
			}
			num2 = num3;
		}
		float num4 = MathF.Floor(bodyWorldPos.X + bodyWorldPos.Width / 2f + num2 * 2f);
		float num5 = MathF.Floor(bodyWorldPos.Y + bodyWorldPos.Height * 0.55f);
		Bursts.Add(new Burst(num4, num5, num2, hitRecoilY, heavy, milliseconds));
		if (Bursts.Count > 32)
		{
			Bursts.RemoveAt(0);
		}
		float num6 = MathF.Atan2(hitRecoilY, num2);
		int num7 = (heavy ? (14 + Noise.Next(4)) : (4 + Noise.Next(3)));
		for (int i = 0; i < num7; i++)
		{
			int num8 = ((heavy && i < 3) ? 3 : ((!(Noise.NextDouble() < (heavy ? 0.45 : 0.5))) ? 1 : 2));
			float num9 = num8 switch
			{
				2 => 0.85f, 
				3 => 0.6f, 
				_ => 1f, 
			};
			float num10 = (heavy ? 1.1f : 0.8f);
			float x = num6 + ((float)Noise.NextDouble() - 0.5f) * 2f * num10;
			float num11 = (heavy ? (30f + (float)Noise.NextDouble() * 77f) : (27f + (float)Noise.NextDouble() * 46f)) * num9;
			float num12 = (heavy ? (17f + (float)Noise.NextDouble() * 83f) : (23f + (float)Noise.NextDouble() * 54f));
			Drops.Add(new Drop
			{
				MapId = target.MapId,
				X = num4 + (float)Noise.Next(-1, 2),
				Y = num5 + (float)Noise.Next(-2, 3),
				Vx = MathF.Cos(x) * num11,
				Vy = MathF.Sin(x) * num11 * 0.6f - num12,
				StartedAt = milliseconds + Noise.Next(30),
				GroundY = num - 4f + (float)Noise.Next(5) + hitRecoilY * (float)(6 + Noise.Next(10)),
				Size = num8
			});
		}
		if (Drops.Count > 240)
		{
			Drops.RemoveRange(0, Drops.Count - 240);
		}
	}

	public static void DrawGround()
	{
		if (Stains.Count == 0)
		{
			return;
		}
		if (!Enabled)
		{
			Stains.Clear();
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		for (int num = Stains.Count - 1; num >= 0; num--)
		{
			Stain stain = Stains[num];
			long num2 = milliseconds - stain.LandedAt;
			long num3 = (stain.Trail ? 12000 : 4000);
			long num4 = (stain.Trail ? 7200 : 600);
			MapInstance instance;
			if (num2 >= num3)
			{
				Stains.RemoveAt(num);
			}
			else if (MapInstance.TryGet(stain.MapId, out instance))
			{
				float alpha = ((num2 < num4) ? 1f : (1f - (float)(num2 - num4) / (float)(num3 - num4)));
				float num5 = (float)instance.X + stain.LocalX;
				float num6 = (float)instance.Y + stain.LocalY;
				Color color = Fade(StainMid, alpha);
				Color color2 = Fade(StainDark, alpha);
				if (stain.Trail)
				{
					float amount = Math.Clamp((float)num2 / 5500f, 0f, 1f);
					color = Fade(Mix(TrailFresh, TrailDry, amount), alpha);
					color2 = Fade(Mix(TrailFreshDark, TrailDryDark, amount), alpha);
				}
				switch (stain.Size)
				{
				case 1:
					Dot(gameTexture, num5, num6, color);
					break;
				case 2:
					Dot(gameTexture, num5, num6, color);
					Dot(gameTexture, num5 + 1f, num6, color2);
					break;
				default:
					Dot(gameTexture, num5 - 1f, num6, color, 4);
					Dot(gameTexture, num5 - 2f, num6 + 1f, color2, 6);
					Dot(gameTexture, num5, num6 - 1f, color2);
					break;
				}
				if (num2 < 60 && !stain.Trail)
				{
					Dot(gameTexture, num5 - 2f, num6 - 1f, Mid);
					Dot(gameTexture, num5 + (float)stain.Size + 1f, num6 - 1f, Mid);
				}
			}
		}
	}

	public static void Draw()
	{
		if (!Enabled)
		{
			Drops.Clear();
			Bursts.Clear();
			NextDrip.Clear();
			return;
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture != null)
		{
			long milliseconds = Timing.Global.Milliseconds;
			Drip(milliseconds);
			DrawBursts(gameTexture, milliseconds);
			DrawDrops(gameTexture, milliseconds);
		}
	}

	private static void DrawBursts(IGameTexture pixel, long now)
	{
		for (int num = Bursts.Count - 1; num >= 0; num--)
		{
			Burst burst = Bursts[num];
			long num2 = now - burst.StartedAt;
			if (num2 >= 120)
			{
				Bursts.RemoveAt(num);
			}
			else
			{
				bool flag = num2 < 60;
				bool flag2 = MathF.Abs(burst.DirectionX) >= MathF.Abs(burst.DirectionY);
				(float, int)[] array = (burst.Heavy ? HeavyRays : NormalRays);
				for (int i = 0; i < array.Length; i++)
				{
					(float, int) tuple = array[i];
					float item = tuple.Item1;
					int item2 = tuple.Item2;
					float num3 = item * (float)Math.PI / 180f;
					float num4;
					float num5;
					if (flag2)
					{
						num4 = MathF.Cos(num3) * (float)MathF.Sign(burst.DirectionX);
						num5 = MathF.Sin(num3);
					}
					else
					{
						float num6 = MathF.Atan2(burst.DirectionY, burst.DirectionX);
						num4 = MathF.Cos(num3 + num6);
						num5 = MathF.Sin(num3 + num6);
					}
					for (int j = ((!flag) ? ((int)((float)item2 * 0.6f)) : 0); j <= item2; j++)
					{
						Color color = ((j <= 1) ? Bright : ((j < item2) ? Mid : Dark));
						Dot(pixel, MathF.Floor(burst.X + num4 * (float)j), MathF.Floor(burst.Y + num5 * (float)j), color);
					}
				}
				if (flag)
				{
					Dot(pixel, burst.X, burst.Y, Gleam);
					Dot(pixel, burst.X + 1f, burst.Y, Bright);
					Dot(pixel, burst.X, burst.Y + 1f, Bright);
					Dot(pixel, burst.X + 1f, burst.Y + 1f, Mid);
				}
			}
		}
	}

	private static void DrawDrops(IGameTexture pixel, long now)
	{
		for (int num = Drops.Count - 1; num >= 0; num--)
		{
			Drop drop = Drops[num];
			if (now >= drop.StartedAt)
			{
				long num2 = now - drop.StartedAt;
				float num3 = (float)num2 / 1000f;
				var (num4, num5) = PositionOf(drop, num3);
				if ((drop.Vy + 555f * num3 > 0f && num5 >= drop.GroundY) || num2 > 1500)
				{
					Drops.RemoveAt(num);
					if (num2 <= 1500 && MapInstance.TryGet(drop.MapId, out MapInstance instance))
					{
						Stains.Add(new Stain(drop.MapId, MathF.Floor(num4) - (float)instance.X, MathF.Floor(drop.GroundY) - (float)instance.Y, drop.Size, now, drop.Trail));
						if (Stains.Count > 400)
						{
							Stains.RemoveAt(0);
						}
					}
				}
				else
				{
					(float X, float Y) tuple2 = PositionOf(drop, MathF.Max(0f, num3 - 0.03f));
					float item = tuple2.X;
					float item2 = tuple2.Y;
					float num6 = num4 - item;
					float num7 = num5 - item2;
					int num8 = Math.Max(1, (int)MathF.Round(MathF.Max(MathF.Abs(num6), MathF.Abs(num7))));
					for (int i = 0; i <= num8; i++)
					{
						Color color = ((i == num8) ? Bright : ((i == num8 - 1) ? Mid : Dark));
						Dot(pixel, MathF.Floor(item + num6 * (float)i / (float)num8), MathF.Floor(item2 + num7 * (float)i / (float)num8), color);
					}
					float num9 = MathF.Floor(num4);
					float num10 = MathF.Floor(num5);
					if (drop.Size >= 2)
					{
						Dot(pixel, num9, num10, Gleam);
						Dot(pixel, num9 + 1f, num10, Bright);
						Dot(pixel, num9, num10 + 1f, Mid);
						Dot(pixel, num9 + 1f, num10 + 1f, Dark);
					}
					if (drop.Size >= 3)
					{
						Dot(pixel, num9 + 2f, num10 + 1f, Mid);
						Dot(pixel, num9 + 1f, num10 + 2f, Dark);
						Dot(pixel, num9 - 1f, num10 + 1f, Mid);
						Dot(pixel, num9, num10 - 1f, Bright);
					}
				}
			}
		}
	}

	private static void Drip(long now)
	{
		if (NextDrip.Count > 128)
		{
			NextDrip.Clear();
		}
		foreach (Entity value2 in Globals.Entities.Values)
		{
			if (value2 == null || !Bleeds(value2))
			{
				continue;
			}
			long num = value2.MaxVital[0];
			long num2 = value2.Vital[0];
			if (num <= 0 || num2 <= 0 || (float)num2 >= (float)num * 0.4f)
			{
				NextDrip.Remove(value2.Id);
				continue;
			}
			float num3 = Math.Clamp((0.4f - (float)num2 / (float)num) / 0.35f, 0.05f, 1f);
			float num4 = num3 * (value2.IsMoving ? 5f : 1.4f);
			float num5 = Math.Min(4000f, 1000f / num4) * (0.65f + 0.7f * (float)Noise.NextDouble());
			if (!NextDrip.TryGetValue(value2.Id, out var value))
			{
				NextDrip[value2.Id] = now + (long)num5;
			}
			else if (now >= value)
			{
				NextDrip[value2.Id] = now + (long)num5;
				FloatRect bodyWorldPos = value2.BodyWorldPos;
				Drops.Add(new Drop
				{
					MapId = value2.MapId,
					X = MathF.Floor(bodyWorldPos.X + bodyWorldPos.Width / 2f) + (float)Noise.Next(-4, 5),
					Y = MathF.Floor(bodyWorldPos.Y + bodyWorldPos.Height * (0.3f + 0.3f * (float)Noise.NextDouble())),
					Vx = Noise.Next(-8, 9),
					Vy = Noise.Next(-10, 6),
					StartedAt = now,
					GroundY = value2.WorldPos.Y + value2.WorldPos.Height - 2f + (float)Noise.Next(4),
					Size = ((!(Noise.NextDouble() < 0.3)) ? 1 : 2),
					Trail = true
				});
			}
		}
	}

	private static (float X, float Y) PositionOf(Drop drop, float seconds)
	{
		return (X: drop.X + drop.Vx * seconds, Y: drop.Y + drop.Vy * seconds + 277.5f * seconds * seconds);
	}

	private static Color Mix(Color from, Color to, float amount)
	{
		return new Color(255, (int)((float)(int)from.R + (float)(to.R - from.R) * amount), (int)((float)(int)from.G + (float)(to.G - from.G) * amount), (int)((float)(int)from.B + (float)(to.B - from.B) * amount));
	}

	private static Color Fade(Color color, float alpha)
	{
		return new Color((int)((float)(int)color.A * Math.Clamp(alpha, 0f, 1f)), color.R, color.G, color.B);
	}

	private static void Dot(IGameTexture pixel, float x, float y, Color color, int width = 1)
	{
		if (color.A > 0)
		{
			Graphics.DrawGameTexture(pixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x, y, width, 1f), color);
		}
	}
}
