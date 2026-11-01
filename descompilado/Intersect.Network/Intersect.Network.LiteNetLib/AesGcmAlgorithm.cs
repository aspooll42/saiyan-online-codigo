using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.LiteNetLib;

public sealed class AesGcmAlgorithm : SymmetricAlgorithm
{
	private const int HeaderSize = 7;

	private readonly HashSet<AesGcm> _algorithmInstances = new HashSet<AesGcm>();

	private readonly HashSet<AesGcm> _algorithmInstancesToDispose = new HashSet<AesGcm>();

	private readonly Stack<AesGcm> _algorithmPool = new Stack<AesGcm>();

	private readonly Memory<byte> _key;

	private readonly byte _nonceSize;

	private readonly byte _tagSize;

	private MemoryHandle _keyHandle;

	public AesGcmAlgorithm(ReadOnlySpan<byte> key = default(ReadOnlySpan<byte>))
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(32);
		if (key != default(ReadOnlySpan<byte>))
		{
			int length = key.Length;
			if (length != 32)
			{
				throw new ArgumentOutOfRangeException("key", length, "Key size must be exactly 32 bytes.");
			}
			if (!key.TryCopyTo(bytes))
			{
				throw new ArgumentException("The provided key could not be copied to the internal buffer.", "key");
			}
		}
		_key = new Memory<byte>(bytes);
		_keyHandle = _key.Pin();
		if (!SymmetricAlgorithm.TryPickValidSize(AesGcm.NonceByteSizes, out _nonceSize))
		{
			throw new SecurityException("Failed to initialize network encryption, no valid nonce sizes");
		}
		if (!SymmetricAlgorithm.TryPickValidSize(AesGcm.TagByteSizes, out _tagSize))
		{
			throw new SecurityException("Failed to initialize network encryption, no valid tag sizes");
		}
	}

	private AesGcm AcquireInstance()
	{
		lock (_algorithmPool)
		{
			if (_algorithmPool.TryPop(out AesGcm result))
			{
				return result;
			}
			result = new AesGcm(_key.Span, _tagSize);
			_algorithmInstances.Add(result);
			return result;
		}
	}

	private void ReleaseInstance(AesGcm algorithmInstance)
	{
		lock (_algorithmPool)
		{
			if (_algorithmInstancesToDispose.Remove(algorithmInstance))
			{
				algorithmInstance.Dispose();
			}
			else
			{
				_algorithmPool.Push(algorithmInstance);
			}
		}
	}

	public override void Dispose()
	{
		_keyHandle.Dispose();
		ClearPool();
	}

	private void ClearPool()
	{
		lock (_algorithmPool)
		{
			AesGcm result;
			while (_algorithmPool.TryPop(out result))
			{
				_algorithmInstances.Remove(result);
				result.Dispose();
			}
			foreach (AesGcm algorithmInstance in _algorithmInstances)
			{
				_algorithmInstancesToDispose.Add(algorithmInstance);
			}
			_algorithmInstances.Clear();
		}
	}

	public override bool SetKey(ReadOnlySpan<byte> key)
	{
		if (!key.TryCopyTo(_key.Span))
		{
			return false;
		}
		ClearPool();
		return true;
	}

	public override EncryptionResult TryDecrypt(ReadOnlySpan<byte> cipherdata, out ReadOnlySpan<byte> plaindata)
	{
		if (cipherdata.Length < 7)
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.NoHeader;
		}
		ReadOnlySpan<byte> readOnlySpan = cipherdata;
		byte b = readOnlySpan[0];
		ref ReadOnlySpan<byte> reference = ref readOnlySpan;
		readOnlySpan = reference.Slice(1, reference.Length - 1);
		if (b != 1)
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.InvalidVersion;
		}
		int num = BinaryPrimitives.ReadInt32LittleEndian(readOnlySpan);
		if (num < 1)
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.EmptyInput;
		}
		reference = ref readOnlySpan;
		readOnlySpan = reference.Slice(4, reference.Length - 4);
		byte b2 = readOnlySpan[0];
		if (!SymmetricAlgorithm.IsValidSize(AesGcm.NonceByteSizes, b2))
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.InvalidNonce;
		}
		reference = ref readOnlySpan;
		readOnlySpan = reference.Slice(1, reference.Length - 1);
		byte b3 = readOnlySpan[0];
		if (!SymmetricAlgorithm.IsValidSize(AesGcm.TagByteSizes, b3))
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.InvalidTag;
		}
		reference = ref readOnlySpan;
		readOnlySpan = reference.Slice(1, reference.Length - 1);
		ReadOnlySpan<byte> nonce = readOnlySpan.Slice(0, b2);
		reference = ref readOnlySpan;
		int num2 = b2;
		readOnlySpan = reference.Slice(num2, reference.Length - num2);
		ReadOnlySpan<byte> tag = readOnlySpan.Slice(0, b3);
		reference = ref readOnlySpan;
		num2 = b3;
		readOnlySpan = reference.Slice(num2, reference.Length - num2);
		if (readOnlySpan.Length != num)
		{
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.SizeMismatch;
		}
		byte[] array = new byte[num];
		Memory<byte> memory = new Memory<byte>(array);
		try
		{
			AesGcm aesGcm = AcquireInstance();
			aesGcm.Decrypt(nonce, readOnlySpan, tag, memory.Span);
			ReleaseInstance(aesGcm);
			plaindata = memory.Span;
			return EncryptionResult.Success;
		}
		catch (Exception exception)
		{
			LogLevel logLevel = Options.Instance?.Logging.Level ?? LogLevel.Trace;
			if (logLevel >= LogLevel.Debug)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Error decrypting cipherdata");
			}
			plaindata = default(ReadOnlySpan<byte>);
			return EncryptionResult.Error;
		}
	}

	public override EncryptionResult TryDecrypt(ReadOnlySpan<byte> cipherdata, int offset, int length, out ReadOnlySpan<byte> plaindata)
	{
		return TryDecrypt(cipherdata.Slice(offset, offset + length - offset), out plaindata);
	}

	private EncryptionResult TryEncryptInternal(ReadOnlySpan<byte> plaindata, ReadOnlySpan<byte> nonce, out ReadOnlySpan<byte> cipherdata)
	{
		if (plaindata.Length < 1)
		{
			cipherdata = default(ReadOnlySpan<byte>);
			return EncryptionResult.EmptyInput;
		}
		if (nonce.Length != _nonceSize)
		{
			cipherdata = default(ReadOnlySpan<byte>);
			return EncryptionResult.InvalidNonce;
		}
		byte b = (byte)nonce.Length;
		int num = 7 + b + _tagSize + plaindata.Length;
		byte[] array = new byte[num];
		Memory<byte> memory = new Memory<byte>(array);
		Span<byte> span = memory.Span;
		span[0] = 1;
		ref Span<byte> reference = ref span;
		span = reference.Slice(1, reference.Length - 1);
		BinaryPrimitives.WriteInt32LittleEndian(span, plaindata.Length);
		reference = ref span;
		span = reference.Slice(4, reference.Length - 4);
		span[0] = b;
		reference = ref span;
		span = reference.Slice(1, reference.Length - 1);
		span[0] = _tagSize;
		reference = ref span;
		span = reference.Slice(1, reference.Length - 1);
		nonce.CopyTo(span.Slice(0, b));
		reference = ref span;
		int num2 = b;
		span = reference.Slice(num2, reference.Length - num2);
		Span<byte> tag = span.Slice(0, _tagSize);
		reference = ref span;
		num2 = _tagSize;
		span = reference.Slice(num2, reference.Length - num2);
		try
		{
			AesGcm aesGcm = AcquireInstance();
			aesGcm.Encrypt(nonce, plaindata, span, tag);
			ReleaseInstance(aesGcm);
			cipherdata = memory.Span;
			return EncryptionResult.Success;
		}
		catch (Exception exception)
		{
			LogLevel logLevel = Options.Instance?.Logging.Level ?? LogLevel.Trace;
			if (logLevel >= LogLevel.Debug)
			{
				ApplicationContext.Context.Value?.Logger.LogError(exception, "Error encrypting plaindata");
			}
			cipherdata = default(ReadOnlySpan<byte>);
			return EncryptionResult.Error;
		}
	}

	public override EncryptionResult TryEncrypt(ReadOnlySpan<byte> plaindata, out ReadOnlySpan<byte> cipherdata)
	{
		byte[] array = new byte[_nonceSize];
		RandomNumberGenerator.Fill(array);
		return TryEncryptInternal(plaindata, array, out cipherdata);
	}

	public override EncryptionResult TryEncrypt(ReadOnlySpan<byte> plaindata, int offset, int length, out ReadOnlySpan<byte> cipherdata)
	{
		return TryEncrypt(plaindata.Slice(offset, offset + length - offset), out cipherdata);
	}
}
