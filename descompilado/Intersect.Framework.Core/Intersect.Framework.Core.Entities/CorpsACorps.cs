using System;
using Intersect.Enums;

namespace Intersect.Framework.Core.Entities;

public static class CorpsACorps
{
	public const float Portee = 46f;

	public const float Colle = 24f;

	public const float DemiArc = 70f;

	private static readonly float CosinusDuDemiArc = MathF.Cos((float)Math.PI * 7f / 18f);

	public const float Bras = 26f;

	public static float PorteeEntre(float rayonA, float rayonB)
	{
		return rayonA + rayonB + 26f;
	}

	public static float ColleEntre(float rayonA, float rayonB)
	{
		return rayonA + rayonB + 4f;
	}

	public static bool APortee(float ecartX, float ecartY, float tolerance = 0f, float rayonA = 10f, float rayonB = 10f)
	{
		float num = PorteeEntre(rayonA, rayonB) + tolerance;
		return ecartX * ecartX + ecartY * ecartY <= num * num;
	}

	public static bool Devant(float ecartX, float ecartY, Direction regard, float rayonA = 10f, float rayonB = 10f)
	{
		(float X, float Y) tuple = Vecteur(regard);
		float item = tuple.X;
		float item2 = tuple.Y;
		float num = MathF.Sqrt(ecartX * ecartX + ecartY * ecartY);
		if (num <= ColleEntre(rayonA, rayonB) || (item == 0f && item2 == 0f))
		{
			return true;
		}
		return (ecartX * item + ecartY * item2) / num >= CosinusDuDemiArc;
	}

	public static bool Touche(float ecartX, float ecartY, Direction regard, float tolerance = 0f, float rayonA = 10f, float rayonB = 10f)
	{
		if (APortee(ecartX, ecartY, tolerance, rayonA, rayonB))
		{
			return Devant(ecartX, ecartY, regard, rayonA, rayonB);
		}
		return false;
	}

	public static Direction DirectionVers(float ecartX, float ecartY)
	{
		if (MathF.Abs(ecartX) < 0.001f && MathF.Abs(ecartY) < 0.001f)
		{
			return Direction.None;
		}
		float num = MathF.Atan2(ecartY, ecartX) * 180f / (float)Math.PI;
		if (num >= -22.5f)
		{
			if (num < 22.5f)
			{
				return Direction.Right;
			}
			if (num < 67.5f)
			{
				return Direction.DownRight;
			}
			if (num < 112.5f)
			{
				return Direction.Down;
			}
			if (num < 157.5f)
			{
				return Direction.DownLeft;
			}
		}
		else
		{
			if (num >= -67.5f)
			{
				return Direction.UpRight;
			}
			if (num >= -112.5f)
			{
				return Direction.Up;
			}
			if (num >= -157.5f)
			{
				return Direction.UpLeft;
			}
		}
		return Direction.Left;
	}

	public static (float X, float Y) Vecteur(Direction direction)
	{
		return direction switch
		{
			Direction.Up => (X: 0f, Y: -1f), 
			Direction.Down => (X: 0f, Y: 1f), 
			Direction.Left => (X: -1f, Y: 0f), 
			Direction.Right => (X: 1f, Y: 0f), 
			Direction.UpLeft => (X: -0.70710677f, Y: -0.70710677f), 
			Direction.UpRight => (X: 0.70710677f, Y: -0.70710677f), 
			Direction.DownLeft => (X: -0.70710677f, Y: 0.70710677f), 
			Direction.DownRight => (X: 0.70710677f, Y: 0.70710677f), 
			_ => (X: 0f, Y: 0f), 
		};
	}
}
