using System;
using System.Collections.Generic;
using System.Linq;
using Ceras;
using Intersect.Core;
using Intersect.Framework.Reflection;
using K4os.Compression.LZ4;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public class Ceras
{
	private static readonly string[] BuiltInPacketNamespaces = new string[5] { "Intersect.Network.Packets", "Intersect.Network.Packets.Client", "Intersect.Network.Packets.Editor", "Intersect.Network.Packets.Server", "Intersect.Admin.Actions" };

	protected CerasSerializer mSerializer;

	protected SerializerConfig mSerializerConfig;

	public static List<Type> KnownTypes { get; } = FindTypes(BuiltInPacketNamespaces).ToList();

	public Ceras(bool forNetworking = true)
	{
		if (CerasBufferPool.Pool == null)
		{
			CerasBufferPool.Pool = new CerasDefaultBufferPool();
		}
		mSerializerConfig = new SerializerConfig
		{
			PreserveReferences = false
		};
		mSerializerConfig.Advanced.SealTypesWhenUsingKnownTypes = forNetworking;
		if (forNetworking)
		{
			mSerializerConfig.VersionTolerance.Mode = VersionToleranceMode.Disabled;
			mSerializerConfig.KnownTypes.AddRange(KnownTypes);
			mSerializerConfig.KnownTypes.ForEach(delegate(Type knownType)
			{
				mSerializerConfig.ConfigType(knownType).TypeConstruction = TypeConstruction.ByUninitialized();
			});
		}
		else
		{
			mSerializerConfig.VersionTolerance.Mode = VersionToleranceMode.Standard;
		}
		mSerializer = new CerasSerializer(mSerializerConfig);
	}

	public Ceras(Dictionary<string, Type> nameTypeMap)
	{
		mSerializerConfig = new SerializerConfig
		{
			PreserveReferences = false
		};
		mSerializerConfig.Advanced.SealTypesWhenUsingKnownTypes = false;
		mSerializerConfig.VersionTolerance.Mode = VersionToleranceMode.Standard;
		mSerializerConfig.Advanced.TypeBinder = new CerasTypeBinder(nameTypeMap);
		mSerializer = new CerasSerializer(mSerializerConfig);
	}

	public static void AddKnownTypes(IReadOnlyList<Type> types)
	{
		KnownTypes.AddRange(types.Where((Type type) => !KnownTypes.Contains(type)));
	}

	private static IEnumerable<Type> FindTypes(IEnumerable<string> nameSpaces)
	{
		return nameSpaces.SelectMany(FindTypes);
	}

	private static IEnumerable<Type> FindTypes(string nameSpace)
	{
		return from type in typeof(Ceras).Assembly.GetTypes()
			where type.Namespace == nameSpace
			select type;
	}

	public byte[] Serialize(object obj)
	{
		lock (mSerializer)
		{
			return mSerializer.Serialize(obj);
		}
	}

	public object Deserialize(byte[] data)
	{
		try
		{
			lock (mSerializer)
			{
				return mSerializer.Deserialize<object>(data);
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to deserialize Ceras packet");
			return null;
		}
	}

	public T? Deserialize<T>(byte[] data)
	{
		try
		{
			lock (mSerializer)
			{
				return mSerializer.Deserialize<T>(data);
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to deserialize Ceras packet to a {Type}", typeof(T).GetName(qualified: true));
			return default(T);
		}
	}

	public byte[] Compress(object obj)
	{
		return LZ4Pickler.Pickle(Serialize(obj));
	}

	public object Decompress(byte[] data)
	{
		return Deserialize(LZ4Pickler.Unpickle(data));
	}

	public T? Decompress<T>(byte[] data)
	{
		return Deserialize<T>(LZ4Pickler.Unpickle(data));
	}
}
