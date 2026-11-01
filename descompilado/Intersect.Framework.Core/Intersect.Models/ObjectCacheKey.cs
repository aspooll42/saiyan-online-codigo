using System;
using System.IO.Hashing;
using Intersect.Framework;
using MessagePack;

namespace Intersect.Models;

[MessagePackObject(true)]
public record struct ObjectCacheKey<TObject>([property: Key(0)] Id<TObject> Id, [property: Key(1)] string? Checksum = null, [property: Key(2)] string? Version = null)
{
	[SerializationConstructor]
	public ObjectCacheKey()
		: this(default(Id<TObject>))
	{
	}

	public static string? ComputeChecksum(ReadOnlySpan<byte> data)
	{
		if (data.Length < 1)
		{
			return null;
		}
		byte[] array = new byte[8];
		if (!Crc64.TryHash(data, array, out var bytesWritten) || bytesWritten != 8)
		{
			return null;
		}
		return Convert.ToBase64String(array);
	}
}
