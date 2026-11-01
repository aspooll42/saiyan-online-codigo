using System;
using MessagePack;

namespace Intersect.Framework.Core.GameObjects.Dungeons;

[MessagePackObject(false)]
public sealed class DungeonTelegraphShape
{
	[Key(0)]
	public float X { get; set; }

	[Key(1)]
	public float Y { get; set; }

	[Key(2)]
	public float DirX { get; set; }

	[Key(3)]
	public float DirY { get; set; }

	[Key(4)]
	public float Length { get; set; }

	[Key(5)]
	public float Width { get; set; }

	[Key(6)]
	public float Radius { get; set; }

	[Key(7)]
	public int DelayMs { get; set; }

	[IgnoreMember]
	public bool IsLane => Length > 0f;

	public bool Contains(float x, float y)
	{
		float num = x - X;
		float num2 = y - Y;
		if (!IsLane)
		{
			return num * num + num2 * num2 <= Radius * Radius;
		}
		float num3 = num * DirX + num2 * DirY;
		float num4 = MathF.Abs((0f - num) * DirY + num2 * DirX);
		if (num3 >= -0.5f && num3 <= Length)
		{
			return num4 <= Width / 2f;
		}
		return false;
	}

	public bool ToucheLeCorps(float x, float y, float rayon)
	{
		float num = x - X;
		float num2 = y - Y;
		if (!IsLane)
		{
			float num3 = Radius + rayon;
			return num * num + num2 * num2 <= num3 * num3;
		}
		float num4 = num * DirX + num2 * DirY;
		float num5 = MathF.Abs((0f - num) * DirY + num2 * DirX);
		float num6 = ((num4 < -0.5f) ? (-0.5f - num4) : ((num4 > Length) ? (num4 - Length) : 0f));
		float num7 = MathF.Max(0f, num5 - Width / 2f);
		return num6 * num6 + num7 * num7 <= rayon * rayon;
	}
}
