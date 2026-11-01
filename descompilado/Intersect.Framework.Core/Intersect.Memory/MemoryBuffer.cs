using System;
using System.IO;
using System.Text;

namespace Intersect.Memory;

public class MemoryBuffer : MemoryStream, IBuffer, IDisposable
{
	public new long Length => base.Length;

	public new long Position => base.Position;

	public long Remaining => Length - Position;

	public MemoryBuffer()
		: this(0)
	{
	}

	public MemoryBuffer(int capacity)
		: base(capacity)
	{
	}

	public MemoryBuffer(IBuffer buffer)
		: base(buffer?.ToBytes() ?? Array.Empty<byte>())
	{
	}

	public MemoryBuffer(byte[] data)
		: base(data)
	{
	}

	public byte[] ToBytes()
	{
		return ToArray();
	}

	public bool Has(long bytes)
	{
		return bytes <= Remaining;
	}

	public bool Read(out bool value)
	{
		if (!Has(1L))
		{
			value = false;
			return false;
		}
		byte[] array = new byte[1];
		Read(array, 0, 1);
		value = BitConverter.ToBoolean(array, 0);
		return true;
	}

	public bool Read(out byte value)
	{
		if (!Has(1L))
		{
			value = 0;
			return false;
		}
		value = (byte)(base.ReadByte() & 0xFF);
		return true;
	}

	public bool Read(out byte[] value)
	{
		if (!Read(out int value2))
		{
			value = null;
			return false;
		}
		value = new byte[value2];
		return Read(ref value, 0L, value2);
	}

	public bool Read(out byte[] value, long count)
	{
		value = new byte[count];
		return Read(ref value, 0L, count);
	}

	public bool Read(ref byte[] value, long offset, long count)
	{
		return count == Read(value, (int)offset, (int)count);
	}

	public bool Read(out char value)
	{
		if (Has(2L) && Read(out var value2, 2L))
		{
			value = BitConverter.ToChar(value2, 0);
			return true;
		}
		value = '\0';
		return false;
	}

	public bool Read(out decimal value)
	{
		value = default(decimal);
		int[] array = new int[4];
		if (!Read(out array[0]))
		{
			return false;
		}
		if (!Read(out array[1]))
		{
			return false;
		}
		if (!Read(out array[2]))
		{
			return false;
		}
		if (!Read(out array[3]))
		{
			return false;
		}
		value = new decimal(array);
		return true;
	}

	public bool Read(out double value)
	{
		if (!Has(8L))
		{
			value = 0.0;
			return false;
		}
		byte[] array = new byte[8];
		Read(array, 0, 8);
		value = BitConverter.ToDouble(array, 0);
		return true;
	}

	public bool Read(out float value)
	{
		if (!Has(4L))
		{
			value = 0f;
			return false;
		}
		byte[] array = new byte[4];
		Read(array, 0, 4);
		value = BitConverter.ToSingle(array, 0);
		return true;
	}

	public bool Read(out int value)
	{
		if (!Has(4L))
		{
			value = 0;
			return false;
		}
		byte[] array = new byte[4];
		Read(array, 0, 4);
		value = BitConverter.ToInt32(array, 0);
		return true;
	}

	public bool Read(out long value)
	{
		if (!Has(8L))
		{
			value = 0L;
			return false;
		}
		byte[] array = new byte[8];
		Read(array, 0, 8);
		value = BitConverter.ToInt64(array, 0);
		return true;
	}

	public bool Read(out sbyte value)
	{
		if (!Has(1L))
		{
			value = 0;
			return false;
		}
		byte[] array = new byte[1];
		Read(array, 0, 1);
		value = (sbyte)array[0];
		return true;
	}

	public bool Read(out short value)
	{
		if (!Has(2L))
		{
			value = 0;
			return false;
		}
		byte[] array = new byte[2];
		Read(array, 0, 2);
		value = BitConverter.ToInt16(array, 0);
		return true;
	}

	public bool Read(out string value)
	{
		return Read(out value, Encoding.UTF8);
	}

	public bool Read(out string value, Encoding encoding, bool nullTerminated = false)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException();
		}
		int value2;
		if (nullTerminated)
		{
			if (!CanSeek)
			{
				throw new InvalidOperationException("Unable to read null-terminated strings on a Stream without seek.");
			}
			long position = base.Position;
			while (Length - Position > 0 && base.ReadByte() != 0)
			{
				base.Position++;
			}
			base.Position = position;
			value2 = (int)(Length - position);
		}
		else if (!Read(out value2))
		{
			value = null;
			return false;
		}
		switch (value2)
		{
		case 0:
			value = string.Empty;
			break;
		case -1:
			value = null;
			break;
		default:
		{
			value = (Read(out var value3, value2) ? encoding.GetString(value3, 0, value2) : null);
			break;
		}
		}
		return true;
	}

	public bool Read(out uint value)
	{
		if (!Has(4L))
		{
			value = 0u;
			return false;
		}
		byte[] array = new byte[4];
		Read(array, 0, 4);
		value = BitConverter.ToUInt32(array, 0);
		return true;
	}

	public bool Read(out ulong value)
	{
		if (!Has(8L))
		{
			value = 0uL;
			return false;
		}
		byte[] array = new byte[8];
		Read(array, 0, 8);
		value = BitConverter.ToUInt64(array, 0);
		return true;
	}

	public bool Read(out ushort value)
	{
		if (!Has(2L))
		{
			value = 0;
			return false;
		}
		byte[] array = new byte[2];
		Read(array, 0, 2);
		value = BitConverter.ToUInt16(array, 0);
		return true;
	}

	public bool ReadBool()
	{
		if (Read(out bool value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public bool ReadBoolean()
	{
		if (Read(out bool value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public new byte ReadByte()
	{
		return (byte)(base.ReadByte() & 0xFF);
	}

	public byte ReadUInt8()
	{
		if (Read(out byte value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public byte[] ReadBytes()
	{
		if (Read(out byte[] value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public byte[] ReadBytes(long count)
	{
		if (Read(out var value, count))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public byte[] ReadBytes(ref byte[] bytes, long offset, long count)
	{
		if (Read(ref bytes, offset, count))
		{
			return bytes;
		}
		throw new OutOfMemoryException();
	}

	public char ReadChar()
	{
		if (Read(out char value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public char ReadCharacter()
	{
		if (Read(out char value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public decimal ReadDecimal()
	{
		if (Read(out decimal value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public double ReadDouble()
	{
		if (Read(out double value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public float ReadFloat()
	{
		if (Read(out float value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public float ReadSingle()
	{
		if (Read(out float value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public int ReadInt()
	{
		if (Read(out int value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public int ReadInt32()
	{
		if (Read(out int value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public int ReadInteger()
	{
		if (Read(out int value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public long ReadInt64()
	{
		if (Read(out long value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public long ReadLong()
	{
		if (Read(out long value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public sbyte ReadInt8()
	{
		if (Read(out sbyte value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public sbyte ReadSByte()
	{
		if (Read(out sbyte value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public short ReadInt16()
	{
		if (Read(out short value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public short ReadShort()
	{
		if (Read(out short value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public string ReadString()
	{
		if (Read(out string value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public string ReadString(Encoding encoding, bool nullTerminated = false)
	{
		if (Read(out var value, encoding, nullTerminated))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public uint ReadUInt()
	{
		if (Read(out uint value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public uint ReadUInt32()
	{
		if (Read(out uint value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public uint ReadUnsignedInteger()
	{
		if (Read(out uint value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public ulong ReadULong()
	{
		if (Read(out ulong value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public ulong ReadUInt64()
	{
		if (Read(out ulong value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public ushort ReadUInt16()
	{
		if (Read(out ushort value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public ushort ReadUShort()
	{
		if (Read(out ushort value))
		{
			return value;
		}
		throw new OutOfMemoryException();
	}

	public void Write(bool value)
	{
		Write(BitConverter.GetBytes(value), 0L, 1L);
	}

	public void Write(byte value)
	{
		WriteByte(value);
	}

	public void Write(byte[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException();
		}
		Write(value.Length);
		Write(value, 0L, value.Length);
	}

	public void Write(byte[] value, long count)
	{
		Write(value, 0L, count);
	}

	public void Write(byte[] value, long offset, long count)
	{
		base.Write(value ?? Array.Empty<byte>(), 0, (int)count);
	}

	public void Write(char value)
	{
		Write(BitConverter.GetBytes(value), 0L, 2L);
	}

	public void Write(decimal value)
	{
		int[] bits = decimal.GetBits(value);
		Write(bits[0]);
		Write(bits[1]);
		Write(bits[2]);
		Write(bits[3]);
	}

	public void Write(double value)
	{
		Write(BitConverter.GetBytes(value), 0L, 8L);
	}

	public void Write(float value)
	{
		Write(BitConverter.GetBytes(value), 0L, 4L);
	}

	public void Write(int value)
	{
		Write(BitConverter.GetBytes(value), 0L, 4L);
	}

	public void Write(long value)
	{
		Write(BitConverter.GetBytes(value), 0L, 8L);
	}

	public void Write(sbyte value)
	{
		WriteByte((byte)value);
	}

	public void Write(short value)
	{
		Write(BitConverter.GetBytes(value), 0L, 2L);
	}

	public void Write(string value)
	{
		Write(value, Encoding.UTF8);
	}

	public void Write(string value, Encoding encoding, bool nullTerminated = false)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException();
		}
		if (!nullTerminated)
		{
			Write(value?.Length ?? (-1));
		}
		if (value != null && value.Length >= 1)
		{
			byte[] bytes = encoding.GetBytes(value);
			Write(bytes, bytes.Length);
		}
	}

	public void Write(uint value)
	{
		Write(BitConverter.GetBytes(value), 0L, 4L);
	}

	public void Write(ulong value)
	{
		Write(BitConverter.GetBytes(value), 0L, 8L);
	}

	public void Write(ushort value)
	{
		Write(BitConverter.GetBytes(value), 0L, 2L);
	}
}
