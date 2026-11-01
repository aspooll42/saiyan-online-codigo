using System;
using System.Net;
using System.Net.Sockets;

namespace Intersect.Framework.Net;

public readonly record struct IPRange
{
	public AddressFamily AddressFamily { get; }

	public byte[] Start { get; }

	public byte[] End { get; }

	public IPRange(IPAddress address)
	{
		ArgumentNullException.ThrowIfNull(address, "address");
		AddressFamily = address.AddressFamily;
		Start = address.GetAddressBytes();
		End = address.GetAddressBytes();
	}

	public IPRange(IPAddress start, IPAddress end)
	{
		ArgumentNullException.ThrowIfNull(start, "start");
		ArgumentNullException.ThrowIfNull(end, "end");
		if (start.AddressFamily != end.AddressFamily)
		{
			throw new ArgumentException("AddressFamily mismatch");
		}
		AddressFamily = start.AddressFamily;
		Start = start.GetAddressBytes();
		End = end.GetAddressBytes();
	}

	public bool Contains(IPAddress address)
	{
		ArgumentNullException.ThrowIfNull(address, "address");
		if (address.AddressFamily != AddressFamily)
		{
			return false;
		}
		byte[] addressBytes = address.GetAddressBytes();
		if (Start.Length != End.Length || End.Length != addressBytes.Length)
		{
			return false;
		}
		for (int i = 0; i < addressBytes.Length; i++)
		{
			byte b = Start[i];
			byte b2 = End[i];
			byte b3 = addressBytes[i];
			if (b3 < b || b2 < b3)
			{
				return false;
			}
		}
		return true;
	}
}
