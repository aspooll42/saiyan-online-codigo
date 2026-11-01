using System;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;

namespace Intersect.Client.Maps;

public class MapGrid : IMapGrid
{
	public Guid[,] Content => Globals.MapGrid;

	public long Height => Globals.MapGridHeight;

	public long Width => Globals.MapGridWidth;
}
