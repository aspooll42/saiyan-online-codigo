using System;
using System.Threading;
using Intersect.Core;
using Intersect.Memory;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

public class MessagePacker
{
	public static readonly MessagePacker Instance = new MessagePacker();

	private readonly MessagePackSerializerOptions mOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(NativeGuidResolver.Instance, NativeDateTimeResolver.Instance, NativeDecimalResolver.Instance, StandardResolver.Instance));

	private readonly MessagePackSerializerOptions mCompressedOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

	public byte[] Serialize(IntersectPacket packet)
	{
		PackedIntersectPacket value = new PackedIntersectPacket(packet)
		{
			Data = MessagePackSerializer.Serialize((object)packet, mOptions, default(CancellationToken))
		};
		return MessagePackSerializer.Serialize((object)value, mCompressedOptions, default(CancellationToken));
	}

	public object? Deserialize(byte[] packetData, bool silent = false)
	{
		try
		{
			PackedIntersectPacket packedIntersectPacket = MessagePackSerializer.Deserialize<PackedIntersectPacket>(packetData, mCompressedOptions);
			return MessagePackSerializer.Deserialize(packedIntersectPacket.PacketType, packedIntersectPacket.Data, mOptions);
		}
		catch (Exception exception)
		{
			if (!silent)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to deserialize packet data");
			}
			return null;
		}
	}

	public TPacket? Deserialize<TPacket>(byte[] packetData, bool silent = false) where TPacket : IntersectPacket
	{
		return Deserialize(packetData, silent) as TPacket;
	}

	public object Deserialize(IBuffer buffer)
	{
		return Deserialize(buffer.ReadBytes(buffer.Remaining));
	}
}
