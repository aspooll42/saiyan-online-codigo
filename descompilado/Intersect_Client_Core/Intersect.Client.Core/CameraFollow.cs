using System;
using System.Diagnostics;
using System.Numerics;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Database;
using Intersect.Client.General;

namespace Intersect.Client.Core;

public static class CameraFollow
{
	private const float CatchUpMs = 60f;

	private const float MaxLag = 4f;

	private static float _lagX;

	private static float _lagY;

	private static float _lastX = float.NaN;

	private static float _lastY = float.NaN;

	private static double _lastAt;

	private static readonly Stopwatch Montre = Stopwatch.StartNew();

	public static (float X, float Y) Lag(Entity entity)
	{
		double totalMilliseconds = Montre.Elapsed.TotalMilliseconds;
		Vector2 center = entity.Center;
		float num = (float)Math.Clamp(totalMilliseconds - _lastAt, 0.0, 100.0);
		_lastAt = totalMilliseconds;
		GameDatabase? database = Globals.Database;
		if ((database != null && !database.CameraEffects) || float.IsNaN(_lastX))
		{
			Reset(center.X, center.Y);
			return (X: 0f, Y: 0f);
		}
		float num2 = center.X - _lastX;
		float num3 = center.Y - _lastY;
		_lastX = center.X;
		_lastY = center.Y;
		int num4 = Options.Instance.Map.TileWidth * 2;
		if (Math.Abs(num2) > (float)num4 || Math.Abs(num3) > (float)num4)
		{
			num2 = 0f;
			num3 = 0f;
		}
		float num5 = MathF.Exp((0f - num) / 60f);
		_lagX = Math.Clamp((_lagX + num2) * num5, -4f, 4f);
		_lagY = Math.Clamp((_lagY + num3) * num5, -4f, 4f);
		return (X: MathF.Round(_lagX), Y: MathF.Round(_lagY));
	}

	private static void Reset(float x, float y)
	{
		_lagX = 0f;
		_lagY = 0f;
		_lastX = x;
		_lastY = y;
	}
}
