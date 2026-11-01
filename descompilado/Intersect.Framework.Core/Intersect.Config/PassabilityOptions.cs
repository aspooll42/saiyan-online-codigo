using System.Collections.Generic;
using Intersect.Framework.Core.GameObjects.Maps;

namespace Intersect.Config;

public class PassabilityOptions : Dictionary<MapZone, bool>
{
	public bool Default { get; set; }

	public PassabilityOptions()
	{
		base[MapZone.Arena] = false;
		base[MapZone.Normal] = false;
		base[MapZone.Safe] = true;
	}

	public bool IsPassable(MapZone mapZoneType)
	{
		if (!TryGetValue(mapZoneType, out var value))
		{
			return Default;
		}
		return value;
	}
}
