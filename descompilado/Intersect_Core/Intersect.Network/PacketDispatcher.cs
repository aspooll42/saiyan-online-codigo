using System;
using System.Collections.Generic;
using System.Linq;

namespace Intersect.Network;

public sealed class PacketDispatcher
{
	private readonly IDictionary<Type, IList<HandlePacket>> mHandlers;

	public PacketDispatcher()
	{
		mHandlers = new Dictionary<Type, IList<HandlePacket>>();
	}

	private IList<HandlePacket> GetHandlers(Type type)
	{
		if (mHandlers == null)
		{
			throw new ArgumentNullException();
		}
		if (type == null)
		{
			throw new ArgumentNullException();
		}
		if (!mHandlers.TryGetValue(type, out var value))
		{
			value = new List<HandlePacket>();
			mHandlers.Add(type, value);
		}
		return value;
	}

	public bool RegisterHandler(Type type, HandlePacket handler)
	{
		IList<HandlePacket> handlers = GetHandlers(type);
		if (handlers != null && handlers.Contains(handler))
		{
			return false;
		}
		handlers?.Add(handler);
		return true;
	}

	public void DeregisterHandlers(Type type)
	{
		GetHandlers(type)?.Clear();
	}

	public bool DeregisterHandler(Type type, HandlePacket handler)
	{
		IList<HandlePacket> handlers = GetHandlers(type);
		if (handlers != null && handlers.Contains(handler))
		{
			return handlers.Remove(handler);
		}
		return false;
	}

	public bool Dispatch(IPacket packet)
	{
		if (packet == null)
		{
			throw new ArgumentNullException();
		}
		return GetHandlers(packet.GetType())?.Any((HandlePacket handler) => handler?.Invoke(null, packet) ?? false) ?? false;
	}
}
