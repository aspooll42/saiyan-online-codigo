using System;
using System.Security.Cryptography;
using Intersect.Framework.Core;
using Intersect.Network.Packets;
using MessagePack;

namespace Intersect.Network;

[MessagePackObject(false)]
[Union(0, typeof(ApprovalPacket))]
[Union(1, typeof(HailPacket))]
public abstract class ConnectionPacket : IntersectPacket
{
	protected const int SIZE_HANDSHAKE_SECRET = 32;

	[IgnoreMember]
	protected RSA mRsa;

	[IgnoreMember]
	protected byte[] mHandshakeSecret;

	[IgnoreMember]
	protected long mAdjusted;

	[IgnoreMember]
	protected long mUTC;

	[IgnoreMember]
	protected long mOffset;

	[IgnoreMember]
	public byte[] HandshakeSecret
	{
		get
		{
			return mHandshakeSecret;
		}
		set
		{
			mHandshakeSecret = value;
		}
	}

	[IgnoreMember]
	public long Adjusted
	{
		get
		{
			return mAdjusted;
		}
		set
		{
			mAdjusted = value;
		}
	}

	[IgnoreMember]
	public long UTC
	{
		get
		{
			return mUTC;
		}
		set
		{
			mUTC = value;
		}
	}

	[IgnoreMember]
	public long Offset
	{
		get
		{
			return mOffset;
		}
		set
		{
			mOffset = value;
		}
	}

	[Key(0)]
	public byte[] EncryptedData { get; set; }

	protected ConnectionPacket()
	{
	}

	protected ConnectionPacket(RSA rsa, byte[] handshakeSecret)
	{
		mRsa = rsa ?? throw new ArgumentNullException("rsa");
		mHandshakeSecret = handshakeSecret;
		Adjusted = Timing.Global.Ticks;
		Offset = Timing.Global.TicksOffset;
		UTC = Timing.Global.TicksUtc;
	}

	public abstract bool Encrypt();

	public abstract bool Decrypt(RSA rsa);

	protected static void DumpKey(RSAParameters parameters, bool isPublic)
	{
	}
}
