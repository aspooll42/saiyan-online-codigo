using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Intersect.Client.MonoGame.NativeInterop;

public static class Sdl2
{
	private static class Loader
	{
		internal static readonly FunctionLoader Functions = new FunctionLoader(KnownLibrary.SDL2);
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* SDL_GetError_d();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate void SDL_free_d(void* ptr);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* SDL_GetClipboardText_d();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate int SDL_SetClipboardText_d(byte* text);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate nint SDL_GL_GetProcAddress_d(string proc);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate int SDL_GL_SetSwapInterval_d(int interval);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* SDL_GetHint_d(byte* name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate int SDL_SetHint_d(byte* name, byte* value);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate byte* SDL_getenv_d(byte* name);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate int SDL_setenv_d(byte* name, byte* value, int overwrite);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate int SDL_GetNumVideoDisplays_d();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private unsafe delegate int SDL_GetDisplayBounds_d(int displayIndex, SDL_Rect* rect);

	private static SDL_GetError_d SDL_GetError_f = Loader.Functions.LoadFunction<SDL_GetError_d>("SDL_GetError");

	private static SDL_free_d? SDL_free_f = Loader.Functions.LoadFunction<SDL_free_d>("SDL_free");

	private static SDL_GetClipboardText_d? SDL_GetClipboardText_f = Loader.Functions.LoadFunction<SDL_GetClipboardText_d>("SDL_GetClipboardText");

	private static SDL_SetClipboardText_d? SDL_SetClipboardText_f = Loader.Functions.LoadFunction<SDL_SetClipboardText_d>("SDL_SetClipboardText");

	private static SDL_GL_GetProcAddress_d SDL_GL_GetProcAddress_f = Loader.Functions.LoadFunction<SDL_GL_GetProcAddress_d>("SDL_GL_GetProcAddress");

	private static SDL_GL_SetSwapInterval_d SDL_GL_SetSwapInterval_f = Loader.Functions.LoadFunction<SDL_GL_SetSwapInterval_d>("SDL_GL_SetSwapInterval");

	public const string SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR = "SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR";

	private static SDL_GetHint_d SDL_GetHint_f = Loader.Functions.LoadFunction<SDL_GetHint_d>("SDL_GetHint");

	private static SDL_SetHint_d SDL_SetHint_f = Loader.Functions.LoadFunction<SDL_SetHint_d>("SDL_SetHint");

	private static SDL_getenv_d SDL_getenv_f = Loader.Functions.LoadFunction<SDL_getenv_d>("SDL_getenv");

	private static SDL_setenv_d SDL_setenv_f = Loader.Functions.LoadFunction<SDL_setenv_d>("SDL_setenv");

	public static SDL_GetNumVideoDisplays_d SDL_GetNumVideoDisplays = Loader.Functions.LoadFunction<SDL_GetNumVideoDisplays_d>("SDL_GetNumVideoDisplays");

	private static SDL_GetDisplayBounds_d SDL_GetDisplayBounds_f = Loader.Functions.LoadFunction<SDL_GetDisplayBounds_d>("SDL_GetDisplayBounds");

	public static bool IsClipboardSupported
	{
		get
		{
			if (SDL_GetClipboardText_f != null && SDL_SetClipboardText_f != null)
			{
				return SDL_free_f != null;
			}
			return false;
		}
	}

	public unsafe static string SDL_GetError()
	{
		if (SDL_GetError_f == null)
		{
			throw new PlatformNotSupportedException();
		}
		byte* ptr = SDL_GetError_f();
		byte* ptr2;
		for (ptr2 = ptr; *ptr2 != 0; ptr2++)
		{
		}
		return Encoding.UTF8.GetString(ptr, (int)(ptr2 - ptr));
	}

	private unsafe static void SDL_free(void* ptr)
	{
		SDL_free_f(ptr);
	}

	public unsafe static string SDL_GetClipboardText()
	{
		if (SDL_GetClipboardText_f == null || SDL_free_f == null)
		{
			throw new PlatformNotSupportedException();
		}
		byte* ptr = SDL_GetClipboardText_f();
		byte* ptr2;
		for (ptr2 = ptr; *ptr2 != 0; ptr2++)
		{
		}
		string result = Encoding.UTF8.GetString(ptr, (int)(ptr2 - ptr));
		SDL_free(ptr);
		return result;
	}

	public unsafe static bool SDL_SetClipboardText(string text)
	{
		if (SDL_SetClipboardText_f == null)
		{
			throw new PlatformNotSupportedException();
		}
		fixed (byte* bytes = Encoding.UTF8.GetBytes(text))
		{
			return SDL_SetClipboardText_f(bytes) == 0;
		}
	}

	public static nint SDL_GL_GetProcAddress(string proc)
	{
		return SDL_GL_GetProcAddress_f(proc);
	}

	public static int SDL_GL_SetSwapInterval(int interval)
	{
		return SDL_GL_SetSwapInterval_f(interval);
	}

	public unsafe static string? SDL_GetHint(string name)
	{
		fixed (byte* bytes = Encoding.UTF8.GetBytes(name))
		{
			byte* ptr = SDL_GetHint_f(bytes);
			if (ptr == null)
			{
				return null;
			}
			byte* ptr2;
			for (ptr2 = ptr; *ptr2 != 0; ptr2++)
			{
			}
			return Encoding.UTF8.GetString(ptr, (int)(ptr2 - ptr));
		}
	}

	public unsafe static bool SDL_SetHint(string name, string value)
	{
		fixed (byte* bytes = Encoding.UTF8.GetBytes(value))
		{
			fixed (byte* bytes2 = Encoding.UTF8.GetBytes(name))
			{
				return SDL_SetHint_f(bytes2, bytes) != 0;
			}
		}
	}

	public static bool SDL_SetHint(string name, bool value)
	{
		return SDL_SetHint(name, value ? "1" : "0");
	}

	public unsafe static string? SDL_getenv(string name)
	{
		fixed (byte* bytes = Encoding.UTF8.GetBytes(name))
		{
			byte* ptr = SDL_GetHint_f(bytes);
			if (ptr == null)
			{
				return null;
			}
			byte* ptr2;
			for (ptr2 = ptr; *ptr2 != 0; ptr2++)
			{
			}
			return Encoding.UTF8.GetString(ptr, (int)(ptr2 - ptr));
		}
	}

	public unsafe static bool SDL_setenv(string name, string value, bool overwrite)
	{
		fixed (byte* bytes = Encoding.UTF8.GetBytes(value))
		{
			fixed (byte* bytes2 = Encoding.UTF8.GetBytes(name))
			{
				return SDL_setenv_f(bytes2, bytes, overwrite ? 1 : 0) != 0;
			}
		}
	}

	public static SDL_Rect SDL_GetDisplayBounds(int displayIndex)
	{
		if (TryGetDisplayBounds(displayIndex, out var bounds))
		{
			return bounds;
		}
		throw new Exception(SDL_GetError());
	}

	public unsafe static bool TryGetDisplayBounds(int displayIndex, out SDL_Rect bounds)
	{
		fixed (SDL_Rect* rect = &bounds)
		{
			return SDL_GetDisplayBounds_f(displayIndex, rect) == 0;
		}
	}

	public static SDL_Rect[] GetDisplayBounds()
	{
		int num = SDL_GetNumVideoDisplays();
		if (num < 1)
		{
			throw new Exception(SDL_GetError());
		}
		SDL_Rect[] array = new SDL_Rect[num];
		for (int i = 0; i < num; i++)
		{
			if (!TryGetDisplayBounds(i, out array[i]))
			{
				throw new Exception(SDL_GetError());
			}
		}
		return array;
	}
}
