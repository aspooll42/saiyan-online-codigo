using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Intersect.Memory;

public interface IBuffer : IDisposable
{
	long Length { get; }

	long Position { get; }

	long Remaining { get; }

	byte[] ToBytes();

	bool Has(long bytes);

	bool Read(out bool value);

	bool Read(out byte value);

	bool Read(out byte[] value);

	bool Read(out byte[] value, long count);

	bool Read(ref byte[] value, long offset, long count);

	bool Read(out char value);

	bool Read(out decimal value);

	bool Read(out double value);

	bool Read(out float value);

	bool Read(out int value);

	bool Read(out long value);

	bool Read(out sbyte value);

	bool Read(out short value);

	bool Read([NotNullWhen(true)] out string? value);

	bool Read([NotNullWhen(true)] out string? value, Encoding encoding, bool nullTerminated = false);

	bool Read(out uint value);

	bool Read(out ulong value);

	bool Read(out ushort value);

	bool ReadBool()
	{
		if (!Read(out bool value))
		{
			return false;
		}
		return value;
	}

	bool ReadBoolean()
	{
		if (!Read(out bool value))
		{
			return false;
		}
		return value;
	}

	byte ReadByte()
	{
		if (!Read(out byte value))
		{
			return 0;
		}
		return value;
	}

	byte ReadUInt8()
	{
		if (!Read(out byte value))
		{
			return 0;
		}
		return value;
	}

	byte[] ReadBytes()
	{
		if (!Read(out byte[] value))
		{
			return null;
		}
		return value;
	}

	byte[] ReadBytes(long count)
	{
		if (!Read(out var value, count))
		{
			return null;
		}
		return value;
	}

	byte[] ReadBytes(ref byte[] bytes, long offset, long count)
	{
		if (!Read(ref bytes, offset, count))
		{
			return null;
		}
		return bytes;
	}

	char ReadChar()
	{
		if (!Read(out char value))
		{
			return '\0';
		}
		return value;
	}

	char ReadCharacter()
	{
		if (!Read(out char value))
		{
			return '\0';
		}
		return value;
	}

	decimal ReadDecimal()
	{
		if (!Read(out decimal value))
		{
			return 0m;
		}
		return value;
	}

	double ReadDouble()
	{
		if (!Read(out double value))
		{
			return 0.0;
		}
		return value;
	}

	float ReadFloat()
	{
		if (!Read(out float value))
		{
			return 0f;
		}
		return value;
	}

	float ReadSingle()
	{
		if (!Read(out float value))
		{
			return 0f;
		}
		return value;
	}

	int ReadInt()
	{
		if (!Read(out int value))
		{
			return 0;
		}
		return value;
	}

	int ReadInt32()
	{
		if (!Read(out int value))
		{
			return 0;
		}
		return value;
	}

	int ReadInteger()
	{
		if (!Read(out int value))
		{
			return 0;
		}
		return value;
	}

	long ReadInt64()
	{
		if (!Read(out long value))
		{
			return 0L;
		}
		return value;
	}

	long ReadLong()
	{
		if (!Read(out long value))
		{
			return 0L;
		}
		return value;
	}

	sbyte ReadInt8()
	{
		if (!Read(out sbyte value))
		{
			return 0;
		}
		return value;
	}

	sbyte ReadSByte()
	{
		if (!Read(out sbyte value))
		{
			return 0;
		}
		return value;
	}

	short ReadInt16()
	{
		if (!Read(out short value))
		{
			return 0;
		}
		return value;
	}

	short ReadShort()
	{
		if (!Read(out short value))
		{
			return 0;
		}
		return value;
	}

	string? ReadString()
	{
		if (!Read(out string value))
		{
			return null;
		}
		return value;
	}

	string? ReadString(Encoding encoding, bool nullTerminated = false)
	{
		if (!Read(out string value, encoding, nullTerminated))
		{
			return null;
		}
		return value;
	}

	uint ReadUInt()
	{
		if (!Read(out uint value))
		{
			return 0u;
		}
		return value;
	}

	uint ReadUInt32()
	{
		if (!Read(out uint value))
		{
			return 0u;
		}
		return value;
	}

	uint ReadUnsignedInteger()
	{
		if (!Read(out uint value))
		{
			return 0u;
		}
		return value;
	}

	ulong ReadULong()
	{
		if (!Read(out ulong value))
		{
			return 0uL;
		}
		return value;
	}

	ulong ReadUInt64()
	{
		if (!Read(out ulong value))
		{
			return 0uL;
		}
		return value;
	}

	ushort ReadUInt16()
	{
		if (!Read(out ushort value))
		{
			return 0;
		}
		return value;
	}

	ushort ReadUShort()
	{
		if (!Read(out ushort value))
		{
			return 0;
		}
		return value;
	}

	void Write(bool value);

	void Write(byte value);

	void Write(byte[] value);

	void Write(byte[] value, long count);

	void Write(byte[] value, long offset, long count);

	void Write(char value);

	void Write(decimal value);

	void Write(double value);

	void Write(float value);

	void Write(int value);

	void Write(long value);

	void Write(sbyte value);

	void Write(short value);

	void Write(string value);

	void Write(string value, Encoding encoding, bool nullTerminated = false);

	void Write(uint value);

	void Write(ulong value);

	void Write(ushort value);
}
