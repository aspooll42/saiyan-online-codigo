using System;

namespace Intersect.Client.Interface.Game;

internal static class WantedPlaces
{
	public static string ZoneOf(Guid mapId)
	{
		if (!(mapId == Guid.Empty))
		{
			CarteMonde carteMonde = CarteMonde.Charger();
			if (carteMonde != null && carteMonde.ParId.TryGetValue(mapId, out CarteMonde.Case value))
			{
				return carteMonde.ZoneDe(value)?.Nom ?? value.Nom;
			}
		}
		return string.Empty;
	}

	public static string Elapsed(int minutes)
	{
		minutes = Math.Max(1, minutes);
		if (minutes >= 60)
		{
			if (minutes < 2880)
			{
				return $"{minutes / 60} h";
			}
			return $"{minutes / 1440} j";
		}
		return $"{minutes} min";
	}

	public static string Duration(int minutes)
	{
		minutes = Math.Max(1, minutes);
		if (minutes < 60)
		{
			return $"{minutes} min";
		}
		return $"{(minutes + 30) / 60} h";
	}
}
