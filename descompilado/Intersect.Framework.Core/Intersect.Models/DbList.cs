using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Intersect.Collections;

namespace Intersect.Models;

public class DbList<T> : List<Guid> where T : IDatabaseObject
{
	public List<T> GetAll()
	{
		List<T> list = new List<T>();
		Guid[] array = ToArray();
		foreach (Guid id in array)
		{
			list.Add((T)DatabaseObjectLookup.LookupMap[typeof(T)].Get(id));
		}
		return list;
	}

	public T Get(Guid id)
	{
		return (T)DatabaseObjectLookup.LookupMap[typeof(T)].Get(id);
	}

	public bool TryGet(Guid id, [NotNullWhen(true)] out T? value)
	{
		if (DatabaseObjectLookup.LookupMap.TryGetValue(typeof(T), out var value2))
		{
			return value2.TryGetValue(id, out value);
		}
		value = default(T);
		return false;
	}
}
