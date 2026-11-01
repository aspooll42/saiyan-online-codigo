using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Intersect.Framework;

namespace Intersect.Models;

public sealed class ObjectCacheData<TObject>
{
	private string? _checksum;

	public Id<TObject> Id { get; init; }

	public string Checksum
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_checksum))
			{
				return _checksum;
			}
			_checksum = ObjectCacheKey<TObject>.ComputeChecksum(Data);
			return _checksum;
		}
	}

	public byte[] Data { get; init; }

	public string Version { get; init; }

	public ObjectCacheKey<TObject> GetKey()
	{
		return new ObjectCacheKey<TObject>(Id, Checksum, Version);
	}

	public static string? GetCacheFileName(Id<TObject> id)
	{
		byte[] array = new byte[32];
		byte[] array2 = id.Guid.ToByteArray().Concat(Encoding.UTF8.GetBytes(typeof(TObject).FullName ?? typeof(TObject).Name)).ToArray();
		if (!SHA256.TryHashData(array2, array, out var bytesWritten))
		{
			return null;
		}
		if (bytesWritten != array.Length)
		{
			return null;
		}
		string value = Convert.ToHexString(array);
		return $"{id.Guid}-{value}";
	}
}
