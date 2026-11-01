using System;
using Intersect.Collections;

namespace Intersect.Framework.Core.GameObjects.Maps.MapList;

public class MapListMap : MapListItem, IComparable<MapListMap>
{
	public Guid MapId;

	public long TimeCreated;

	public MapListMap()
	{
		Name = "New Map";
		Type = 1;
	}

	public int CompareTo(MapListMap other)
	{
		return string.Compare(Name, other.Name, StringComparison.CurrentCultureIgnoreCase);
	}

	public void PostLoad(DatabaseObjectLookup gameMaps, bool isServer = true)
	{
		if (!isServer)
		{
			if (gameMaps.Keys.Contains(MapId))
			{
				gameMaps[MapId].Name = Name;
			}
		}
		else if (gameMaps.Keys.Contains(MapId))
		{
			Name = gameMaps[MapId].Name;
		}
	}
}
