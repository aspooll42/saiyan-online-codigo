using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Hashing;

namespace Intersect.Framework.IO;

public static class FileInfoExtensions
{
	public static bool TryComputeChecksum(this FileInfo fileInfo, [NotNullWhen(true)] out string? checksum)
	{
		if (!fileInfo.Exists)
		{
			checksum = null;
			return false;
		}
		Crc64 crc = new Crc64();
		using FileStream stream = fileInfo.OpenRead();
		crc.Append(stream);
		byte[] hashAndReset = crc.GetHashAndReset();
		if (hashAndReset.Length < 1)
		{
			checksum = null;
			return false;
		}
		checksum = Convert.ToBase64String(hashAndReset);
		if (!string.IsNullOrWhiteSpace(checksum))
		{
			return true;
		}
		checksum = null;
		return false;
	}
}
