using System;
using System.Linq;
using System.Reflection;
using Intersect.Framework.Reflection;

namespace Intersect.Network;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = false)]
public class PacketHandlerAttribute : Attribute
{
	internal static Type TypeIPacketHandlerInterface_1 = typeof(IPacketHandler<>);

	internal static Type TypeIPacket = typeof(IPacket);

	internal static Type TypeIPacketSender = typeof(IPacketSender);

	public Type PacketType { get; }

	public PacketHandlerAttribute(Type packetType)
	{
		if (packetType == null)
		{
			throw new ArgumentNullException("packetType");
		}
		if (!typeof(IPacket).IsAssignableFrom(packetType))
		{
			throw new ArgumentException(packetType.FullName + " does not implement " + typeof(IPacket).FullName + ".", "packetType");
		}
		PacketType = packetType;
	}

	public static Type GetPacketType(MethodInfo methodInfo)
	{
		if (null == methodInfo)
		{
			throw new ArgumentNullException("methodInfo");
		}
		Type[] array = (from parameter in methodInfo.GetParameters()
			select parameter.ParameterType).ToArray();
		if (array.Length != 2)
		{
			throw new ArgumentOutOfRangeException("methodInfo", "Invalid packet handler '" + methodInfo.GetFullSignature() + "'.");
		}
		Type type = array[0];
		if (!TypeIPacketSender.IsAssignableFrom(type))
		{
			throw new ArgumentException($"Invalid sender parameter type ({type.FullName}) in handler '{methodInfo.GetFullSignature()}'.", "methodInfo");
		}
		Type type2 = array[1];
		if (type2.IsInterface || type2.IsAbstract || (type2.IsGenericType && !type2.IsConstructedGenericType) || !TypeIPacket.IsAssignableFrom(type2))
		{
			throw new ArgumentException($"Invalid packet parameter type ({type2.FullName}) in handler '{methodInfo.GetFullSignature()}'.", "methodInfo");
		}
		Type type3 = ((Attribute.GetCustomAttribute(methodInfo, typeof(PacketHandlerAttribute)) is PacketHandlerAttribute packetHandlerAttribute) ? packetHandlerAttribute.PacketType : null);
		Type type4 = type3 ?? type2;
		if (type2 != type4)
		{
			throw new ArgumentException($"{methodInfo.Name} is a packet handler for {type2.FullName} but is marked with an attribute for {type3.FullName}.", "methodInfo");
		}
		return type4;
	}

	public static Type[] GetHandlerInterfaces(Type implementationType)
	{
		ArgumentNullException.ThrowIfNull(implementationType, "implementationType");
		Type[] array = (from interfaceType in implementationType.GetInterfaces()
			where interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == TypeIPacketHandlerInterface_1
			select interfaceType).ToArray();
		if (array.Length < 1)
		{
			throw new ArgumentException(implementationType.GetName(qualified: true) + " does not directly implement " + TypeIPacketHandlerInterface_1.GetName(qualified: true), "implementationType");
		}
		return array;
	}

	public static Type[] GetPacketTypes<THandler>() where THandler : IPacketHandler
	{
		return GetPacketTypes(typeof(THandler));
	}

	public static Type[] GetPacketTypes(Type packetHandlerType)
	{
		ArgumentNullException.ThrowIfNull(packetHandlerType, "packetHandlerType");
		Type[] handlerInterfaces = GetHandlerInterfaces(packetHandlerType);
		if (handlerInterfaces.Length < 1)
		{
			throw new ArgumentException(packetHandlerType.GetName(qualified: true) + " does not implement " + TypeIPacketHandlerInterface_1.GetName(qualified: true) + ".", "packetHandlerType");
		}
		Type[] source = handlerInterfaces.SelectMany((Type interfaceType) => interfaceType.GenericTypeArguments).Distinct().ToArray();
		Type[] array = source.Where((Type packetTypeFromType) => (object)packetTypeFromType != null && !packetTypeFromType.IsInterface && !packetTypeFromType.IsAbstract && !packetTypeFromType.IsGenericType && packetTypeFromType.Extends(TypeIPacket)).ToArray();
		if (array.Length < 1)
		{
			throw new ArgumentException(packetHandlerType.GetName(qualified: true) + " handles no packet types", "packetHandlerType");
		}
		Type type = ((Attribute.GetCustomAttribute(packetHandlerType, typeof(PacketHandlerAttribute)) is PacketHandlerAttribute packetHandlerAttribute) ? packetHandlerAttribute.PacketType : null);
		Type[] array2 = ((type == null) ? array : new Type[1] { type });
		if (!array.SequenceEqual(array2))
		{
			string name = packetHandlerType.GetName(qualified: true);
			string value = string.Join(", ", array.Select((Type type2) => type2.GetName(qualified: true)));
			string value2 = string.Join(", ", array2.Select((Type type2) => type2.GetName(qualified: true)));
			throw new ArgumentException($"{name} is a packet handler for {value} but is marked with an attribute for {value2}.", "packetHandlerType");
		}
		return array2;
	}

	public static bool IsValidHandler(MethodInfo methodInfo)
	{
		if (methodInfo == null)
		{
			throw new ArgumentNullException("methodInfo");
		}
		try
		{
			return GetPacketType(methodInfo) != null;
		}
		catch (ArgumentNullException)
		{
			throw;
		}
		catch
		{
			return false;
		}
	}

	public static bool IsValidHandler(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type.IsInterface || type.IsAbstract || type.IsGenericType)
		{
			return false;
		}
		try
		{
			return GetPacketTypes(type).Length != 0;
		}
		catch (ArgumentNullException)
		{
			throw;
		}
		catch
		{
			return false;
		}
	}
}
