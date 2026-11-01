using System;
using System.Security.Cryptography;
using Intersect.Core;
using Intersect.Memory;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.Packets;

[MessagePackObject(false)]
public class ApprovalPacket : ConnectionPacket
{
	[IgnoreMember]
	private const int SIZE_AES_KEY = 32;

	[IgnoreMember]
	private const int SIZE_GUID = 16;

	[IgnoreMember]
	private byte[] mAesKey;

	[IgnoreMember]
	public Guid Guid { get; set; }

	[IgnoreMember]
	public byte[] AesKey
	{
		get
		{
			return mAesKey;
		}
		set
		{
			mAesKey = value;
		}
	}

	public ApprovalPacket()
	{
	}

	public ApprovalPacket(RSA rsa, byte[] handshakeSecret, byte[] aesKey, Guid guid)
		: base(rsa, handshakeSecret)
	{
		AesKey = aesKey;
		Guid = guid;
	}

	public ApprovalPacket(RSAParameters rsaParameters, byte[] handshakeSecret, byte[] aesKey, Guid guid)
		: base(RSA.Create(rsaParameters), handshakeSecret)
	{
		AesKey = aesKey;
		Guid = guid;
	}

	public override bool Encrypt()
	{
		using MemoryBuffer memoryBuffer = new MemoryBuffer();
		memoryBuffer.Write(base.HandshakeSecret, 32L);
		memoryBuffer.Write(AesKey, 32L);
		memoryBuffer.Write(Guid.ToByteArray(), 16L);
		memoryBuffer.Write(base.Adjusted);
		base.EncryptedData = mRsa.Encrypt(memoryBuffer.ToArray(), RSAEncryptionPadding.OaepSHA256) ?? throw new InvalidOperationException("Failed to encrypt the buffer.");
		return true;
	}

	public override bool Decrypt(RSA rsa)
	{
		mRsa = rsa;
		if (mRsa == null)
		{
			throw new ArgumentNullException("rsa");
		}
		try
		{
			byte[] data = mRsa.Decrypt(base.EncryptedData, RSAEncryptionPadding.OaepSHA256);
			using MemoryBuffer memoryBuffer = new MemoryBuffer(data);
			if (!memoryBuffer.Read(out mHandshakeSecret, 32L))
			{
				return false;
			}
			if (!memoryBuffer.Read(out mAesKey, 32L))
			{
				return false;
			}
			if (!memoryBuffer.Read(out var value, 16L) || value == null)
			{
				return false;
			}
			Guid = new Guid(value);
			if (!memoryBuffer.Read(out mAdjusted))
			{
				return false;
			}
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error decrypting approval packet");
			return false;
		}
	}
}
