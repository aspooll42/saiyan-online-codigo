using System;
using System.Security.Cryptography;
using Intersect.Core;
using Intersect.Memory;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Intersect.Network.Packets;

[MessagePackObject(false)]
public class HailPacket : ConnectionPacket
{
	[IgnoreMember]
	private byte[] mVersionData;

	[IgnoreMember]
	private RSAParameters mRsaParameters;

	[IgnoreMember]
	private byte _symmetricVersion = (AesGcm.IsSupported ? ((byte)1) : ((byte)0));

	[IgnoreMember]
	public byte[] VersionData
	{
		get
		{
			return mVersionData;
		}
		set
		{
			mVersionData = value;
		}
	}

	[IgnoreMember]
	public RSAParameters RsaParameters
	{
		get
		{
			return mRsaParameters;
		}
		set
		{
			mRsaParameters = value;
		}
	}

	[IgnoreMember]
	public byte SymmetricVersion
	{
		get
		{
			return _symmetricVersion;
		}
		set
		{
			_symmetricVersion = value;
		}
	}

	public HailPacket()
	{
	}

	public HailPacket(RSACryptoServiceProvider rsa)
		: base(rsa, null)
	{
	}

	public HailPacket(RSA rsa, byte[] handshakeSecret, byte[] versionData, RSAParameters rsaParameters)
		: base(rsa, handshakeSecret)
	{
		VersionData = versionData;
		RsaParameters = rsaParameters;
	}

	public override bool Encrypt()
	{
		using MemoryBuffer memoryBuffer = new MemoryBuffer();
		memoryBuffer.Write(VersionData);
		memoryBuffer.Write(base.HandshakeSecret, 32L);
		memoryBuffer.Write(base.Adjusted);
		ushort num = (ushort)(RsaParameters.Modulus.Length << 3);
		memoryBuffer.Write(num);
		memoryBuffer.Write(RsaParameters.Exponent, 3L);
		memoryBuffer.Write(RsaParameters.Modulus, num >> 3);
		memoryBuffer.Write(SymmetricVersion);
		base.EncryptedData = mRsa.Encrypt(memoryBuffer.ToArray(), RSAEncryptionPadding.OaepSHA256) ?? throw new InvalidOperationException("Failed to encrypt the buffer.");
		return true;
	}

	public override bool Decrypt(RSA rsa)
	{
		try
		{
			mRsa = rsa;
			if (mRsa == null)
			{
				throw new ArgumentNullException("rsa");
			}
			byte[] data = mRsa.Decrypt(base.EncryptedData, RSAEncryptionPadding.OaepSHA256);
			using MemoryBuffer memoryBuffer = new MemoryBuffer(data);
			if (!memoryBuffer.Read(out mVersionData))
			{
				return false;
			}
			if (!memoryBuffer.Read(out mHandshakeSecret, 32L))
			{
				return false;
			}
			if (!memoryBuffer.Read(out mAdjusted))
			{
				return false;
			}
			if (!memoryBuffer.Read(out ushort value))
			{
				return false;
			}
			RsaParameters = default(RSAParameters);
			if (!memoryBuffer.Read(out mRsaParameters.Exponent, 3L))
			{
				return false;
			}
			if (!memoryBuffer.Read(out mRsaParameters.Modulus, value >> 3))
			{
				return false;
			}
			if (!memoryBuffer.Read(out _symmetricVersion))
			{
				return false;
			}
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error decrypting hail packet");
			return false;
		}
	}
}
