using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Intersect.Framework.Reflection;
using Newtonsoft.Json.Serialization;

namespace Intersect.Framework.Core.Serialization;

public sealed class IntersectTypeSerializationBinder : ISerializationBinder
{
	private readonly DefaultSerializationBinder _defaultSerializationBinder = new DefaultSerializationBinder();

	public Type BindToType(string assemblyName, string qualifiedTypeName)
	{
		if (!assemblyName.StartsWith("Intersect"))
		{
			return _defaultSerializationBinder.BindToType(assemblyName, qualifiedTypeName);
		}
		Type type = Type.GetType(qualifiedTypeName);
		if ((object)type != null)
		{
			return type;
		}
		if (!(assemblyName == "Intersect Core") && !(assemblyName == "Intersect.Framework.Core"))
		{
			throw new NotImplementedException($"{"assemblyName"}=\"{assemblyName}\", {"qualifiedTypeName"}=\"{qualifiedTypeName}\"");
		}
		string[] array = qualifiedTypeName.Split('.');
		string typeName = qualifiedTypeName.Split('.').Last();
		Assembly assembly = Assembly.Load("Intersect.Framework.Core");
		Type[] types = assembly.GetTypes();
		Type[] array2 = types.Where((Type t) => typeName.Equals(t.Name)).ToArray();
		int num = array2.Length;
		if (num >= 1)
		{
			if (num <= 2)
			{
				if (num == 1)
				{
					return array2.First();
				}
				throw new UnreachableException("There should be no uncovered value");
			}
			throw new InvalidOperationException("Multiple matching types for " + qualifiedTypeName + ":\n" + string.Join("\n", array2.Select((Type t) => "\t" + t.GetName(qualified: true))));
		}
		throw new InvalidOperationException("No matching types for " + qualifiedTypeName);
	}

	public void BindToName(Type serializedType, [UnscopedRef] out string assemblyName, [UnscopedRef] out string typeName)
	{
		_defaultSerializationBinder.BindToName(serializedType, out assemblyName, out typeName);
	}
}
