using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Intersect.Compression;

public sealed class AssetPacker : IAssetPacker, IDisposable
{
	private class PackageCacheEntry
	{
		public long LastAccessTime { get; set; }

		public MemoryStream StreamCache { get; set; }
	}

	private class PackageIndexEntry
	{
		public string FileName { get; set; }

		public string PackName { get; set; }

		public int FileStartByte { get; set; }

		public int FileEndByte { get; set; }
	}

	private class PackFileEntry
	{
		public string Name { get; set; }

		public string FullPath { get; set; }
	}

	private List<PackageIndexEntry> fileIndex;

	private string packageLocation;

	private Dictionary<string, PackageCacheEntry> packageCache = new Dictionary<string, PackageCacheEntry>();

	private long cacheTimeOutTimer = 1200000000L;

	public string PackageLocation => packageLocation;

	public List<string> CachedPackages => packageCache.Keys.ToList();

	public List<string> FileList => fileIndex.Select((PackageIndexEntry i) => i.FileName).ToList();

	public AssetPacker(string indexFile, string packLocation)
	{
		fileIndex = JsonConvert.DeserializeObject<List<PackageIndexEntry>>(GzipCompression.ReadDecompressedString(indexFile));
		packageLocation = packLocation;
		string[] array = fileIndex.Select((PackageIndexEntry i) => i.PackName).Distinct().ToArray();
		foreach (string key in array)
		{
			packageCache.Add(key, new PackageCacheEntry());
		}
	}

	public MemoryStream GetAsset(string fileName)
	{
		PackageIndexEntry packageIndexEntry = findAsset(fileName);
		if (packageIndexEntry == null)
		{
			return null;
		}
		packageCache[packageIndexEntry.PackName].LastAccessTime = DateTime.Now.Ticks;
		if (packageCache[packageIndexEntry.PackName].StreamCache == null)
		{
			using CryptoStream cryptoStream = GzipCompression.CreateDecompressedFileStream(Path.Combine(packageLocation, packageIndexEntry.PackName));
			packageCache[packageIndexEntry.PackName].StreamCache = new MemoryStream();
			cryptoStream.CopyTo(packageCache[packageIndexEntry.PackName].StreamCache);
		}
		byte[] array = new byte[packageIndexEntry.FileEndByte - packageIndexEntry.FileStartByte];
		packageCache[packageIndexEntry.PackName].StreamCache.Seek(packageIndexEntry.FileStartByte, SeekOrigin.Begin);
		packageCache[packageIndexEntry.PackName].StreamCache.Read(array, 0, array.Length);
		MemoryStream memoryStream = new MemoryStream();
		memoryStream.Write(array, 0, array.Length);
		memoryStream.Position = 0L;
		return memoryStream;
	}

	public bool Contains(string fileName)
	{
		return findAsset(fileName) != null;
	}

	public void UpdateCache()
	{
		foreach (KeyValuePair<string, PackageCacheEntry> item in packageCache)
		{
			if (item.Value.StreamCache != null && DateTime.Now.Ticks - item.Value.LastAccessTime > cacheTimeOutTimer)
			{
				item.Value.StreamCache.Dispose();
				item.Value.StreamCache = null;
			}
		}
	}

	private PackageIndexEntry findAsset(string fileName)
	{
		return fileIndex.Where((PackageIndexEntry d) => d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
	}

	public void Dispose()
	{
		foreach (KeyValuePair<string, PackageCacheEntry> item in packageCache)
		{
			if (item.Value.StreamCache != null)
			{
				item.Value.StreamCache.Dispose();
				item.Value.StreamCache = null;
			}
		}
	}

	public static void PackageAssets(string inputDir, string inputFilter, string outputDir, string indexName, string packPrefix, string packExt, int batchSize)
	{
		if (batchSize < 1)
		{
			batchSize = 1;
		}
		batchSize = batchSize * 1024 * 1024;
		Dictionary<string, List<PackFileEntry>> dictionary = GeneratePackLayout(inputDir, inputFilter, packPrefix, packExt, batchSize);
		List<PackageIndexEntry> list = new List<PackageIndexEntry>();
		foreach (KeyValuePair<string, List<PackFileEntry>> item in dictionary)
		{
			using CryptoStream destination = GzipCompression.CreateCompressedFileStream(Path.Combine(outputDir, item.Key));
			int num = 0;
			foreach (PackFileEntry item2 in item.Value)
			{
				using FileStream fileStream = File.OpenRead(item2.FullPath);
				fileStream.CopyTo(destination);
				list.Add(new PackageIndexEntry
				{
					FileName = Path.GetFileName(item2.Name),
					PackName = item.Key,
					FileStartByte = num,
					FileEndByte = num + (int)fileStream.Length
				});
				num += (int)fileStream.Length;
			}
		}
		GzipCompression.WriteCompressedString(Path.Combine(outputDir, indexName), JsonConvert.SerializeObject(list));
	}

	private static Dictionary<string, List<PackFileEntry>> GeneratePackLayout(string inputDir, string inputFilter, string packPrefix, string packExt, long batchSize)
	{
		Dictionary<string, List<PackFileEntry>> dictionary = new Dictionary<string, List<PackFileEntry>>();
		int num = 0;
		long num2 = 0L;
		foreach (FileInfo item in from f in new DirectoryInfo(inputDir).GetFiles(inputFilter)
			orderby f.Length descending
			select f)
		{
			if (num2 + item.Length > batchSize && num2 != 0L)
			{
				num2 = 0L;
				num++;
			}
			string key = $"{packPrefix}{num}{packExt}";
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, new List<PackFileEntry>());
			}
			dictionary[key].Add(new PackFileEntry
			{
				Name = item.Name,
				FullPath = item.FullName
			});
			num2 += item.Length;
		}
		return dictionary;
	}
}
