using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class _003CPrivateImplementationDetails_003E
{
	internal static ref TElement InlineArrayElementRef<TBuffer, TElement>(ref TBuffer buffer, int index)
	{
		return ref Unsafe.Add(ref Unsafe.As<TBuffer, TElement>(ref buffer), index);
	}

	internal static Span<TElement> InlineArrayAsSpan<TBuffer, TElement>(ref TBuffer buffer, int length)
	{
		return MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TElement>(ref buffer), length);
	}

	internal static ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(in TBuffer buffer, int length)
	{
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, TElement>(ref Unsafe.AsRef(in buffer)), length);
	}
}
