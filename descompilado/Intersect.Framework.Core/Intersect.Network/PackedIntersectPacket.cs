using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Intersect.Network;

[MessagePackObject(false)]
public class PackedIntersectPacket
{
	private static readonly string[] BuiltInPacketNamespaces = new string[5] { "Intersect.Network.Packets", "Intersect.Network.Packets.Client", "Intersect.Network.Packets.Editor", "Intersect.Network.Packets.Server", "Intersect.Admin.Actions" };

	public static readonly Dictionary<Type, ushort> KnownTypes = new Dictionary<Type, ushort>();

	public static readonly Dictionary<ushort, Type> KnownKeys = new Dictionary<ushort, Type>();

	[Key(0)]
	public ushort Key { get; set; } = ushort.MaxValue;

	[Key(1)]
	public byte[] Data { get; set; }

	[IgnoreMember]
	public Type PacketType => KnownKeys[Key];

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

	public static void AddKnownTypes(IReadOnlyList<Type> types)
	{
		ushort num = (ushort)KnownKeys.Count;
		foreach (Type type in types)
		{
			if (!KnownTypes.ContainsKey(type))
			{
				KnownKeys.Add(num, type);
				KnownTypes.Add(type, num);
				num++;
			}
		}
	}

	public PackedIntersectPacket()
	{
	}

	public PackedIntersectPacket(IntersectPacket packet)
	{
		if (!KnownTypes.TryGetValue(packet.GetType(), out var value))
		{
			throw new ArgumentException("Type not a known packet type: " + packet.GetType().FullName);
		}
		Key = value;
	}
}
