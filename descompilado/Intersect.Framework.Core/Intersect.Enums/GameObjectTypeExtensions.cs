using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Intersect.Collections;
using Intersect.Extensions;
using Intersect.Framework.Core.GameObjects.Variables;
using Intersect.Models;

namespace Intersect.Enums;

public static class GameObjectTypeExtensions
{
	private static readonly Dictionary<Type, GameObjectType> _gameObjectTypeLookup;

	private static Type EnumType { get; }

	private static Type AttributeType { get; }

	private static Dictionary<GameObjectType, GameObjectInfoAttribute> AttributeMap { get; }

	static GameObjectTypeExtensions()
	{
		_gameObjectTypeLookup = new Dictionary<Type, GameObjectType>();
		EnumType = typeof(GameObjectType);
		AttributeType = typeof(GameObjectInfoAttribute);
		AttributeMap = new Dictionary<GameObjectType, GameObjectInfoAttribute>();
		foreach (GameObjectType value in Enum.GetValues(EnumType))
		{
			GameObjectInfoAttribute gameObjectInfoAttribute = (GameObjectInfoAttribute)(EnumType.GetMember(Enum.GetName(EnumType, value)).FirstOrDefault()?.GetCustomAttributes(AttributeType, inherit: false).FirstOrDefault());
			AttributeMap[value] = gameObjectInfoAttribute;
			if (gameObjectInfoAttribute != null)
			{
				_gameObjectTypeLookup[gameObjectInfoAttribute.Type] = value;
			}
		}
	}

	public static bool TryGetGameObjectType(this Type type, out GameObjectType gameObjectType)
	{
		return _gameObjectTypeLookup.TryGetValue(type, out gameObjectType);
	}

	public static Type GetObjectType(this GameObjectType gameObjectType)
	{
		return AttributeMap?[gameObjectType]?.Type;
	}

	public static string GetTable(this GameObjectType gameObjectType)
	{
		return AttributeMap?[gameObjectType]?.Table;
	}

	public static DatabaseObjectLookup GetLookup(this GameObjectType gameObjectType)
	{
		return DatabaseObjectLookup.GetLookup(gameObjectType.GetObjectType());
	}

	public static bool TryGetLookup(this GameObjectType gameObjectType, [NotNullWhen(true)] out DatabaseObjectLookup? lookup)
	{
		lookup = gameObjectType.GetLookup();
		return lookup != null;
	}

	public static IDatabaseObject CreateNew(this GameObjectType gameObjectType)
	{
		object obj = Activator.CreateInstance(AttributeMap?[gameObjectType]?.Type, new object[0]);
		return obj as IDatabaseObject;
	}

	public static int ListIndex(this GameObjectType gameObjectType, Guid id, VariableDataType dataTypeFilter = VariableDataType.None)
	{
		DatabaseObjectLookup lookup = gameObjectType.GetLookup();
		if (dataTypeFilter == VariableDataType.None)
		{
			return lookup.KeyList.OrderBy((Guid pairs) => lookup[pairs]?.Name).ToList().IndexOf(id);
		}
		return (from descriptor in (from kv in lookup
				orderby kv.Value?.Name
				select kv.Value).OfType<IVariableDescriptor>()
			where dataTypeFilter == VariableDataType.None || descriptor.DataType == dataTypeFilter
			select descriptor.Id).ToList().IndexOf(id);
	}

	public static VariableDataType GetVariableType(this GameObjectType gameObjectType, Guid variableDescriptorId)
	{
		DatabaseObjectLookup lookup = gameObjectType.GetLookup();
		return lookup.ValueList.OfType<IVariableDescriptor>().FirstOrDefault((IVariableDescriptor descriptor) => descriptor.Id == variableDescriptorId)?.DataType ?? VariableDataType.None;
	}

	public static Guid IdFromList(this GameObjectType gameObjectType, int listIndex, VariableDataType dataTypeFilter = VariableDataType.None)
	{
		DatabaseObjectLookup lookup = gameObjectType.GetLookup();
		if (listIndex < 0 || listIndex >= lookup.KeyList.Count)
		{
			return Guid.Empty;
		}
		if (dataTypeFilter == VariableDataType.None)
		{
			return lookup.KeyList.OrderBy((Guid pairs) => lookup[pairs]?.Name).ToArray()[listIndex];
		}
		return (from descriptor in (from kv in lookup
				orderby kv.Value?.Name
				select kv.Value).OfType<IVariableDescriptor>()
			where dataTypeFilter == VariableDataType.None || descriptor.DataType == dataTypeFilter
			select descriptor.Id).Skip(listIndex).FirstOrDefault();
	}

	public static string[] Names(this GameObjectType gameObjectType, VariableDataType dataTypeFilter = VariableDataType.None)
	{
		if (dataTypeFilter == VariableDataType.None)
		{
			return (from pair in gameObjectType.GetLookup()
				orderby pair.Value?.Name
				select pair.Value?.Name ?? "ERR_DELETED").ToArray();
		}
		return (from descriptor in (from kv in gameObjectType.GetLookup()
				select kv.Value).OfType<IVariableDescriptor>()
			where dataTypeFilter == VariableDataType.None || descriptor.DataType == dataTypeFilter
			orderby descriptor.Name
			select descriptor.Name ?? "ERR_DELETED").ToArray();
	}
}
