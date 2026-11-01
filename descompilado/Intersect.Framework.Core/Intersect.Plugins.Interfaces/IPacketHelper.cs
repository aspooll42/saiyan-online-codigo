using System;
using System.Collections.Generic;
using Intersect.Network;

namespace Intersect.Plugins.Interfaces;

public interface IPacketHelper
{
	PacketTypeRegistry TypeRegistry { get; }

	PacketHandlerRegistry HandlerRegistry { get; }

	IReadOnlyList<Type> AvailablePacketTypes { get; }

	IReadOnlyList<Type> BuiltInPacketTypes { get; }

	IReadOnlyList<Type> AllPluginPacketTypes { get; }

	IReadOnlyList<Type> CurrentPluginPacketTypes { get; }

	bool TryRegisterPacketType<TPacket>() where TPacket : IPacket;

	bool TryRegisterPacketPreprocessor<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket;

	bool TryRegisterPacketHandler<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket;

	bool TryRegisterPacketPreHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket;

	bool TryRegisterPacketPostHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket;
}
