using System;
using System.Linq;
using System.Reflection;
using Intersect.Collections;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Models;

namespace Intersect.GameObjects.Annotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class EditorReferenceAttribute : EditorDisplayAttribute
{
	private readonly PropertyInfo _referenceProperty;

	public Type DescriptorType { get; }

	public string ReferenceName { get; }

	public EditorReferenceAttribute(Type descriptorType, string referenceName)
	{
		DescriptorType = descriptorType ?? throw new ArgumentNullException("descriptorType");
		if (string.IsNullOrEmpty(referenceName))
		{
			throw new ArgumentNullException("referenceName");
		}
		ReferenceName = referenceName;
		if (DescriptorType.BaseType.GetGenericTypeDefinition() != typeof(DatabaseObject<>))
		{
			throw new ArgumentException("Invalid descriptor type '" + DescriptorType.FullName + "'.", "descriptorType");
		}
		_referenceProperty = DescriptorType.GetProperty(ReferenceName);
		if (_referenceProperty == null)
		{
			throw new ArgumentException($"'{referenceName}' does not exist on {descriptorType.FullName}.", "referenceName");
		}
	}

	[Obsolete("We want to re-implement strings to be object-oriented.")]
	public override string Format(Type stringsType, object value)
	{
		if (value is Guid)
		{
			Guid guid = (Guid)value;
			if (DescriptorType == typeof(MapDescriptor))
			{
				string text = MapList.OrderedMaps.FirstOrDefault((MapListMap map) => map.MapId == guid)?.Name;
				if (text != null)
				{
					return text;
				}
			}
			else
			{
				if (!(DescriptorType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).FirstOrDefault((FieldInfo fieldInfo2) => fieldInfo2.FieldType == typeof(DatabaseObjectLookup))?.GetValue(null) is DatabaseObjectLookup databaseObjectLookup))
				{
					throw new InvalidOperationException();
				}
				if (databaseObjectLookup.TryGetValue(guid, out var value2))
				{
					return value2.Name;
				}
			}
			if (stringsType == null)
			{
				throw new ArgumentNullException("stringsType");
			}
			FieldInfo fieldInfo = stringsType.GetNestedType("General")?.GetField("None");
			if (fieldInfo == null)
			{
				throw new InvalidOperationException();
			}
			return fieldInfo?.GetValue(null)?.ToString();
		}
		throw new ArgumentException("Invalid value type " + value?.GetType().FullName, "value");
	}
}
