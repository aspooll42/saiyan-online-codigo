using System;
using System.Collections.Generic;
using System.Linq;
using Ceras;

namespace Intersect.Network;

internal class CerasTypeBinder : ITypeBinder
{
	private Dictionary<string, Type> _nameToType = new Dictionary<string, Type>();

	public CerasTypeBinder(Dictionary<string, Type> nameTypeMap)
	{
		_nameToType = nameTypeMap;
	}

	public string GetBaseName(Type type)
	{
		return _nameToType.Keys.FirstOrDefault((string k) => _nameToType[k] == type);
	}

	public Type GetTypeFromBase(string baseTypeName)
	{
		return _nameToType[baseTypeName];
	}

	public Type GetTypeFromBaseAndArguments(string baseTypeName, params Type[] genericTypeArguments)
	{
		throw new NotSupportedException("this binder is only for debugging");
	}
}
