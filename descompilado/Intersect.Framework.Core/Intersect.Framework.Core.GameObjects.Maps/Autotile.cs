namespace Intersect.Framework.Core.GameObjects.Maps;

public class Autotile
{
	public Point16i[] QuarterTile = new Point16i[5];

	public byte RenderState;

	public Autotile Copy()
	{
		Autotile autotile = new Autotile()
		{
			RenderState = RenderState,
		};
		for (int i = 0; i < 5; i++)
		{
			autotile.QuarterTile[i] = new Point16i
			{
				X = QuarterTile[i].X,
				Y = QuarterTile[i].Y
			};
		}
		return autotile;
	}

	public bool Equals(Autotile quarterTile)
	{
		if (quarterTile.RenderState != RenderState)
		{
			return false;
		}
		for (int i = 0; i < 5; i++)
		{
			if (quarterTile.QuarterTile[i].X != QuarterTile[i].X)
			{
				return false;
			}
			if (quarterTile.QuarterTile[i].Y != QuarterTile[i].Y)
			{
				return false;
			}
		}
		return true;
	}
}
