using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Maps;

public sealed class LigneDeBlocage
{
	public float X0 { get; set; }

	public float Y0 { get; set; }

	public float X1 { get; set; }

	public float Y1 { get; set; }

	public bool ArreteLesTirs { get; set; }

	public bool Chute { get; set; }

	[JsonIgnore]
	public float Longueur => MathF.Sqrt((X1 - X0) * (X1 - X0) + (Y1 - Y0) * (Y1 - Y0));

	public LigneDeBlocage()
	{
	}

	public LigneDeBlocage(float x0, float y0, float x1, float y1, bool arreteLesTirs = false, bool chute = false)
	{
		X0 = x0;
		Y0 = y0;
		X1 = x1;
		Y1 = y1;
		ArreteLesTirs = arreteLesTirs;
		Chute = chute;
	}

	public LigneDeBlocage(LigneDeBlocage autre)
		: this(autre.X0, autre.Y0, autre.X1, autre.Y1, autre.ArreteLesTirs, autre.Chute)
	{
	}

	public float DistanceCarree(float x, float y)
	{
		float num = X1 - X0;
		float num2 = Y1 - Y0;
		float num3 = num * num + num2 * num2;
		float num4 = ((num3 < 0.0001f) ? 0f : Math.Clamp(((x - X0) * num + (y - Y0) * num2) / num3, 0f, 1f));
		float num5 = x - (X0 + num * num4);
		float num6 = y - (Y0 + num2 * num4);
		return num5 * num5 + num6 * num6;
	}

	public bool Coupe(float ax, float ay, float bx, float by)
	{
		float num = Sens(X0, Y0, X1, Y1, ax, ay);
		float num2 = Sens(X0, Y0, X1, Y1, bx, by);
		float num3 = Sens(ax, ay, bx, by, X0, Y0);
		float num4 = Sens(ax, ay, bx, by, X1, Y1);
		if (((num > 0f && num2 < 0f) || (num < 0f && num2 > 0f)) && ((num3 > 0f && num4 < 0f) || (num3 < 0f && num4 > 0f)))
		{
			return true;
		}
		if ((!(MathF.Abs(num) < 0.01f) || !Entre(X0, Y0, X1, Y1, ax, ay)) && (!(MathF.Abs(num2) < 0.01f) || !Entre(X0, Y0, X1, Y1, bx, by)) && (!(MathF.Abs(num3) < 0.01f) || !Entre(ax, ay, bx, by, X0, Y0)))
		{
			if (MathF.Abs(num4) < 0.01f)
			{
				return Entre(ax, ay, bx, by, X1, Y1);
			}
			return false;
		}
		return true;
		static bool Entre(float px, float py, float qx, float qy, float rx, float ry)
		{
			if (MathF.Min(px, qx) - 0.01f <= rx && rx <= MathF.Max(px, qx) + 0.01f && MathF.Min(py, qy) - 0.01f <= ry)
			{
				return ry <= MathF.Max(py, qy) + 0.01f;
			}
			return false;
		}
		static float Sens(float px, float py, float qx, float qy, float rx, float ry)
		{
			return (qx - px) * (ry - py) - (qy - py) * (rx - px);
		}
	}
}
