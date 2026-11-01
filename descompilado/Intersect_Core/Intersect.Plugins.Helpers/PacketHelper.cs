using System;
using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Network;
using Intersect.Plugins.Interfaces;

namespace Intersect.Plugins.Helpers;

public sealed class PacketHelper : IPacketHelper
{
	private PacketHelper Parent { get; }

	private List<Type> PluginPacketTypes { get; }

	public PacketHandlerRegistry HandlerRegistry { get; }

	public PacketTypeRegistry TypeRegistry { get; }

	public IReadOnlyList<Type> AvailablePacketTypes => TypeRegistry.Types;

	public IReadOnlyList<Type> BuiltInPacketTypes => TypeRegistry.BuiltInTypes;

	public IReadOnlyList<Type> AllPluginPacketTypes => (Parent?.PluginPacketTypes ?? PluginPacketTypes).WrapReadOnly();

	public IReadOnlyList<Type> CurrentPluginPacketTypes => PluginPacketTypes.WrapReadOnly();

	public PacketHelper(PacketTypeRegistry packetTypeRegistry, PacketHandlerRegistry packetHandlerRegistry)
	{
		Parent = null;
		HandlerRegistry = packetHandlerRegistry ?? throw new ArgumentNullException("packetHandlerRegistry");
		TypeRegistry = packetTypeRegistry ?? throw new ArgumentNullException("packetTypeRegistry");
		PluginPacketTypes = new List<Type>();
	}

	public PacketHelper(IPacketHelper parentPacketHelper)
	{
		if (parentPacketHelper == null)
		{
			throw new ArgumentNullException("parentPacketHelper");
		}
		if (!(parentPacketHelper is PacketHelper packetHelper))
		{
			throw new ArgumentException($"This constructor can only be used if {"parentPacketHelper"} is a {typeof(PacketHelper).FullName}.", "parentPacketHelper");
		}
		Parent = packetHelper;
		HandlerRegistry = Parent.HandlerRegistry;
		TypeRegistry = Parent.TypeRegistry;
		PluginPacketTypes = new List<Type>();
	}

	public bool TryRegisterPacketHandler<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return HandlerRegistry.TryRegisterHandler<THandler>(out handler);
	}

	public bool TryRegisterPacketPostHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return HandlerRegistry.TryRegisterPostHook<THandler, TPacket>(out handler);
	}

	public bool TryRegisterPacketPreHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return HandlerRegistry.TryRegisterPreHook<THandler, TPacket>(out handler);
	}

	public bool TryRegisterPacketPreprocessor<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return HandlerRegistry.TryRegisterPreHook<THandler, TPacket>(out handler);
	}

	public bool TryRegisterPacketType<TPacket>() where TPacket : IPacket
	{
		if (TypeRegistry.TryRegister<TPacket>())
		{
			PluginPacketTypes.Add(typeof(TPacket));
			Parent?.PluginPacketTypes?.Add(typeof(TPacket));
			return true;
		}
		return false;
	}
}
