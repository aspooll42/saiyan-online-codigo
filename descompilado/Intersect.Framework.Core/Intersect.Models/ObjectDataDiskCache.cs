using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Framework;
using Microsoft.Extensions.Logging;

namespace Intersect.Models;

public static class ObjectDataDiskCache<TObject>
{
	private static readonly ConcurrentDictionary<string, byte[]> Clefs = new ConcurrentDictionary<string, byte[]>();

	private static readonly object VerrouEcriture = new object();

	private static Task _ecritures = Task.CompletedTask;

	private static string RootCacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".intersect", (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name ?? throw new InvalidOperationException(), ConfigurationHelper.CacheName);

	private static SymmetricAlgorithm CreateCryptoServiceProvider(string identifier, out string nonce)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(16);
		nonce = Convert.ToBase64String(bytes);
		return CreateCryptoServiceProvider(identifier, nonce);
	}

	private static SymmetricAlgorithm CreateCryptoServiceProvider(string identifier, string nonce)
	{
		byte[] orAdd = Clefs.GetOrAdd(identifier, delegate(string cle)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(ConfigurationHelper.CacheName);
			byte[] bytes2 = Encoding.UTF8.GetBytes(cle);
			return Rfc2898DeriveBytes.Pbkdf2(bytes, bytes2, 1000, HashAlgorithmName.SHA256, 16);
		});
		byte[] iV = Convert.FromBase64String(nonce);
		Aes aes = Aes.Create();
		aes.Key = orAdd;
		aes.IV = iV;
		return aes;
	}

	public static bool TryLoad(Guid id, [NotNullWhen(true)] out ObjectCacheData<TObject>? data)
	{
		try
		{
			string rootCacheDirectory = RootCacheDirectory;
			if (!Directory.Exists(rootCacheDirectory))
			{
				data = null;
				return false;
			}
			Id<TObject> id2 = new Id<TObject>(id);
			string cacheFileName = ObjectCacheData<TObject>.GetCacheFileName(id2);
			if (string.IsNullOrWhiteSpace(cacheFileName))
			{
				data = null;
				return false;
			}
			string path = Path.Combine(rootCacheDirectory, cacheFileName);
			if (!File.Exists(path))
			{
				data = null;
				return false;
			}
			using FileStream fileStream = File.OpenRead(path);
			int num = fileStream.ReadByte();
			Span<byte> span = stackalloc byte[num];
			int num2 = fileStream.Read(span);
			if (num2 != num)
			{
				data = null;
				return false;
			}
			string nonce = Encoding.UTF8.GetString(span);
			using SymmetricAlgorithm symmetricAlgorithm = CreateCryptoServiceProvider(cacheFileName, nonce);
			using CryptoStream stream = new CryptoStream(fileStream, symmetricAlgorithm.CreateDecryptor(), CryptoStreamMode.Read);
			using GZipStream input = new GZipStream(stream, CompressionMode.Decompress);
			using BinaryReader binaryReader = new BinaryReader(input);
			Guid guid = new Guid(binaryReader.ReadBytes(16));
			if (guid != id)
			{
				data = null;
				return false;
			}
			int num3 = binaryReader.ReadInt32();
			string version = ((num3 > 0) ? Encoding.UTF8.GetString(binaryReader.ReadBytes(num3)) : null);
			int num4 = binaryReader.ReadInt32();
			if (num4 < 0)
			{
				data = null;
				return false;
			}
			byte[] data2 = binaryReader.ReadBytes(num4);
			data = new ObjectCacheData<TObject>
			{
				Id = id2,
				Data = data2,
				Version = version
			};
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to load cache item {ID}", id);
			data = null;
			return false;
		}
	}

	public static bool TrySave(ObjectCacheData<TObject> objectCacheData)
	{
		if (objectCacheData.Data == null)
		{
			return false;
		}
		lock (VerrouEcriture)
		{
			_ecritures = _ecritures.ContinueWith((Task _) => Save(objectCacheData), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
		}
		return true;
	}

	private static bool Save(ObjectCacheData<TObject> objectCacheData)
	{
		try
		{
			byte[] data = objectCacheData.Data;
			if (data == null)
			{
				return false;
			}
			string rootCacheDirectory = RootCacheDirectory;
			if (!Directory.Exists(rootCacheDirectory))
			{
				Directory.CreateDirectory(rootCacheDirectory);
			}
			string cacheFileName = ObjectCacheData<TObject>.GetCacheFileName(objectCacheData.Id);
			if (string.IsNullOrWhiteSpace(cacheFileName))
			{
				return false;
			}
			string path = Path.Combine(rootCacheDirectory, cacheFileName);
			string nonce;
			using SymmetricAlgorithm symmetricAlgorithm = CreateCryptoServiceProvider(cacheFileName, out nonce);
			using FileStream fileStream = File.OpenWrite(path);
			byte[] bytes = Encoding.UTF8.GetBytes(nonce);
			fileStream.WriteByte((byte)bytes.Length);
			fileStream.Write(bytes);
			using CryptoStream stream = new CryptoStream(fileStream, symmetricAlgorithm.CreateEncryptor(), CryptoStreamMode.Write);
			using GZipStream output = new GZipStream(stream, CompressionLevel.Optimal);
			using BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(objectCacheData.Id.Guid.ToByteArray());
			string version = objectCacheData.Version;
			if (string.IsNullOrWhiteSpace(version))
			{
				binaryWriter.Write(0);
			}
			else
			{
				binaryWriter.Write(version.Length);
				binaryWriter.Write(Encoding.UTF8.GetBytes(version));
			}
			binaryWriter.Write(data.Length);
			binaryWriter.Write(data);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to save cache item {CacheKey}", objectCacheData.GetKey());
			return false;
		}
	}
}
