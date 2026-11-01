using System;
using System.Collections.Generic;
using Intersect.Collections;

namespace Intersect.Network;

public abstract class CerasPacket : IPacket, IDisposable
{
	private static Ceras sCerasInstance { get; set; }

	private static Ceras Ceras
	{
		get
		{
			Ceras result = sCerasInstance ?? new Ceras();
			sCerasInstance = result;
			return result;
		}
	}

	public virtual byte[] Data => Ceras.Serialize(this) ?? throw new Exception("Failed to serialize packet.");

	public virtual bool IsValid => true;

	public abstract long ReceiveTime { get; set; }

	public abstract long ProcessTime { get; set; }

	public virtual void Dispose()
	{
	}

	public virtual Dictionary<string, SanitizedValue<object>> Sanitize()
	{
		return null;
	}
}
