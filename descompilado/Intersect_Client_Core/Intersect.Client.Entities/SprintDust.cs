using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public static class SprintDust
{
	private const int Frames = 4;

	private const long FrameMs = 60L;

	private const long CooldownMs = 250L;

	private static readonly Dictionary<Guid, (bool Running, Direction Direction, long LastPuffAt)> States = new Dictionary<Guid, (bool, Direction, long)>();

	private static readonly List<(float FeetX, float FeetY, Direction Direction, long StartedAt)> Puffs = new List<(float, float, Direction, long)>();

	public static void Observe(Player player, bool skidded = false)
	{
		long milliseconds = Timing.Global.Milliseconds;
		bool flag = player.IsSprinting && player.IsMoving && player.Dashing == null && !Flight.IsAirborne(player);
		Direction directionFacing = player.DirectionFacing;
		States.TryGetValue(player.Id, out (bool, Direction, long) value);
		bool flag2 = flag && !value.Item1;
		bool flag3 = flag && value.Item1 && directionFacing != value.Item2 && !skidded;
		if ((flag2 || flag3) && milliseconds - value.Item3 >= 250 && player.MapInstance is MapInstance { IsIndoors: false })
		{
			Vector2 origin = player.Origin;
			Puffs.Add((origin.X, origin.Y - 2f, directionFacing, milliseconds));
			if (Puffs.Count > 32)
			{
				Puffs.RemoveAt(0);
			}
			value.Item3 = milliseconds;
		}
		if (States.Count > 128 && !States.ContainsKey(player.Id))
		{
			States.Clear();
		}
		States[player.Id] = (flag, directionFacing, value.Item3);
	}

	public static void AddPuff(float feetX, float feetY, Direction direction)
	{
		Puffs.Add((feetX, feetY, direction, Timing.Global.Milliseconds));
		if (Puffs.Count > 48)
		{
			Puffs.RemoveAt(0);
		}
	}

	public static void Draw()
	{
		if (Puffs.Count == 0)
		{
			return;
		}
		long now = Timing.Global.Milliseconds;
		Puffs.RemoveAll(((float FeetX, float FeetY, Direction Direction, long StartedAt) puff) => now - puff.StartedAt >= 240);
		foreach (var (num, num2, direction, num3) in Puffs)
		{
			(string, int) tuple2;
			switch (direction)
			{
			case Direction.Right:
			case Direction.UpRight:
			case Direction.DownRight:
				tuple2 = ("poussiere_droite.png", 24);
				break;
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				tuple2 = ("poussiere_gauche.png", 8);
				break;
			default:
				tuple2 = ("poussiere_verticale.png", 16);
				break;
			}
			(string, int) tuple3 = tuple2;
			string item = tuple3.Item1;
			int item2 = tuple3.Item2;
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Misc, item);
			if (texture != null)
			{
				int num4 = (int)Math.Min(3L, (now - num3) / 60);
				int num5 = texture.Width / 4;
				Graphics.DrawGameTexture(texture, new FloatRect(num4 * num5, 0f, num5, texture.Height), new FloatRect(MathF.Round(num - (float)item2), MathF.Round(num2 - 17f), num5, texture.Height), new Color(255, 255, 255, 255));
			}
		}
	}
}
