using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.LiteNetLib;

public sealed class AesAlgorithm : SymmetricAlgorithm
{
	private const int HeaderSize = 6;

	private static KeySizes[] LegalBlockSizes;

	private static KeySizes[] LegalKeySizes;

	private readonly Memory<byte> _key;

	private MemoryHandle _keyHandle;

	static AesAlgorithm()
	{
		using Aes aes = Aes.Create();
		LegalBlockSizes = aes.LegalBlockSizes;
		LegalKeySizes = aes.LegalKeySizes;
	}

	private new static bool IsValidSize(KeySizes sizes, int size)
	{
		return SymmetricAlgorithm.IsValidSize(new KeySizes(sizes.MinSize / 8, sizes.MaxSize / 8, sizes.SkipSize / 8), size);
	}

	public AesAlgorithm(ReadOnlySpan<byte> key = default(ReadOnlySpan<byte>))
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(32);
		if (key != default(ReadOnlySpan<byte>))
		{
			int keyLength = key.Length;
			if (!LegalKeySizes.Any((KeySizes keySizes) => IsValidSize(keySizes, keyLength)))
			{
				throw new ArgumentException($"{key.Length} is not a supported key length in bytes.", "key");
			}
			if (!key.TryCopyTo(bytes))
			{
				throw new ArgumentException("The provided key could not be copied to the internal buffer.", "key");
			}
		}
		_key = new Memory<byte>(bytes);
		_keyHandle = _key.Pin();
	}

	public override void Dispose()
	{
		_keyHandle.Dispose();
	}

	public override bool SetKey(ReadOnlySpan<byte> key)
	{
		return key.TryCopyTo(_key.Span);
	}

	public override EncryptionResult TryDecrypt(ReadOnlySpan<byte> cipherdata, out ReadOnlySpan<byte> plaindata)
	{
		plaindata = default(ReadOnlySpan<byte>);
		try
		{
			byte b = cipherdata[0];
			ref ReadOnlySpan<byte> reference = ref cipherdata;
			cipherdata = reference.Slice(1, reference.Length - 1);
			if (b != 0)
			{
				return EncryptionResult.InvalidVersion;
			}
			using Aes aes = Aes.Create();
			aes.Key = _key.ToArray();
			int num = BinaryPrimitives.ReadInt32LittleEndian(cipherdata);
			reference = ref cipherdata;
			cipherdata = reference.Slice(4, reference.Length - 4);
			byte b2 = cipherdata[0];
			reference = ref cipherdata;
			cipherdata = reference.Slice(1, reference.Length - 1);
			ReadOnlySpan<byte> readOnlySpan = cipherdata.Slice(0, b2);
			reference = ref cipherdata;
			int num2 = b2;
			cipherdata = reference.Slice(num2, reference.Length - num2);
			aes.IV = readOnlySpan.ToArray();
			ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
			using MemoryStream stream = new MemoryStream(cipherdata.ToArray());
			using CryptoStream cryptoStream = new CryptoStream(stream, transform, CryptoStreamMode.Read);
			byte[] array = new byte[num];
			cryptoStream.ReadExactly(array);
			plaindata = array;
			return EncryptionResult.Success;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Error decrypting cipherdata");
			return EncryptionResult.Error;
		}
	}

	public override EncryptionResult TryDecrypt(ReadOnlySpan<byte> cipherdata, int offset, int length, out ReadOnlySpan<byte> plaindata)
	{
		return TryDecrypt(cipherdata.Slice(offset, offset + length - offset), out plaindata);
	}

	public override EncryptionResult TryEncrypt(ReadOnlySpan<byte> plaindata, out ReadOnlySpan<byte> cipherdata)
	{
		cipherdata = default(ReadOnlySpan<byte>);
		try
		{
			using Aes aes = Aes.Create();
			aes.Key = _key.ToArray();
			byte[] array = (aes.IV = RandomNumberGenerator.GetBytes(aes.BlockSize / 8));
			using MemoryStream memoryStream = new MemoryStream();
			memoryStream.WriteByte(0);
			byte[] array2 = new byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(array2, plaindata.Length);
			memoryStream.Write(array2);
			memoryStream.WriteByte((byte)array.Length);
			memoryStream.Write(array);
			ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
			using CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write, leaveOpen: true);
			cryptoStream.Write(plaindata);
			cryptoStream.Close();
			memoryStream.Close();
			cipherdata = memoryStream.ToArray();
			return EncryptionResult.Success;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Error encrypting plaindata");
			return EncryptionResult.Error;
		}
	}

	public override EncryptionResult TryEncrypt(ReadOnlySpan<byte> plaindata, int offset, int length, out ReadOnlySpan<byte> cipherdata)
	{
		return TryEncrypt(plaindata.Slice(offset, offset + length - offset), out cipherdata);
	}
}
