using System;

namespace Intersect.Client.Framework.Maps;

public interface IMapGrid
{
	Guid[,] Content { get; }

	long Width { get; }

	long Height { get; }
}
