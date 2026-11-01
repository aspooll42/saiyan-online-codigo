using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Intersect.Framework.SystemInformation;

namespace Intersect.Client.MonoGame.NativeInterop.OpenGL;

public static class GL
{
	private struct ATI_meminfo_Tuple
	{
		public int FreeInPool;

		public int LargestFreeBlockInPool;

		public int FreeInAuxiliaryPool;

		public int LargestFreeBlockInAuxiliaryPool;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate GLenum glGetError_d();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate void glGetIntegerv_d(GLenum property, int* value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* glGetString_d(GLenum name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* glGetStringi_d(GLenum name, uint index);

	private sealed class AMDStatisticsProvider : IGPUStatisticsProvider
	{
		public long? AvailableMemory => (long)glGetATIMemInfo(GLenum.TEXTURE_FREE_MEMORY_ATI).FreeInPool * 1000L;

		public long? TotalMemory
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return -1L;
			}
		}
	}

	private sealed class NvidiaStatisticsProvider : IGPUStatisticsProvider
	{
		public long? AvailableMemory => glGetNVXGPUMemoryInfo(GLenum.GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX) * 1000;

		public long? TotalMemory => glGetNVXGPUMemoryInfo(GLenum.GPU_MEMORY_INFO_TOTAL_AVAILABLE_MEMORY_NVX) * 1000;
	}

	private static readonly glGetError_d glGetError_f = LoadFunction<glGetError_d>("glGetError");

	private static HashSet<string>? _cachedExtensions;

	private static readonly glGetIntegerv_d glGetIntegerv_f = LoadFunction<glGetIntegerv_d>("glGetIntegerv");

	private static readonly glGetString_d glGetString_f = LoadFunction<glGetString_d>("glGetString");

	private static readonly glGetStringi_d glGetStringi_f = LoadFunction<glGetStringi_d>("glGetStringi");

	private static bool _gpuStatisticsProviderCreated;

	private static IGPUStatisticsProvider? _gpuStatisticsProvider;

	private static bool IsATI_meminfoSupported => IsExtensionSupported("GL_ATI_meminfo");

	public static bool IsNVX_gpu_memory_infoSupported => IsExtensionSupported("GL_NVX_gpu_memory_info");

	private unsafe static ATI_meminfo_Tuple glGetATIMemInfo(GLenum name)
	{
		ATI_meminfo_Tuple result = default(ATI_meminfo_Tuple);
		int* value = &result.FreeInPool;
		glGetIntegerv_f(name, value);
		return result;
	}

	private static TDelegate LoadFunction<TDelegate>(string function)
	{
		return LoadFunction<TDelegate>(function, throwIfNotFound: false);
	}

	private static T? LoadFunction<T>(string function, bool throwIfNotFound)
	{
		nint num = Sdl2.SDL_GL_GetProcAddress(function);
		if (num != IntPtr.Zero)
		{
			return Marshal.GetDelegateForFunctionPointer<T>(num);
		}
		if (throwIfNotFound)
		{
			throw new EntryPointNotFoundException(function);
		}
		return default(T);
	}

	public static GLenum glGetError()
	{
		return glGetError_f();
	}

	public static HashSet<string> glGetExtensions()
	{
		HashSet<string> cachedExtensions = _cachedExtensions;
		if (cachedExtensions == null || cachedExtensions.Count <= 0)
		{
			int count = glGetIntegerv(GLenum.GL_NUM_EXTENSIONS);
			string[] source = glGetStrings(GLenum.GL_EXTENSIONS, count);
			_cachedExtensions = source.OfType<string>().ToHashSet();
		}
		return _cachedExtensions;
	}

	public static bool IsExtensionSupported(string extensionName)
	{
		return glGetExtensions().Contains(extensionName);
	}

	public unsafe static int glGetIntegerv(GLenum property)
	{
		int result = default(int);
		glGetIntegerv_f(property, &result);
		return result;
	}

	public unsafe static int[] glGetIntegerv(GLenum property, uint count)
	{
		int[] array = new int[count];
		fixed (int* value = array)
		{
			glGetIntegerv_f(property, value);
		}
		return array;
	}

	public unsafe static string? glGetString(GLenum name)
	{
		byte* ptr = glGetString_f(name);
		if (ptr != null)
		{
			return PointerToUTF8(ptr);
		}
		GLenum gLenum = glGetError();
		if (gLenum == GLenum.GL_INVALID_ENUM)
		{
			throw new ArgumentException($"Invalid glGetString() name '{name}'", "name");
		}
		throw new InvalidOperationException($"Unexpected error {gLenum} when executing glGetString({name})");
	}

	public unsafe static string? glGetStringi(GLenum name, uint index)
	{
		byte* ptr = glGetStringi_f(name, index);
		if (ptr != null)
		{
			return PointerToUTF8(ptr);
		}
		GLenum gLenum = glGetError();
		switch (gLenum)
		{
		case GLenum.GL_INVALID_ENUM:
			throw new ArgumentException($"Invalid glGetString() name '{name}'", "name");
		case GLenum.GL_INVALID_VALUE:
			throw new ArgumentOutOfRangeException("index", index, $"Index out of range for glGetString enum {name}");
		default:
			throw new InvalidOperationException($"Unexpected error {gLenum} when executing glGetStringi({name}, {index})");
		}
	}

	public static string?[] glGetStrings(GLenum name, int count)
	{
		string[] array = new string[count];
		for (uint num = 0u; num < count; num++)
		{
			array[num] = glGetStringi(name, num);
		}
		return array;
	}

	private unsafe static string? PointerToUTF8(byte* ptr)
	{
		if (ptr == null)
		{
			return null;
		}
		byte* ptr2;
		for (ptr2 = ptr; *ptr2 != 0; ptr2++)
		{
		}
		int byteCount = (int)(ptr2 - ptr);
		return Encoding.UTF8.GetString(ptr, byteCount);
	}

	public static IGPUStatisticsProvider? CreateGPUStatisticsProvider()
	{
		if (_gpuStatisticsProviderCreated)
		{
			return _gpuStatisticsProvider;
		}
		_gpuStatisticsProviderCreated = true;
		if (IsATI_meminfoSupported)
		{
			_gpuStatisticsProvider = new AMDStatisticsProvider();
		}
		else if (IsNVX_gpu_memory_infoSupported)
		{
			_gpuStatisticsProvider = new NvidiaStatisticsProvider();
		}
		return _gpuStatisticsProvider;
	}

	private static long glGetNVXGPUMemoryInfo(GLenum name)
	{
		return glGetIntegerv(name);
	}
}
