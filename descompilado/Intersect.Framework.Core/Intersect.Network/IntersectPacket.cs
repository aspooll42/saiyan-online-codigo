using System;
using System.Collections.Generic;
using Intersect.Collections;
using Intersect.Framework.Reflection;
using MessagePack;

namespace Intersect.Network;

public abstract class IntersectPacket : IPacket, IDisposable
{
	[IgnoreMember]
	private byte[]? _cachedData;

	[IgnoreMember]
	private byte[]? _cachedCompressedData;

	[IgnoreMember]
	public virtual byte[] Data
	{
		get
		{
			if (_cachedData == null)
			{
				_cachedData = MessagePacker.Instance.Serialize(this) ?? throw new Exception("Failed to serialize " + this.GetFullishName());
			}
			return _cachedData;
		}
	}

	[IgnoreMember]
	public virtual bool IsValid => true;

	[IgnoreMember]
	public virtual long ReceiveTime { get; set; }

	[IgnoreMember]
	public virtual long ProcessTime { get; set; }

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public virtual void ClearCachedData()
	{
		_cachedData = null;
	}

	public virtual Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		return null;
	}
}
