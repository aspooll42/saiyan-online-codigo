using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Intersect.Collections;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public class PacketHandlerRegistry : IDisposable
{
	private struct DisposableHandler : IDisposable
	{
		public Type PacketType { get; }

		public IDisposable Reference { get; }

		public DisposableHandler(Type packetType, IDisposable reference)
		{
			PacketType = packetType ?? throw new ArgumentNullException("packetType");
			Reference = reference ?? throw new ArgumentNullException("reference");
		}

		public void Dispose()
		{
			Reference.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	private readonly MethodInfo _createHandlerDelegateForTypeInfo;

	private static Type HandlePacketDelegateType { get; } = typeof(HandlePacket<, >);

	private static Type HandlePacketVoidDelegateType { get; } = typeof(HandlePacketVoid<, >);

	private MethodInfo CreateWeaklyTypedDelegateForMethodInfoInfo { get; }

	private Dictionary<Type, HandlePacketGeneric> Handlers { get; }

	private Dictionary<Type, List<IPacketHandler>> Preprocessors { get; }

	private Dictionary<Type, List<IPacketHandler>> PreHooks { get; }

	private Dictionary<Type, List<IPacketHandler>> PostHooks { get; }

	private Stack<DisposableHandler> DisposableHandlers { get; }

	protected PacketTypeRegistry PacketTypeRegistry { get; }

	protected ILogger Logger { get; }

	public int Count => Handlers.Count;

	public bool IsEmpty => Count < 1;

	public PacketHandlerRegistry(PacketTypeRegistry packetTypeRegistry, ILogger logger)
	{
		PacketTypeRegistry = packetTypeRegistry ?? throw new ArgumentNullException("packetTypeRegistry");
		Logger = logger ?? throw new ArgumentNullException("logger");
		CreateWeaklyTypedDelegateForMethodInfoInfo = GetType().GetMethod("CreateWeaklyTypedDelegateForMethodInfo", BindingFlags.Instance | BindingFlags.NonPublic);
		if (null == CreateWeaklyTypedDelegateForMethodInfoInfo)
		{
			throw new NullReferenceException("Failed to reflect CreateWeaklyTypedDelegateForMethodInfo.");
		}
		_createHandlerDelegateForTypeInfo = GetType().GetMethod("CreateHandlerDelegateForType", BindingFlags.Instance | BindingFlags.NonPublic);
		if (null == _createHandlerDelegateForTypeInfo)
		{
			throw new NullReferenceException("Failed to reflect CreateHandlerDelegateForType.");
		}
		Handlers = new Dictionary<Type, HandlePacketGeneric>();
		DisposableHandlers = new Stack<DisposableHandler>();
		Preprocessors = new Dictionary<Type, List<IPacketHandler>>();
		PreHooks = new Dictionary<Type, List<IPacketHandler>>();
		PostHooks = new Dictionary<Type, List<IPacketHandler>>();
		TryRegisterAvailableHandlers(typeof(PacketHandlerRegistry).Assembly);
	}

	protected virtual HandlePacketGeneric CreateWeaklyTypedDelegateForMethodInfo<TPacketSender, TPacket>(MethodInfo methodInfo, object target = null) where TPacketSender : IPacketSender where TPacket : IPacket
	{
		if (methodInfo == null)
		{
			throw new ArgumentNullException("methodInfo");
		}
		if (typeof(bool) == methodInfo.ReturnType)
		{
			HandlePacket<TPacketSender, TPacket> stronglyTyped = Delegate.CreateDelegate(typeof(HandlePacket<TPacketSender, TPacket>), target, methodInfo) as HandlePacket<TPacketSender, TPacket>;
			return (IPacketSender packetSender, IPacket packet) => stronglyTyped((TPacketSender)packetSender, (TPacket)packet);
		}
		if (typeof(void) == methodInfo.ReturnType)
		{
			HandlePacketVoid<TPacketSender, TPacket> stronglyTyped2 = Delegate.CreateDelegate(typeof(HandlePacketVoid<TPacketSender, TPacket>), target, methodInfo) as HandlePacketVoid<TPacketSender, TPacket>;
			return delegate(IPacketSender packetSender, IPacket packet)
			{
				stronglyTyped2((TPacketSender)packetSender, (TPacket)packet);
				return true;
			};
		}
		throw new ArgumentException("Unsupported packet handler return type '" + methodInfo.ReturnType.FullName + "'.");
	}

	protected virtual HandlePacketGeneric CreateHandlerDelegateForInstance<TPacket>(IPacketHandler packetHandlerGeneric) where TPacket : class, IPacket
	{
		IPacketHandler<TPacket> stronglyTypedPacketHandler = packetHandlerGeneric as IPacketHandler<TPacket>;
		return (IPacketSender packetSender, IPacket packet) => stronglyTypedPacketHandler.Handle(packetSender, (TPacket)packet);
	}

	protected virtual HandlePacketGeneric CreateHandlerDelegateForType<TPacket>(Type handlerType) where TPacket : class, IPacket
	{
		IPacketHandler packetHandler = Activator.CreateInstance(handlerType) as IPacketHandler;
		if (packetHandler is IDisposable reference)
		{
			DisposableHandlers.Push(new DisposableHandler(typeof(TPacket), reference));
		}
		return CreateHandlerDelegateForInstance<TPacket>(packetHandler);
	}

	protected virtual HandlePacketGeneric CreateInstance(Type packetSenderType, Type packetType, MethodInfo methodInfo, object target = null)
	{
		MethodInfo methodInfo2 = CreateWeaklyTypedDelegateForMethodInfoInfo.MakeGenericMethod(packetSenderType, packetType);
		return methodInfo2.Invoke(this, new object[2] { methodInfo, target }) as HandlePacketGeneric;
	}

	protected virtual HandlePacketGeneric CreateInstance(Type packetType, Type type)
	{
		MethodInfo methodInfo = _createHandlerDelegateForTypeInfo.MakeGenericMethod(packetType);
		return methodInfo.Invoke(this, new object[1] { type }) as HandlePacketGeneric;
	}

	public bool HasHandler<TPacket>() where TPacket : IPacket
	{
		HandlePacketGeneric handlerInstance;
		return TryGetHandler(typeof(TPacket), out handlerInstance);
	}

	public bool HasHandler(Type packetType)
	{
		HandlePacketGeneric handlerInstance;
		return TryGetHandler(packetType, out handlerInstance);
	}

	public bool TryGetHandler(Type packetType, out HandlePacketGeneric handlerInstance)
	{
		return Handlers.TryGetValue(packetType ?? throw new ArgumentNullException("packetType"), out handlerInstance);
	}

	public bool TryGetHandler<TPacket>(out HandlePacketGeneric handlerInstance)
	{
		return TryGetHandler(typeof(TPacket), out handlerInstance);
	}

	public bool TryGetHandler(IPacket packet, out HandlePacketGeneric handlerInstance)
	{
		return TryGetHandler(packet?.GetType(), out handlerInstance);
	}

	public bool TryGetPreprocessors(Type packetType, out IReadOnlyList<IPacketHandler> preprocessors)
	{
		if (!Preprocessors.TryGetValue(packetType, out var value))
		{
			preprocessors = null;
			return false;
		}
		preprocessors = value?.WrapReadOnly();
		return preprocessors != null;
	}

	public bool TryGetPreprocessors<TPacket>(out IReadOnlyList<IPacketHandler> preprocessors) where TPacket : IPacket
	{
		return TryGetPreprocessors(typeof(TPacket), out preprocessors);
	}

	public bool TryGetPreprocessors(IPacket packet, out IReadOnlyList<IPacketHandler> preprocessors)
	{
		return TryGetPreprocessors(packet?.GetType(), out preprocessors);
	}

	public bool TryGetPreHooks(Type packetType, out IReadOnlyList<IPacketHandler> preHooks)
	{
		if (!PreHooks.TryGetValue(packetType, out var value))
		{
			preHooks = null;
			return false;
		}
		preHooks = value?.WrapReadOnly();
		return preHooks != null;
	}

	public bool TryGetPreHooks<TPacket>(out IReadOnlyList<IPacketHandler> preHooks) where TPacket : IPacket
	{
		return TryGetPreHooks(typeof(TPacket), out preHooks);
	}

	public bool TryGetPreHooks(IPacket packet, out IReadOnlyList<IPacketHandler> preHooks)
	{
		return TryGetPreHooks(packet?.GetType(), out preHooks);
	}

	public bool TryGetPostHooks(Type packetType, out IReadOnlyList<IPacketHandler> postHooks)
	{
		if (!PostHooks.TryGetValue(packetType, out var value))
		{
			postHooks = null;
			return false;
		}
		postHooks = value?.WrapReadOnly();
		return postHooks != null;
	}

	public bool TryGetPostHooks<TPacket>(out IReadOnlyList<IPacketHandler> postHooks) where TPacket : IPacket
	{
		return TryGetPostHooks(typeof(TPacket), out postHooks);
	}

	public bool TryGetPostHooks(IPacket packet, out IReadOnlyList<IPacketHandler> postHooks)
	{
		return TryGetPostHooks(packet?.GetType(), out postHooks);
	}

	protected static Type GetPacketSenderType(MethodInfo methodInfo)
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
		if (!PacketHandlerAttribute.TypeIPacketSender.IsAssignableFrom(type))
		{
			throw new ArgumentException($"Invalid sender parameter type ({type.FullName}) in handler '{methodInfo.GetFullSignature()}'.", "methodInfo");
		}
		return type;
	}

	protected virtual bool TryRegister(IEnumerable<MethodInfo> methodInfos, object target = null)
	{
		if (methodInfos == null)
		{
			throw new ArgumentNullException("methodInfos");
		}
		foreach (MethodInfo methodInfo in methodInfos)
		{
			try
			{
				Type packetType = PacketHandlerAttribute.GetPacketType(methodInfo);
				if (!PacketTypeRegistry.IsRegistered(packetType))
				{
					Logger.LogError("The packet type '" + packetType.FullName + "' has not been registered, a handler cannot be added.");
					return false;
				}
				if (HasHandler(packetType))
				{
					Logger.LogError("There is already a packet handler for '" + packetType.FullName + "'.");
					return false;
				}
				HandlePacketGeneric value = CreateInstance(GetPacketSenderType(methodInfo), packetType, methodInfo, target);
				Handlers.Add(packetType, value);
			}
			catch (Exception exception)
			{
				Logger.LogError(exception, "Error registering handler for {Method} on {Type}", methodInfo.Name, methodInfo.DeclaringType?.GetName(qualified: true));
				return false;
			}
		}
		return true;
	}

	protected virtual bool TryRegister(IEnumerable<Type> handlerTypes)
	{
		if (handlerTypes == null)
		{
			throw new ArgumentNullException("handlerTypes");
		}
		foreach (Type handlerType in handlerTypes)
		{
			Type[] packetTypes = PacketHandlerAttribute.GetPacketTypes(handlerType);
			Type[] array = packetTypes;
			foreach (Type type in array)
			{
				if (!PacketTypeRegistry.IsRegistered(type))
				{
					Logger.LogError("The packet type {PacketType} has not been registered, a the handler type {HandlerType} cannot be added.", type.GetName(qualified: true), handlerType.GetName(qualified: true));
					return false;
				}
				if (TryGetHandler(type, out var handlerInstance))
				{
					Logger.LogError("There is already a packet handler ({ExistingHandlerType}) for '{PacketType}'", handlerInstance.GetType().GetName(qualified: true), type.GetName(qualified: true));
					return false;
				}
				HandlePacketGeneric value = CreateInstance(type, handlerType);
				Handlers.Add(type, value);
			}
		}
		return true;
	}

	protected virtual bool TryRegister(Dictionary<Type, List<IPacketHandler>> collection, Type handlerType, Type packetType, out IPacketHandler handler)
	{
		ArgumentNullException.ThrowIfNull(collection, "collection");
		ArgumentNullException.ThrowIfNull(handlerType, "handlerType");
		ArgumentNullException.ThrowIfNull(packetType, "packetType");
		handler = Activator.CreateInstance(handlerType) as IPacketHandler;
		return TryRegister(collection, handlerType, packetType, handler);
	}

	protected virtual bool TryRegister(Dictionary<Type, List<IPacketHandler>> collection, Type handlerType, Type packetType, IPacketHandler handler)
	{
		ArgumentNullException.ThrowIfNull(collection, "collection");
		ArgumentNullException.ThrowIfNull(handlerType, "handlerType");
		ArgumentNullException.ThrowIfNull(packetType, "packetType");
		ArgumentNullException.ThrowIfNull(handler, "handler");
		Type[] packetTypes = PacketHandlerAttribute.GetPacketTypes(handlerType);
		if (!packetTypes.Contains(packetType))
		{
			Logger.LogError("Handler {HandlerTypeName} is incompatible with {ActualPacketTypeName}, expected one of {ExpectedPacketTypeNames}.", handlerType.GetName(qualified: true), packetType.GetName(qualified: true), string.Join(", ", packetTypes.Select((Type type) => type.GetName(qualified: true))));
			return false;
		}
		if (!PacketTypeRegistry.IsRegistered(packetType))
		{
			Logger.LogError("The packet type '{PacketTypeName}' has not been registered, a handler cannot be added.", packetType.GetName(qualified: true));
			return false;
		}
		if (!collection.TryGetValue(packetType, out var value))
		{
			value = new List<IPacketHandler>();
			collection.Add(packetType, value);
		}
		value.Add(handler);
		return true;
	}

	protected virtual bool TryRegister<THandler, TPacket>(Dictionary<Type, List<IPacketHandler>> collection, out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		if (TryRegister(collection, typeof(THandler), typeof(TPacket), out var handler2))
		{
			handler = (THandler)handler2;
			return true;
		}
		handler = default(THandler);
		return false;
	}

	protected virtual bool TryRegister<THandler, TPacket>(Dictionary<Type, List<IPacketHandler>> collection, THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister(collection, typeof(THandler), typeof(TPacket), handler);
	}

	public virtual bool TryRegisterPreprocessor(Type handlerType, Type packetType, out IPacketHandler handler)
	{
		return TryRegister(Preprocessors, handlerType, packetType, out handler);
	}

	public bool TryRegisterPreprocessor<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister<THandler, TPacket>(Preprocessors, out handler);
	}

	public virtual bool TryRegisterPreHook(Type handlerType, Type packetType, out IPacketHandler handler)
	{
		return TryRegister(PreHooks, handlerType, packetType, out handler);
	}

	public bool TryRegisterPreHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister<THandler, TPacket>(PreHooks, out handler);
	}

	public bool TryRegisterPreHook<THandler, TPacket>(THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister<THandler, TPacket>(PreHooks, handler);
	}

	public virtual bool TryRegisterPostHook(Type handlerType, Type packetType, out IPacketHandler handler)
	{
		return TryRegister(PostHooks, handlerType, packetType, out handler);
	}

	public bool TryRegisterPostHook<THandler, TPacket>(out THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister<THandler, TPacket>(PostHooks, out handler);
	}

	public bool TryRegisterPostHook<THandler, TPacket>(THandler handler) where THandler : IPacketHandler<TPacket> where TPacket : class, IPacket
	{
		return TryRegister<THandler, TPacket>(PostHooks, handler);
	}

	public virtual bool TryRegisterHandler(Type handlerType, out IPacketHandler handler)
	{
		handler = null;
		Type[] packetTypes = PacketHandlerAttribute.GetPacketTypes(handlerType);
		Type[] array = packetTypes;
		foreach (Type type in array)
		{
			if (!PacketTypeRegistry.IsRegistered(type))
			{
				Logger.LogError("The packet type '{PacketType}' has not been registered, a {Ha cannot be added.");
				return false;
			}
			if (HasHandler(type))
			{
				Logger.LogError("There is already a packet handler for '{packetType.FullName}'.");
				return false;
			}
			HandlePacketGeneric value = CreateInstance(type, handlerType);
			Handlers.Add(type, value);
		}
		return true;
	}

	public bool TryRegisterHandler<THandler>(out THandler handler) where THandler : IPacketHandler
	{
		handler = default(THandler);
		if (TryRegisterHandler(typeof(THandler), out var handler2))
		{
			handler = (THandler)handler2;
			return true;
		}
		return false;
	}

	private bool TryRegisterAvailableHandlers(Assembly assembly, bool requireAttribute = true)
	{
		if (TryRegisterAvailableMethodHandlers(assembly, requireAttribute))
		{
			return TryRegisterAvailableTypeHandlers(assembly, requireAttribute);
		}
		return false;
	}

	private bool TryRegisterAvailableMethodHandlers(Assembly assembly, bool requireAttribute = true)
	{
		return TryRegister(DiscoverMethods(assembly, requireAttribute));
	}

	public bool TryRegisterAvailableMethodHandlers(Type type, object target = null, bool requireAttribute = true)
	{
		return TryRegister(DiscoverMethods(type, (target == null) ? BindingFlags.Static : BindingFlags.Instance, requireAttribute), target);
	}

	public bool TryRegisterAvailableTypeHandlers(Assembly assembly, bool requireAttribute = true)
	{
		return TryRegister(DiscoverTypes(assembly, requireAttribute));
	}

	public static IEnumerable<MethodInfo> DiscoverMethods(Assembly assembly, bool requireAttribute = true)
	{
		if (null == assembly)
		{
			throw new ArgumentNullException("assembly");
		}
		Type[] types = assembly.GetTypes();
		return types.SelectMany((Type assemblyType) => DiscoverMethods(assemblyType, BindingFlags.Static, requireAttribute));
	}

	public static IEnumerable<MethodInfo> DiscoverMethods(Type type, BindingFlags extraBindingFlags, bool requireAttribute = true)
	{
		if (null == type)
		{
			throw new ArgumentNullException("type");
		}
		return from methodInfo in type.GetMethods(BindingFlags.Public | extraBindingFlags)
			where (!requireAttribute || Attribute.IsDefined(methodInfo, typeof(PacketHandlerAttribute))) && PacketHandlerAttribute.IsValidHandler(methodInfo)
			select methodInfo;
	}

	public static IEnumerable<Type> DiscoverTypes(Assembly assembly, bool requireAttribute = true)
	{
		if (null == assembly)
		{
			throw new ArgumentNullException("assembly");
		}
		Type[] types = assembly.GetTypes();
		return types.Where((Type type) => (!requireAttribute || Attribute.IsDefined(type, typeof(PacketHandlerAttribute))) && PacketHandlerAttribute.IsValidHandler(type));
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			while (DisposableHandlers.Count > 0)
			{
				DisposableHandler disposableHandler = DisposableHandlers.Pop();
				Handlers.Remove(disposableHandler.PacketType);
				disposableHandler.Dispose();
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
