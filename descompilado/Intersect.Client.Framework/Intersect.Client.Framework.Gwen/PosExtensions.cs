namespace Intersect.Client.Framework.Gwen;

public static class PosExtensions
{
	private const Pos MaskTopPriority = ~(Pos.Bottom | Pos.CenterV);

	private const Pos MaskBottomPriority = ~(Pos.Top | Pos.CenterV);

	private const Pos MaskLeftPriority = ~(Pos.Right | Pos.CenterH);

	private const Pos MaskRightPriority = ~(Pos.Left | Pos.CenterH);

	public static Point GetDockSpacing(this Pos dock, Padding dockSpacing)
	{
		if (dock.HasFlag(Pos.Fill) || dock.HasFlag(Pos.Center))
		{
			return default(Point);
		}
		Point result = default(Point);
		if (dock == (dock & ~(Pos.Bottom | Pos.CenterV)))
		{
			result.Y += dockSpacing.Top;
		}
		else if (dock == (dock & ~(Pos.Top | Pos.CenterV)))
		{
			result.Y += dockSpacing.Bottom;
		}
		if (dock == (dock & ~(Pos.Right | Pos.CenterH)))
		{
			result.Y += dockSpacing.Left;
		}
		else if (dock == (dock & ~(Pos.Left | Pos.CenterH)))
		{
			result.Y += dockSpacing.Right;
		}
		return result;
	}
}
