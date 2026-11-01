using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Intersect.Collections;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public sealed class PacketTypeRegistry
{
	private Assembly _builtinAssembly;

	private ILogger Logger { get; }

	private List<Type> BuiltInTypesInternal { get; }

	private List<Type> TypesInternal { get; }

	public IReadOnlyList<Type> BuiltInTypes { get; }

	public IReadOnlyList<Type> Types { get; }

	public PacketTypeRegistry(ILogger logger, Assembly builtinAssembly)
	{
		Logger = logger ?? throw new ArgumentNullException("logger");
		_builtinAssembly = builtinAssembly;
		BuiltInTypesInternal = new List<Type>();
		BuiltInTypes = BuiltInTypesInternal.WrapReadOnly();
		TypesInternal = new List<Type>();
		Types = TypesInternal.WrapReadOnly();
	}

	public bool IsRegistered(Type type)
	{
		if (!(null == type))
		{
			return TypesInternal.Contains(type);
		}
		throw new ArgumentNullException("type");
	}

	public bool TryRegisterBuiltIn()
	{
		Type[] exportedTypes = _builtinAssembly.GetExportedTypes();
		IEnumerable<Type> source = exportedTypes.Where((Type type) => type.GetInterfaces().Any((Type interfaceType) => interfaceType == typeof(IPacket)));
		IOrderedEnumerable<Type> collection = source.Where((Type type) => !type.IsInterface && !type.IsValueType && !type.IsAbstract).SelectMany(delegate(Type type)
		{
			if (!type.IsGenericType)
			{
				return Enumerable.Empty<Type>().Append(type);
			}
			IEnumerable<GenericPacketTypeArgumentsAttribute> customAttributes = type.GetCustomAttributes<GenericPacketTypeArgumentsAttribute>();
			return customAttributes.Select((GenericPacketTypeArgumentsAttribute attribute) => type.MakeGenericType(attribute.TypeArguments));
		}).OrderBy<Type, string>((Type type) => type.GetName(qualified: true), CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CompareOptions.Ordinal));
		BuiltInTypesInternal.AddRange(collection);
		if (BuiltInTypesInternal.All(TryRegister))
		{
			return BuiltInTypesInternal.Count > 0;
		}
		return false;
	}

	public bool TryRegister<TPacket>() where TPacket : IPacket
	{
		return TryRegister(typeof(TPacket));
	}

	public bool TryRegister(Type packetType)
	{
		try
		{
			Register(packetType);
			return true;
		}
		catch (Exception ex)
		{
			if (ex is ArgumentNullException || ex is ArgumentException || ex is InvalidOperationException)
			{
				Logger.LogError(ex, "Failed to register packet type {Type}", packetType.GetName(qualified: true));
				return false;
			}
			throw;
		}
	}

	internal void Register(Type packetType)
	{
		if (null == packetType)
		{
			throw new ArgumentNullException("packetType");
		}
		if (!packetType.GetInterfaces().Any((Type baseInterface) => typeof(IPacket) == baseInterface))
		{
			throw new ArgumentException($"Packet type '{packetType.FullName}' does not inherit from '{typeof(IPacket).FullName}'.", "packetType");
		}
		if (packetType.IsAbstract)
		{
			throw new ArgumentException("Abstract packet types are not allowed: " + packetType.FullName, "packetType");
		}
		if (packetType.IsInterface)
		{
			throw new ArgumentException("Interfaces cannot be packets: " + packetType.FullName, "packetType");
		}
		if (packetType.IsValueType)
		{
			throw new ArgumentException("Value types (structs) cannot be packets: " + packetType.FullName, "packetType");
		}
		if (TypesInternal.Contains(packetType))
		{
			throw new InvalidOperationException("The packet registry already contains '" + packetType.FullName + "'.");
		}
		TypesInternal.Add(packetType);
	}
}
