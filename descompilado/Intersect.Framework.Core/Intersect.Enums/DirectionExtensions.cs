using System;
using System.Diagnostics;

namespace Intersect.Enums;

public static class DirectionExtensions
{
	public static Direction GetOpposite(this Direction direction)
	{
		return direction switch
		{
			Direction.None => Direction.None, 
			Direction.Up => Direction.Down, 
			Direction.Down => Direction.Up, 
			Direction.Left => Direction.Right, 
			Direction.Right => Direction.Left, 
			Direction.UpLeft => Direction.DownRight, 
			Direction.UpRight => Direction.DownLeft, 
			Direction.DownRight => Direction.UpLeft, 
			Direction.DownLeft => Direction.UpRight, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, "Invalid Direction"), 
		};
	}

	public static bool IsOppositeOf(this Direction direction, Direction otherDirection)
	{
		return otherDirection == direction.GetOpposite();
	}

	public static Point GetDeltaPoint(this Direction direction)
	{
		return direction switch
		{
			Direction.None => default(Point), 
			Direction.Down => new Point(0, 1), 
			Direction.DownRight => new Point(1, 1), 
			Direction.Right => new Point(1, 0), 
			Direction.UpRight => new Point(1, -1), 
			Direction.Up => new Point(0, -1), 
			Direction.UpLeft => new Point(-1, -1), 
			Direction.Left => new Point(-1, 0), 
			Direction.DownLeft => new Point(-1, 1), 
			_ => throw new UnreachableException($"Undefined direction: {direction} ({direction})"), 
		};
	}

	public static bool IsDiagonal(this Direction direction)
	{
		return direction switch
		{
			Direction.DownLeft => true, 
			Direction.DownRight => true, 
			Direction.UpLeft => true, 
			Direction.UpRight => true, 
			_ => false, 
		};
	}

	public static Direction[] GetComponentDirections(this Direction direction)
	{
		return direction switch
		{
			Direction.DownRight => new Direction[2]
			{
				Direction.Down,
				Direction.Right
			}, 
			Direction.DownLeft => new Direction[2]
			{
				Direction.Down,
				Direction.Left
			}, 
			Direction.UpLeft => new Direction[2]
			{
				Direction.Up,
				Direction.Left
			}, 
			Direction.UpRight => new Direction[2]
			{
				Direction.Up,
				Direction.Right
			}, 
			_ => new Direction[1] { direction }, 
		};
	}

	public static bool IsAdjacent(this Direction direction, Direction otherDirection)
	{
		return direction switch
		{
			Direction.None => false, 
			Direction.Up => (uint)(otherDirection - 4) <= 1u, 
			Direction.Down => (uint)(otherDirection - 6) <= 1u, 
			Direction.Left => (otherDirection == Direction.UpLeft || otherDirection == Direction.DownLeft) ? true : false, 
			Direction.Right => (uint)(otherDirection - 5) <= 1u, 
			Direction.UpLeft => (otherDirection == Direction.Up || otherDirection == Direction.Left) ? true : false, 
			Direction.UpRight => (otherDirection == Direction.Up || otherDirection == Direction.Right) ? true : false, 
			Direction.DownRight => (otherDirection == Direction.Down || otherDirection == Direction.Right) ? true : false, 
			Direction.DownLeft => (uint)(otherDirection - 1) <= 1u, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}
}
