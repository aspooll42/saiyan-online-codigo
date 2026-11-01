using System;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.AssetManagement;

public sealed record UpdateManifestFile(string Path, string Checksum, long Size) : IComparable<UpdateManifestFile>
{
	[Newtonsoft.Json.JsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public int Segments => Path.Count((char c) => c == '/');

	public int CompareTo(UpdateManifestFile other)
	{
		int num = Segments - other.Segments;
		if (num == 0)
		{
			return string.CompareOrdinal(Path, other.Path);
		}
		return num;
	}

	public static UpdateManifestFile From(FileInfo fileInfo, string rootPath, FileInfo? mappedTo = null)
	{
		string checksum = ComputeChecksum(fileInfo);
		string relativePath = System.IO.Path.GetRelativePath(rootPath, mappedTo?.FullName ?? fileInfo.FullName);
		return new UpdateManifestFile(relativePath, checksum, fileInfo.Length);
	}

	public static string ComputeChecksum(FileInfo fileInfo, NonCryptographicHashAlgorithm algorithm = null)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException("fileInfo");
		}
		if (algorithm == null)
		{
			algorithm = new Crc64();
		}
		using FileStream stream = fileInfo.OpenRead();
		algorithm.Append(stream);
		byte[] hashAndReset = algorithm.GetHashAndReset();
		return Convert.ToBase64String(hashAndReset);
	}
}
