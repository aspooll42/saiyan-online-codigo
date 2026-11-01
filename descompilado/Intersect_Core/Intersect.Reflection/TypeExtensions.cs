using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Intersect.Reflection;

public static class TypeExtensions
{
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
		Type type = childType;
		while (type != null)
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
			{
				return true;
			}
			type = type.BaseType;
		}
		return false;
	}

	public static bool Extends<TBaseType>(this Type type)
	{
		return type.Extends(typeof(TBaseType));
	}

	public static bool ExtendedBy<TChildType>(this Type type)
	{
		return type.ExtendedBy(typeof(TChildType));
	}

	public static bool ExtendedBy(this Type type, Type childType)
	{
		return childType.Extends(type);
	}

	public static Type? FindConcreteType(this Type abstractType, Func<Type, bool> predicate, bool allLoadedAssemblies = false)
	{
		if (!abstractType.IsAbstract && !abstractType.IsInterface)
		{
			throw new ArgumentException("Expected abstract/interface type, received " + abstractType.FullName, "abstractType");
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
				return Enumerable.Empty<Type>();
			}
		});
		IEnumerable<Type> source4 = source3.Where((Type type) => !type.IsAbstract && !type.IsInterface && !type.IsGenericType);
		IEnumerable<Type> source5 = source4.Where((Type type) => type.Extends(abstractType));
		return source5.FirstOrDefault(predicate);
	}

	public static Type FindGenericType(this Type type)
	{
		return type.FindGenericType(throwOnNonGeneric: true);
	}

	public static Type? FindGenericType(this Type type, bool throwOnNonGeneric)
	{
		return type.FindGenericType(null, throwOnNonGeneric);
	}

	public static Type? FindGenericType(this Type type, Type genericTypeDefinition, bool throwOnNonGeneric)
	{
		if (genericTypeDefinition != null && !genericTypeDefinition.IsGenericTypeDefinition)
		{
			throw new ArgumentException("Not a valid generic type definition: " + genericTypeDefinition.FullName, "genericTypeDefinition");
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
		throw new ArgumentException(type.FullName + " is not a generic type and does not extend from a generic type.", "type");
	}

	public static Type FindGenericTypeDefinition(this Type type)
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
		throw new ArgumentException(type.FullName + " is not a generic type and does not extend from a generic type.", "type");
	}

	public static Type[] FindGenericTypeParameters(this Type type)
	{
		return type.FindGenericTypeParameters(throwOnNonGeneric: true);
	}

	public static Type[] FindGenericTypeParameters(this Type type, bool throwOnNonGeneric)
	{
		return type.FindGenericTypeParameters(null, throwOnNonGeneric);
	}

	public static Type[] FindGenericTypeParameters(this Type type, Type genericTypeDefinition, bool throwOnNonGeneric = true)
	{
		if (genericTypeDefinition != null && !genericTypeDefinition.IsGenericTypeDefinition)
		{
			throw new ArgumentException("Not a valid generic type definition: " + genericTypeDefinition.FullName, "genericTypeDefinition");
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
		throw new ArgumentException(type.FullName + " is not a generic type and does not extend from a generic type.", "type");
	}
}
