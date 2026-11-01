using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Intersect.Memory;
using LiteNetLib.Utils;

namespace Intersect.Network.LiteNetLib;

public sealed class LiteNetLibInboundBuffer : IBuffer, IDisposable
{
	private readonly NetDataReader _reader;

	public long Length => _reader.RawDataSize;

	public long Position => _reader.Position;

	public long Remaining => _reader.AvailableBytes;

	public LiteNetLibInboundBuffer(NetDataReader reader)
	{
		_reader = reader ?? throw new ArgumentNullException("reader");
	}

	public void Dispose()
	{
	}

	public byte[] ToBytes()
	{
		return _reader.RawData;
	}

	public bool Has(long bytes)
	{
		return Remaining >= bytes;
	}

	public bool Read(out bool value)
	{
		return _reader.TryGetBool(out value);
	}

	public bool Read(out byte value)
	{
		return _reader.TryGetByte(out value);
	}

	public bool Read(out byte[] value)
	{
		return _reader.TryGetBytesWithLength(out value);
	}

	public bool Read([NotNullWhen(true)] out byte[]? value, long count)
	{
		if (!Has(count))
		{
			value = null;
			return false;
		}
		value = new byte[count];
		_reader.GetBytes(value, value.Length);
		return true;
	}

	public bool Read(ref byte[] value, long offset, long count)
	{
		if (!Has(count))
		{
			return false;
		}
		_reader.GetBytes(value, (int)offset, (int)count);
		return true;
	}

	public bool Read(out char value)
	{
		return _reader.TryGetChar(out value);
	}

	public bool Read(out decimal value)
	{
		if (Remaining < 16)
		{
			value = default(decimal);
			return false;
		}
		ArraySegment<byte> bytesSegment = _reader.GetBytesSegment(16);
		Span<int> span = MemoryMarshal.Cast<byte, int>(bytesSegment);
		value = new decimal(span);
		return true;
	}

	public bool Read(out double value)
	{
		return _reader.TryGetDouble(out value);
	}

	public bool Read(out float value)
	{
		return _reader.TryGetFloat(out value);
	}

	public bool Read(out int value)
	{
		return _reader.TryGetInt(out value);
	}

	public bool Read(out long value)
	{
		return _reader.TryGetLong(out value);
	}

	public bool Read(out sbyte value)
	{
		return _reader.TryGetSByte(out value);
	}

	public bool Read(out short value)
	{
		return _reader.TryGetShort(out value);
	}

	public bool Read([NotNullWhen(true)] out string? value)
	{
		return _reader.TryGetString(out value);
	}

	public bool Read([NotNullWhen(true)] out string? value, Encoding encoding, bool nullTerminated = false)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		int value2 = 0;
		if (nullTerminated)
		{
			long num = Position;
			byte[] array = _reader.RawData ?? Array.Empty<byte>();
			int num2 = array.Length;
			while (num2 > num && array[num] != 0)
			{
				num++;
				if (num2 == num)
				{
					value = null;
					return false;
				}
				value2++;
			}
		}
		else if (!Read(out value2))
		{
			value = null;
			return false;
		}
		byte[] value3;
		string text = ((value2 < 0) ? null : ((value2 != 0) ? (Read(out value3, value2) ? encoding.GetString(value3, 0, value2) : null) : string.Empty));
		value = text;
		return true;
	}

	public bool Read(out uint value)
	{
		return _reader.TryGetUInt(out value);
	}

	public bool Read(out ulong value)
	{
		return _reader.TryGetULong(out value);
	}

	public bool Read(out ushort value)
	{
		return _reader.TryGetUShort(out value);
	}

	public void Write(bool value)
	{
		throw new NotSupportedException();
	}

	public void Write(byte value)
	{
		throw new NotSupportedException();
	}

	public void Write(byte[] value)
	{
		throw new NotSupportedException();
	}

	public void Write(byte[] value, long count)
	{
		throw new NotSupportedException();
	}

	public void Write(byte[] value, long offset, long count)
	{
		throw new NotSupportedException();
	}

	public void Write(char value)
	{
		throw new NotSupportedException();
	}

	public void Write(decimal value)
	{
		throw new NotSupportedException();
	}

	public void Write(double value)
	{
		throw new NotSupportedException();
	}

	public void Write(float value)
	{
		throw new NotSupportedException();
	}

	public void Write(int value)
	{
		throw new NotSupportedException();
	}

	public void Write(long value)
	{
		throw new NotSupportedException();
	}

	public void Write(sbyte value)
	{
		throw new NotSupportedException();
	}

	public void Write(short value)
	{
		throw new NotSupportedException();
	}

	public void Write(string value)
	{
		throw new NotSupportedException();
	}

	public void Write(string value, Encoding encoding, bool nullTerminated = false)
	{
		throw new NotSupportedException();
	}

	public void Write(uint value)
	{
		throw new NotSupportedException();
	}

	public void Write(ulong value)
	{
		throw new NotSupportedException();
	}

	public void Write(ushort value)
	{
		throw new NotSupportedException();
	}
}
