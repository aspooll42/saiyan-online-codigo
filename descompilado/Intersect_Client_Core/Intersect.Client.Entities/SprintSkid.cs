using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class SprintSkid
{
	private sealed class Skid
	{
		public long At;

		public Direction From;

		public Direction To;

		public bool HalfTurn;

		public float FeetX;

		public float FeetY;

		public bool Outdoors;

		public bool SecondPuff;
	}

	private readonly record struct Mark(Guid MapId, float X, float Y, int Dx, int Dy, int Length, long At);

	private readonly record struct Grain(float X, float Y, float Vx, float Vy, long At, long Life, int Size);

	private const long TurnSlackMs = 140L;

	private const long CooldownMs = 250L;

	private const long HalfTurnMs = 190L;

	private const long SecondPuffMs = 70L;

	private const long MarksGrowMs = 90L;

	private const long MarksHoldMs = 900L;

	private const long MarksFadeMs = 1400L;

	private static readonly Dictionary<Guid, (Direction Direction, long RunAt, long SkidAt)> Runs = new Dictionary<Guid, (Direction, long, long)>();

	private static readonly Dictionary<Guid, Skid> Skids = new Dictionary<Guid, Skid>();

	private static readonly List<Mark> Marks = new List<Mark>();

	private static readonly List<Grain> Grains = new List<Grain>();

	private static readonly Random Noise = new Random();

	private static readonly Color MarkColor = new Color(255, 52, 38, 20);

	private static readonly Color GrainColor = new Color(255, 222, 204, 166);

	public static bool Observe(Player player)
	{
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = player.IsSprinting && player.IsMoving && player.Dashing == null && !Flight.IsAirborne(player);
		Direction directionFacing = player.DirectionFacing;
		Runs.TryGetValue(player.Id, out (Direction, long, long) value);
		bool result = false;
		if (flag)
		{
			if (value.Item2 > 0 && milliseconds - value.Item2 <= 140 && directionFacing != value.Item1 && milliseconds - value.Item3 >= 250)
			{
				float num = Dot(value.Item1, directionFacing);
				if (value.Item1 != Direction.None && directionFacing != Direction.None && (num < -0.6f || MathF.Abs(num) < 0.3f))
				{
					Start(player, value.Item1, directionFacing, num < -0.6f, milliseconds);
					value.Item3 = milliseconds;
					result = true;
				}
			}
			value.Item1 = directionFacing;
			value.Item2 = milliseconds;
		}
		if (Runs.Count > 128 && !Runs.ContainsKey(player.Id))
		{
			Runs.Clear();
		}
		Runs[player.Id] = value;
		return result;
	}

	private static void Start(Player player, Direction from, Direction to, bool halfTurn, long now)
	{
		Vector2 origin = player.Origin;
		bool flag = player.MapInstance is MapInstance mapInstance && !mapInstance.IsIndoors;
		Skid skid = new Skid
		{
			At = now,
			From = from,
			To = to,
			HalfTurn = halfTurn,
			FeetX = origin.X,
			FeetY = origin.Y - 2f,
			Outdoors = flag,
			SecondPuff = !halfTurn
		};
		Skids[player.Id] = skid;
		if (!flag)
		{
			return;
		}
		var (num, num2) = Vector(from);
		SprintDust.AddPuff(skid.FeetX + num * 4f, skid.FeetY + num2 * 2f, from);
		if (player.MapInstance is MapInstance mapInstance2)
		{
			int length = (halfTurn ? 11 : 7);
			Marks.Add(new Mark(mapInstance2.Id, origin.X - (float)mapInstance2.X, origin.Y - (float)mapInstance2.Y, Math.Sign(num), Math.Sign(num2), length, now));
			if (Marks.Count > 40)
			{
				Marks.RemoveAt(0);
			}
		}
		float num3 = MathF.Atan2(num2, num);
		for (int i = 0; i < (halfTurn ? 12 : 6); i++)
		{
			float x = num3 + ((float)Noise.NextDouble() - 0.5f) * 1.5f;
			float num4 = 40f + (float)Noise.NextDouble() * 70f;
			Grains.Add(new Grain(origin.X + ((float)Noise.NextDouble() - 0.5f) * 6f, origin.Y - 1f - (float)Noise.NextDouble() * 3f, MathF.Cos(x) * num4, MathF.Sin(x) * num4 * 0.45f - 12f, now, 280 + Noise.Next(260), (!(Noise.NextDouble() < 0.4)) ? 1 : 2));
		}
		if (Grains.Count > 200)
		{
			Grains.RemoveRange(0, Grains.Count - 200);
		}
	}

	public static void Draw()
	{
		long now = Timing.Global.Milliseconds;
		KeyValuePair<Guid, Skid>[] array = Skids.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Guid, Skid> keyValuePair = array[i];
			var (key, skid2) = keyValuePair;
			if (!skid2.SecondPuff && now - skid2.At >= 70)
			{
				skid2.SecondPuff = true;
				if (skid2.Outdoors)
				{
					var (num, num2) = Vector(skid2.From);
					SprintDust.AddPuff(skid2.FeetX + num * 10f, skid2.FeetY + num2 * 5f, skid2.From);
				}
			}
			if (now - skid2.At > 190)
			{
				Skids.Remove(key);
			}
		}
		IGameTexture gameTexture = Graphics.Renderer?.WhitePixel;
		if (gameTexture == null)
		{
			return;
		}
		Marks.RemoveAll((Mark mark) => now - mark.At >= 2300);
		foreach (Mark mark in Marks)
		{
			if (!MapInstance.TryGet(mark.MapId, out MapInstance instance))
			{
				continue;
			}
			long num3 = now - mark.At;
			float num4 = ((num3 < 900) ? 1f : (1f - (float)(num3 - 900) / 1400f));
			int num5 = (int)MathF.Round((float)mark.Length * Math.Clamp((float)num3 / 90f, 0f, 1f));
			if (num5 >= 1 && !(num4 <= 0f))
			{
				Color renderColor = new Color((int)(115f * num4), MarkColor.R, MarkColor.G, MarkColor.B);
				float num6 = MathF.Round((float)instance.X + mark.X);
				float num7 = MathF.Round((float)instance.Y + mark.Y);
				if (mark.Dx != 0)
				{
					float num8 = ((mark.Dx > 0) ? num6 : (num6 - (float)num5));
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num8, num7 - 3f, num5, 1f), renderColor);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num8 + (float)((mark.Dx > 0) ? 2 : 0), num7, Math.Max(1, num5 - 2), 1f), renderColor);
				}
				else
				{
					float num9 = ((mark.Dy > 0) ? num7 : (num7 - (float)num5));
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num6 - 4f, num9, 1f, num5), renderColor);
					Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num6 + 3f, num9 + (float)((mark.Dy > 0) ? 2 : 0), 1f, Math.Max(1, num5 - 2)), renderColor);
				}
			}
		}
		Grains.RemoveAll((Grain grain) => now - grain.At >= grain.Life);
		foreach (Grain grain in Grains)
		{
			float num10 = (float)(now - grain.At) / 1000f;
			float num11 = (float)(now - grain.At) / (float)grain.Life;
			float num12 = (1f - MathF.Exp(-4f * num10)) / 4f;
			Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(MathF.Round(grain.X + grain.Vx * num12), MathF.Round(grain.Y + grain.Vy * num12 + 30f * num10 * num10), grain.Size, grain.Size), new Color((int)(230f * (1f - num11 * num11)), GrainColor.R, GrainColor.G, GrainColor.B));
		}
	}

	private static (float X, float Y) Vector(Direction direction)
	{
		return direction switch
		{
			Direction.Up => (X: 0f, Y: -1f), 
			Direction.Down => (X: 0f, Y: 1f), 
			Direction.Left => (X: -1f, Y: 0f), 
			Direction.Right => (X: 1f, Y: 0f), 
			Direction.UpLeft => (X: -0.7071f, Y: -0.7071f), 
			Direction.UpRight => (X: 0.7071f, Y: -0.7071f), 
			Direction.DownLeft => (X: -0.7071f, Y: 0.7071f), 
			Direction.DownRight => (X: 0.7071f, Y: 0.7071f), 
			_ => (X: 0f, Y: 0f), 
		};
	}

	private static float Dot(Direction a, Direction b)
	{
		var (num, num2) = Vector(a);
		var (num3, num4) = Vector(b);
		return num * num3 + num2 * num4;
	}
}
