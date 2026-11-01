namespace Intersect.Framework.Core.GameObjects.Housing;

public readonly record struct HouseFootprint(int X, int Y, int Width, int Height, int DoorX, int DoorY)
{
	public int FrontX => DoorX;

	public int FrontY => DoorY + 1;

	public bool Contains(int x, int y)
	{
		if (x >= X && y >= Y && x < X + Width)
		{
			return y < Y + Height;
		}
		return false;
	}
}
