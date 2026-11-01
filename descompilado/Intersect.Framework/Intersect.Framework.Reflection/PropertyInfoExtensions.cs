using System;
using System.Linq;
using System.Reflection;

namespace Intersect.Framework.Reflection;

public static class PropertyInfoExtensions
{
	public static bool IsPropertyDeclaredInBaseTypeOrInterface(this PropertyInfo propertyInfo, Type type, bool checkSetter = false)
	{
		if (!(propertyInfo.DeclaringType != type))
		{
			return propertyInfo.IsPropertyDeclaredByInterface(type, checkSetter);
		}
		return true;
	}

	public static bool IsPropertyDeclaredByInterface(this PropertyInfo propertyInfo, bool checkSetter = false)
	{
		return propertyInfo.IsPropertyDeclaredByInterface(propertyInfo.DeclaringType ?? throw new ArgumentException("propertyInfo has a null DeclaringType", "propertyInfo"), checkSetter);
	}

	public static bool IsPropertyDeclaredByInterface<TSearchType>(this PropertyInfo propertyInfo, bool checkSetter = false)
	{
		return propertyInfo.IsPropertyDeclaredByInterface(typeof(TSearchType), checkSetter);
	}

	public static bool IsPropertyDeclaredByInterface(this PropertyInfo propertyInfo, Type searchType, bool checkSetter = false)
	{
		Type declaringType = propertyInfo.DeclaringType;
		if (declaringType == null)
		{
			return false;
		}
		MethodInfo searchMethodInfo;
		if (checkSetter)
		{
			searchMethodInfo = propertyInfo.SetMethod ?? throw new InvalidOperationException($"Tried to check setter of {declaringType.GetName(qualified: true)}.{propertyInfo.Name} which does not exist");
		}
		else
		{
			searchMethodInfo = propertyInfo.GetMethod ?? throw new InvalidOperationException($"Tried to check getter of {declaringType.GetName(qualified: true)}.{propertyInfo.Name} which does not exist");
		}
		Type[] array = searchType.GetInterfaces();
		if (searchType.IsInterface)
		{
			Type[] array2 = array;
			int num = 0;
			Type[] array3 = new Type[1 + array2.Length];
			array3[num] = searchType;
			num++;
			ReadOnlySpan<Type> readOnlySpan = new ReadOnlySpan<Type>(array2);
			readOnlySpan.CopyTo(new Span<Type>(array3).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			array = array3;
		}
		if (searchType.IsInterface)
		{
			return false;
		}
		Type[] array4 = array;
		foreach (Type interfaceType in array4)
		{
			InterfaceMapping interfaceMap = declaringType.GetInterfaceMap(interfaceType);
			if (interfaceMap.TargetMethods.Contains(searchMethodInfo))
			{
				return true;
			}
			if (interfaceMap.TargetMethods.Any((MethodInfo targetMethodInfo) => targetMethodInfo.MethodHandle == searchMethodInfo.MethodHandle))
			{
				return true;
			}
		}
		return false;
	}
}
