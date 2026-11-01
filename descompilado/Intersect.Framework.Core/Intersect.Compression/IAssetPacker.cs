using System;
using System.Collections.Generic;
using System.IO;

namespace Intersect.Compression;

public interface IAssetPacker : IDisposable
{
	string PackageLocation { get; }

	List<string> CachedPackages { get; }

	List<string> FileList { get; }

	MemoryStream GetAsset(string fileName);

	bool Contains(string fileName);

	void UpdateCache();
}
