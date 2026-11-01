using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Localization;

namespace Intersect.Client.Interface.Menu;

internal static class MenuLastPlaces
{
	private static readonly Dictionary<Guid, string> Known = new Dictionary<Guid, string>();

	private static Guid _characterId;

	private static Guid _mapId;

	private static string Key(Guid characterId)
	{
		return $"DernierLieu_{characterId:N}";
	}

	public static void Track()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		IMapInstance mapInstance = me.MapInstance;
		if (mapInstance == null || (me.Id == _characterId && mapInstance.Id == _mapId))
		{
			return;
		}
		_characterId = me.Id;
		_mapId = mapInstance.Id;
		if (!string.IsNullOrWhiteSpace(mapInstance.Name))
		{
			string text = Traduction.NomSource(mapInstance.Id, mapInstance.Name);
			if (!Known.TryGetValue(me.Id, out string value) || !(value == text))
			{
				Known[me.Id] = text;
				Globals.Database?.SavePreference(Key(me.Id), text);
			}
		}
	}

	public static string Get(Guid characterId)
	{
		if (characterId == Guid.Empty)
		{
			return string.Empty;
		}
		if (!Known.TryGetValue(characterId, out string value))
		{
			value = Globals.Database?.LoadPreference(Key(characterId)) ?? string.Empty;
			Known[characterId] = value;
		}
		return Traduction.Contenu(value);
	}
}
