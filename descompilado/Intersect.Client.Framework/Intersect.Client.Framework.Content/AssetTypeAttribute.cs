using System;

namespace Intersect.Client.Framework.Content;

[AttributeUsage(AttributeTargets.Field)]
public class AssetTypeAttribute : Attribute
{
	public Type Type { get; }

	public AssetTypeAttribute(Type type)
	{
		if (!typeof(IAsset).IsAssignableFrom(type))
		{
			throw new ArgumentException($"Invalid asset type {type.FullName}. Must inherit from {"IAsset"}.", "type");
		}
		if (!type.IsClass)
		{
			throw new ArgumentException("Invalid asset type " + type.FullName + ". Must be a class (abstract is acceptable).", "type");
		}
		Type = type;
	}
}
