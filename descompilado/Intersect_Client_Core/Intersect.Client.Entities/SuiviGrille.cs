using System;
using System.Collections.Generic;
using Intersect.Client.Maps;

namespace Intersect.Client.Entities;

internal sealed class SuiviGrille
{
	private readonly Dictionary<Guid, (int X, int Y)> _vues = new Dictionary<Guid, (int, int)>();

	public bool Continu()
	{
		List<MapInstance> list = Meteo.CartesVisibles();
		if (list.Count == 0)
		{
			return true;
		}
		bool result = _vues.Count == 0;
		foreach (MapInstance item in list)
		{
			if (_vues.TryGetValue(item.Id, out (int, int) value) && value.Item1 == item.X && value.Item2 == item.Y)
			{
				result = true;
				break;
			}
		}
		_vues.Clear();
		foreach (MapInstance item2 in list)
		{
			_vues[item2.Id] = (item2.X, item2.Y);
		}
		return result;
	}
}
