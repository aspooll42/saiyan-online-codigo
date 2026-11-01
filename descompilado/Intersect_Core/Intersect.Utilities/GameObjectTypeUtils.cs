using System;
using System.Collections.Generic;
using System.Data;
using Intersect.Enums;

namespace Intersect.Utilities;

public static class GameObjectTypeUtils
{
	private static readonly IDictionary<string, GameObjectType> mNameToTypeCache;

	static GameObjectTypeUtils()
	{
		mNameToTypeCache = new Dictionary<string, GameObjectType>();
		Type typeFromHandle = typeof(GameObjectType);
		Array values = Enum.GetValues(typeFromHandle);
		foreach (GameObjectType item in values)
		{
			string name = Enum.GetName(typeFromHandle, item);
			if (name == null)
			{
				throw new NoNullAllowedException();
			}
			mNameToTypeCache[name] = item;
			mNameToTypeCache[name.ToLower()] = item;
			mNameToTypeCache[name.ToUpper()] = item;
		}
	}

	public static GameObjectType TypeFromName(string name)
	{
		GameObjectType value = GameObjectType.Animation;
		if (mNameToTypeCache.TryGetValue(name, out value))
		{
			return value;
		}
		if (mNameToTypeCache.TryGetValue(name.ToLower(), out value))
		{
			mNameToTypeCache[name.ToLower()] = value;
			return value;
		}
		if (mNameToTypeCache.TryGetValue(name.ToUpper(), out value))
		{
			mNameToTypeCache[name.ToUpper()] = value;
			return value;
		}
		throw new GameObjectTypeException("GameObjectType with name '" + name + "' not found.");
	}
}
