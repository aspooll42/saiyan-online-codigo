using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Intersect.Core;
using Intersect.Framework.Reflection;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Intersect.Network;

[MessagePackObject(false)]
public sealed class AsymmetricEncryptedPacket : IntersectPacket
{
	private byte[]? _encryptedData;

	private IntersectPacket? _innerPacket;

	[Key(0)]
	public byte[]? EncryptedData
	{
		get
		{
			byte[] encryptedData;
			return _encryptedData ?? (_encryptedData = (TryEncrypt(out encryptedData) ? encryptedData : null));
		}
		set
		{
			_encryptedData = value;
			_innerPacket = null;
		}
	}

	[IgnoreMember]
	public IntersectPacket? InnerPacket
	{
		get
		{
			IntersectPacket innerPacket;
			return _innerPacket ?? (_innerPacket = (TryDecrypt(out innerPacket) ? innerPacket : null));
		}
		set
		{
			_encryptedData = null;
			_innerPacket = value;
		}
	}

	[IgnoreMember]
	public RSAParameters? Parameters { get; set; }

	[SerializationConstructor]
	public AsymmetricEncryptedPacket()
	{
	}

	public AsymmetricEncryptedPacket(IntersectPacket innerPacket, RSAParameters parameters)
	{
		InnerPacket = innerPacket;
		Parameters = parameters;
	}

	private bool TryDecrypt([NotNullWhen(true)] out IntersectPacket? innerPacket)
	{
		innerPacket = null;
		RSAParameters? parameters = Parameters;
		byte[] encryptedData = _encryptedData;
		if (!parameters.HasValue || encryptedData == null)
		{
			return false;
		}
		try
		{
			using RSA rSA = RSA.Create(parameters.Value);
			byte[] packetData = rSA.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
			object obj = MessagePacker.Instance.Deserialize(packetData);
			innerPacket = obj as IntersectPacket;
			if (innerPacket != null)
			{
				return true;
			}
			if (obj == null)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Null packet");
				return false;
			}
			ApplicationContext.Context.Value?.Logger.LogDebug("Expected InnerPacket, received " + obj.GetFullishName());
		}
		catch (Exception ex)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("Paquet illisible ({Octets} octets) : {Erreur}", encryptedData.Length, ex.Message);
			ApplicationContext.Context.Value?.Logger.LogTrace($"{"TryDecrypt"} {"encryptedData"}({encryptedData.Length})={Convert.ToHexString(encryptedData)}");
		}
		return false;
	}

	private bool TryEncrypt([NotNullWhen(true)] out byte[]? encryptedData)
	{
		encryptedData = null;
		RSAParameters? parameters = Parameters;
		IntersectPacket innerPacket = _innerPacket;
		if (!parameters.HasValue || innerPacket == null)
		{
			return false;
		}
		try
		{
			using RSA rSA = RSA.Create(parameters.Value);
			byte[] data = innerPacket.Data;
			encryptedData = rSA.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
			return true;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error encrypting packet");
			return false;
		}
	}
}
