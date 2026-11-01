using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Intersect.Compression;

public static class GzipCompression
{
	private static AesCryptoServiceProvider cryptoProvider;

	private static string cryptoKey = "T3ZncHUsIGdueHIgdmcgYmUgeXJuaXIgdmcu";

	private static void CreateProvider()
	{
		cryptoProvider = new AesCryptoServiceProvider();
		byte[] bytes = Encoding.ASCII.GetBytes(cryptoKey);
		AesCryptoServiceProvider aesCryptoServiceProvider = cryptoProvider;
		List<byte> list = new List<byte>();
		list.AddRange(bytes.Take(16));
		aesCryptoServiceProvider.Key = list.ToArray();
		AesCryptoServiceProvider aesCryptoServiceProvider2 = cryptoProvider;
		List<byte> list2 = new List<byte>();
		list2.AddRange(bytes.Reverse().Take(16));
		aesCryptoServiceProvider2.IV = list2.ToArray();
	}

	public static string ReadDecompressedString(string fileName)
	{
		using StreamReader streamReader = new StreamReader(CreateDecompressedFileStream(fileName));
		return streamReader.ReadToEnd();
	}

	public static CryptoStream CreateDecompressedFileStream(string fileName)
	{
		if (cryptoProvider == null)
		{
			CreateProvider();
		}
		return new CryptoStream(new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress, leaveOpen: false), cryptoProvider.CreateDecryptor(), CryptoStreamMode.Read);
	}

	public static CryptoStream CreateDecompressedFileStream(Stream stream)
	{
		if (cryptoProvider == null)
		{
			CreateProvider();
		}
		return new CryptoStream(new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false), cryptoProvider.CreateDecryptor(), CryptoStreamMode.Read);
	}

	public static void WriteCompressedString(string fileName, string data)
	{
		using CryptoStream cryptoStream = CreateCompressedFileStream(fileName);
		byte[] bytes = Encoding.UTF8.GetBytes(data);
		cryptoStream.Write(bytes, 0, bytes.Length);
	}

	public static CryptoStream CreateCompressedFileStream(string fileName)
	{
		if (cryptoProvider == null)
		{
			CreateProvider();
		}
		return new CryptoStream(new GZipStream(new FileStream(fileName, FileMode.Create), CompressionMode.Compress, leaveOpen: false), cryptoProvider.CreateEncryptor(), CryptoStreamMode.Write);
	}
}
