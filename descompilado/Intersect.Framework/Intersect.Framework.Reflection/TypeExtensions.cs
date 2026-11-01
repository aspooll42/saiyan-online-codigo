using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Reflection;

public static class TypeExtensions
{
	public static string[] GetMappedColumnNames(this Type type)
	{
		return (from propertyInfo in type.GetProperties()
			where propertyInfo.GetCustomAttribute<NotMappedAttribute>() == null
			select propertyInfo).GroupBy(delegate(PropertyInfo propertyInfo)
		{
			ForeignKeyAttribute customAttribute = propertyInfo.GetCustomAttribute<ForeignKeyAttribute>();
			return (customAttribute != null) ? customAttribute.Name : propertyInfo.Name;
		}).SelectMany(delegate(IGrouping<string, PropertyInfo> group)
		{
			PropertyInfo[] array = group.ToArray();
			return (array.Length > 1) ? (from propertyInfo in array
				where propertyInfo.PropertyType.IsValueType
				select propertyInfo.Name) : array.SelectMany(delegate(PropertyInfo propertyInfo)
			{
				if (propertyInfo.PropertyType.IsValueType)
				{
					return new global::_003C_003Ez__ReadOnlySingleElementList<string>(propertyInfo.Name);
				}
				return (!propertyInfo.PropertyType.IsClass || propertyInfo.PropertyType.IsAbstract || propertyInfo.PropertyType.GetCustomAttribute<OwnedAttribute>() == null) ? Array.Empty<string>() : (from name in propertyInfo.PropertyType.GetMappedColumnNames()
					select string.Join('_', propertyInfo.Name, name));
			});
		}).ToArray();
	}

	public static bool IsEnumerable(this Type type)
	{
		return typeof(IEnumerable<>).ExtendedBy(type);
	}

	public static bool IsEnumerable<TValue>(this Type type)
	{
		return typeof(IEnumerable<TValue>).ExtendedBy(type);
	}

	public static bool IsIntegral(this Type type)
	{
		if (!(type == typeof(int)) && !(type == typeof(long)) && !(type == typeof(short)) && !(type == typeof(byte)) && !(type == typeof(uint)) && !(type == typeof(ulong)) && !(type == typeof(ushort)))
		{
			return type == typeof(sbyte);
		}
		return true;
	}

	public static bool IsFloatingPoint(this Type type)
	{
		if (!(type == typeof(float)))
		{
			return type == typeof(double);
		}
		return true;
	}

	public static bool IsOwnProperty(this Type type, PropertyInfo propertyInfo)
	{
		return !propertyInfo.IsPropertyDeclaredInBaseTypeOrInterface(type);
	}

	public static bool TryFindProperty(this Type type, string propertyName, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
	{
		return type.TryFindProperty(propertyName, BindingFlags.Instance | BindingFlags.Public, out propertyInfo);
	}

	public static bool TryFindProperty(this Type type, string propertyName, BindingFlags bindingFlags, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
	{
		try
		{
			propertyInfo = type.GetProperty(propertyName, bindingFlags);
		}
		catch (AmbiguousMatchException)
		{
			PropertyInfo[] source = (from propertyInfo2 in type.GetProperties(bindingFlags)
				where string.Equals(propertyName, propertyInfo2.Name, StringComparison.Ordinal)
				select propertyInfo2).ToArray();
			propertyInfo = source.FirstOrDefault((PropertyInfo propertyInfo2) => propertyInfo2.DeclaringType == type) ?? source.First();
		}
		if (!type.IsInterface || propertyInfo != null)
		{
			return propertyInfo != null;
		}
		if (!bindingFlags.HasFlag(BindingFlags.Public) || !bindingFlags.HasFlag(BindingFlags.Instance))
		{
			return propertyInfo != null;
		}
		Type[] interfaces = type.GetInterfaces();
		Type[] array = interfaces;
		foreach (Type type2 in array)
		{
			propertyInfo = type2.GetProperty(propertyName);
			if (propertyInfo != null)
			{
				return true;
			}
		}
		return false;
	}

	public static string QualifiedGenericName(this Type type)
	{
		return type.Name + "<" + string.Join(", ", type.GenericTypeArguments.Select((Type parameterType) => parameterType.Name)) + ">";
	}

	public static IEnumerable<ConstructorInfo> FindConstructors(this Type type, params object[] parameters)
	{
		return type.GetConstructors().Where(delegate(ConstructorInfo constructor)
		{
			ParameterInfo[] parameters2 = constructor.GetParameters();
			if (parameters2.Length < parameters.Length)
			{
				return false;
			}
			for (int i = 0; i < parameters2.Length; i++)
			{
				ParameterInfo parameterInfo = parameters2[i];
				if (i >= parameters.Length)
				{
					return parameterInfo.IsOptional;
				}
				object obj = parameters[i];
				if (obj == null)
				{
					if (parameterInfo.ParameterType.IsValueType)
					{
						return false;
					}
				}
				else
				{
					Type type2 = obj.GetType();
					if (!parameterInfo.ParameterType.IsAssignableFrom(type2))
					{
						return false;
					}
				}
			}
			return true;
		});
	}

	public static bool Extends(this Type childType, Type baseType)
	{
		if (!baseType.IsGenericTypeDefinition)
		{
			return baseType.IsAssignableFrom(childType);
		}
		if (childType.IsGenericType && baseType.MakeGenericType(childType.GenericTypeArguments).IsAssignableFrom(childType))
		{
			return true;
		}
		if (baseType.IsInterface)
		{
			return false;
		}
		Type type = childType;
		while (type != null)
		{
			if (type.IsGenericType)
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				if (type == genericTypeDefinition)
				{
					break;
				}
				type = genericTypeDefinition;
				if (type == baseType)
				{
					return true;
				}
			}
			else
			{
				type = type.BaseType;
			}
		}
		return false;
	}

	public static bool Extends<TBaseType>(this Type type)
	{
		return type.Extends(typeof(TBaseType));
	}

	public static bool ExtendedBy<TChildType>(this Type type)
	{
		return typeof(TChildType).Extends(type);
	}

	public static bool ExtendedBy(this Type type, Type? childType)
	{
		return childType?.Extends(type) ?? false;
	}

	public static Type? FindConcreteType(this Type abstractType, Func<Type, bool> predicate, bool allLoadedAssemblies = false)
	{
		if ((object)abstractType != null && !abstractType.IsAbstract && !abstractType.IsInterface)
		{
			throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindConcreteType_ExpectedAbstractOrInterface, abstractType.FullName), "abstractType");
		}
		Assembly[] source = (allLoadedAssemblies ? AppDomain.CurrentDomain.GetAssemblies() : new Assembly[1] { abstractType.Assembly });
		IEnumerable<Assembly> source2 = source.Where((Assembly assembly) => !assembly.IsDynamic);
		IEnumerable<Type> source3 = source2.SelectMany(delegate(Assembly assembly)
		{
			try
			{
				return assembly.ExportedTypes;
			}
			catch
			{
				return Array.Empty<Type>();
			}
		});
		IEnumerable<Type> source4 = source3.Where((Type type) => (object)type != null && !type.IsAbstract && !type.IsInterface && !type.IsGenericType);
		IEnumerable<Type> source5 = source4.Where((Type type) => type.Extends(abstractType));
		return source5.FirstOrDefault(predicate);
	}

	public static Type[] FindDerivedTypes(this Type type, params Assembly[] assemblies)
	{
		Assembly[] array = assemblies;
		if (array.Length < 1)
		{
			array = AppDomain.CurrentDomain.GetAssemblies();
		}
		return (from propertyInfo in array.Where((Assembly assembly) => !assembly.IsDynamic).SelectMany(delegate(Assembly assembly)
			{
				try
				{
					return assembly.ExportedTypes;
				}
				catch
				{
					return Array.Empty<Type>();
				}
			}).SelectMany((Type assemblyType) => assemblyType.GetProperties())
			select propertyInfo.PropertyType into propertyType
			where propertyType.Extends(type)
			select propertyType).Distinct().ToArray();
	}

	public static Type? FindGenericType(this Type type)
	{
		return type.FindGenericType(throwOnNonGeneric: true);
	}

	public static Type? FindGenericType(this Type type, bool throwOnNonGeneric)
	{
		return type.FindGenericType(null, throwOnNonGeneric);
	}

	public static Type? FindGenericType(this Type type, Type? genericTypeDefinition, bool throwOnNonGeneric)
	{
		if ((object)genericTypeDefinition != null && !genericTypeDefinition.IsGenericTypeDefinition)
		{
			throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindGenericTypeParameters_NotValidGenericTypeDefinition, genericTypeDefinition.FullName), "genericTypeDefinition");
		}
		Type type2 = type;
		while (type2 != null)
		{
			if (genericTypeDefinition != null)
			{
				if (genericTypeDefinition.IsInterface)
				{
					Type type3 = type2.GetInterfaces().FirstOrDefault((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericTypeDefinition);
					if (type3 != null)
					{
						return type3;
					}
				}
				else if (type2.IsGenericType && type2.GetGenericTypeDefinition() == genericTypeDefinition)
				{
					return type2;
				}
			}
			else if (type2.IsGenericType)
			{
				return type2;
			}
			type2 = type2.BaseType;
		}
		if (!throwOnNonGeneric)
		{
			return null;
		}
		throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindGenericTypeParameters_NotGeneric, type.FullName), "type");
	}

	public static Type? FindGenericInterfaceForDefinition(this Type type, Type genericTypeDefinition)
	{
		return type.GetInterfaces().FirstOrDefault((Type i) => i.Extends(genericTypeDefinition));
	}

	public static Type? FindGenericTypeDefinition(this Type type)
	{
		return type.FindGenericTypeDefinition(throwOnNonGeneric: true);
	}

	public static Type? FindGenericTypeDefinition(this Type type, bool throwOnNonGeneric)
	{
		if (type.IsGenericTypeDefinition)
		{
			return type;
		}
		Type type2 = type;
		while (type2 != null)
		{
			if (type2.IsGenericType)
			{
				return type2.GetGenericTypeDefinition();
			}
			type2 = type2.BaseType;
		}
		if (!throwOnNonGeneric)
		{
			return null;
		}
		throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindGenericTypeParameters_NotGeneric, type.FullName), "type");
	}

	public static Type[] GetUniqueInterfaces(this Type type)
	{
		HashSet<Type> hashSet = type.GetInterfaces().ToHashSet();
		Type[] second = hashSet.SelectMany((Type interfaceType) => interfaceType.GetInterfaces()).Distinct().ToArray();
		return hashSet.Except(second).Except(type.BaseType?.GetUniqueInterfaces() ?? Array.Empty<Type>()).ToArray();
	}

	public static Type[] FindGenericTypeParameters(this Type type)
	{
		return type.FindGenericTypeParameters(throwOnNonGeneric: true);
	}

	public static Type[] FindGenericTypeParameters(this Type type, bool throwOnNonGeneric)
	{
		return type.FindGenericTypeParameters(null, throwOnNonGeneric);
	}

	public static Type[] FindGenericTypeParameters(this Type type, Type? genericTypeDefinition, bool throwOnNonGeneric = true)
	{
		if ((object)genericTypeDefinition != null && !genericTypeDefinition.IsGenericTypeDefinition)
		{
			throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindGenericTypeParameters_NotValidGenericTypeDefinition, genericTypeDefinition.FullName), "genericTypeDefinition");
		}
		Type type2 = type;
		while (type2 != null)
		{
			if (genericTypeDefinition != null)
			{
				if (genericTypeDefinition.IsInterface)
				{
					Type type3 = type2.GetInterfaces().FirstOrDefault((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericTypeDefinition);
					if (type3 != null)
					{
						return type3.GetGenericArguments();
					}
				}
				else if (type2.IsGenericType && type2.GetGenericTypeDefinition() == genericTypeDefinition)
				{
					return type2.GetGenericArguments();
				}
			}
			else if (type2.IsGenericType)
			{
				return type2.GetGenericArguments();
			}
			type2 = type2.BaseType;
		}
		if (!throwOnNonGeneric)
		{
			return Array.Empty<Type>();
		}
		throw new ArgumentException(string.Format(ReflectionStrings.TypeExtensions_FindGenericTypeParameters_NotGeneric, type.FullName), "type");
	}

	public static Type[] FindImplementationsIn(this Type incompleteType, IEnumerable<Type> types)
	{
		if ((object)incompleteType != null && !incompleteType.IsAbstract && !incompleteType.IsInterface)
		{
			throw new ArgumentException(string.Format(ReflectionStrings.ExpectedAnIncompleteTypeButReceivedX, incompleteType.FullName), "incompleteType");
		}
		return types.Where((Type type) => (object)type != null && !type.IsAbstract && type.IsClass && !type.IsGenericTypeDefinition && !type.IsInterface && type.Extends(incompleteType)).ToArray();
	}

	[return: NotNullIfNotNull("fallbackIfNull")]
	public static string? GetAssemblyName(this Type type, string? fallbackIfNull = null)
	{
		return type.Assembly.GetName().Name ?? fallbackIfNull;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetName(this Type type, bool qualified = false)
	{
		if (!qualified)
		{
			return type.Name;
		}
		return type.GetQualifiedName();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetQualifiedName(this Type type)
	{
		return type.FullName ?? type.Name;
	}

	public static IEnumerable<object> GetValues(this Type type, bool excludeIgnored = false)
	{
		if (!type.IsEnum)
		{
			throw new ArgumentException("Expected an enum but received " + type.GetName(qualified: true), "type");
		}
		Array values = Enum.GetValues(type);
		Enum[] array = values.OfType<Enum>().Distinct().ToArray();
		if (excludeIgnored)
		{
			HashSet<Enum> ignoredValues = (from field in type.GetFields(BindingFlags.Static | BindingFlags.Public)
				where field.FieldType == type && field.IsIgnored()
				select field.GetValue(null)).OfType<Enum>().ToHashSet();
			array = array.Where((Enum value) => !ignoredValues.Contains(value)).ToArray();
		}
		return array;
	}

	public static IEnumerable<TEnum> GetValues<TEnum>(this Type type, bool excludeIgnored = false) where TEnum : struct, Enum
	{
		if (typeof(TEnum) != type)
		{
			throw new ArgumentException("The generic type argument " + typeof(TEnum).GetName(qualified: true) + " does not match the argument " + type.GetName(qualified: true), "type");
		}
		if (!type.IsEnum)
		{
			throw new ArgumentException("Expected an enum but received " + type.GetName(qualified: true), "type");
		}
		TEnum[] array = Enum.GetValues<TEnum>();
		if (excludeIgnored)
		{
			HashSet<Enum> ignoredValues = (from field in type.GetFields(BindingFlags.Static | BindingFlags.Public)
				where field.FieldType == type && field.IsIgnored()
				select field.GetValue(null)).OfType<Enum>().ToHashSet();
			array = array.Where((TEnum value) => !ignoredValues.Contains(value)).ToArray();
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBitflags(this Type type)
	{
		if (type.IsEnum)
		{
			return type.GetCustomAttribute<FlagsAttribute>() != null;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsConcrete(this Type type)
	{
		if ((object)type != null && !type.IsInterface && !type.IsAbstract)
		{
			return type.IsClass;
		}
		return false;
	}
}
