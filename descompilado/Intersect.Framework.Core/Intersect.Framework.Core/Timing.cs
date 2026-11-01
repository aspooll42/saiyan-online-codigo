using System;
using System.Runtime.CompilerServices;

namespace Intersect.Framework.Core;

public sealed class Timing
{
	public static Timing Global { get; } = new Timing();

	public long Ticks
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return TicksUtc - TicksOffset;
		}
	}

	public long TicksOffset { get; private set; }

	public long TicksUtc
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return DateTime.UtcNow.Ticks;
		}
	}

	public long Milliseconds
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Ticks / 10000;
		}
	}

	public long MillisecondsOffset
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return TicksOffset / 10000;
		}
	}

	public long MillisecondsUtc
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return TicksUtc / 10000;
		}
	}

	public Timing()
	{
		TicksOffset = DateTime.UtcNow.Ticks;
	}

	public void Synchronize(long remoteUtc, long remoteOffset)
	{
		TicksOffset = remoteOffset + (TicksUtc - remoteUtc);
	}
}
