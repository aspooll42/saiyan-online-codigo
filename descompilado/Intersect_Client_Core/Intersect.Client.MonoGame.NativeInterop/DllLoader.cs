using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Intersect.Client.MonoGame.NativeInterop;

public abstract class DllLoader
{
	private sealed class WindowsDllLoader : DllLoader
	{
		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		private static extern nint GetProcAddress(nint hModule, string procName);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern nint LoadLibraryW(string lpszLib);

		public override nint LoadLibrary(string libraryName)
		{
			return LoadLibraryW(libraryName);
		}

		protected override nint LoadFunctionPointer(nint libraryHandle, string functionName)
		{
			return GetProcAddress(libraryHandle, functionName);
		}
	}

	private sealed class MacDllLoader : UnixDllLoader
	{
		public MacDllLoader()
			: base(dlopen, dlsym)
		{
		}

		[DllImport("/usr/lib/libSystem.dylib")]
		private static extern nint dlopen(string path, int flags);

		[DllImport("/usr/lib/libSystem.dylib")]
		private static extern nint dlsym(nint handle, string symbol);
	}

	private sealed class LinuxDllLoader : UnixDllLoader
	{
		public LinuxDllLoader()
			: base(dlopen, dlsym)
		{
		}

		[DllImport("libdl.so.2")]
		private static extern nint dlopen(string path, int flags);

		[DllImport("libdl.so.2")]
		private static extern nint dlsym(nint handle, string symbol);
	}

	private class UnixDllLoader : DllLoader
	{
		protected delegate nint dlopen_d(string path, int flags);

		protected delegate nint dlsym_d(nint handle, string symbol);

		protected const int RTLD_LAZY = 1;

		private readonly dlopen_d _dlopen;

		private readonly dlsym_d _dlsym;

		protected UnixDllLoader(dlopen_d dlopen, dlsym_d dlsym)
		{
			_dlopen = dlopen;
			_dlsym = dlsym;
		}

		public UnixDllLoader()
			: this(dlopen, dlsym)
		{
		}

		[DllImport("libdl")]
		private static extern nint dlopen(string path, int flags);

		[DllImport("libdl")]
		private static extern nint dlsym(nint handle, string symbol);

		public override nint LoadLibrary(string libraryName)
		{
			nint num = _dlopen(libraryName, 1);
			if (num == 0)
			{
				num = _dlopen(Path.Combine(Environment.CurrentDirectory, libraryName), 1);
			}
			return num;
		}

		protected override nint LoadFunctionPointer(nint libraryHandle, string functionName)
		{
			return _dlsym(libraryHandle, functionName);
		}
	}

	public static readonly DllLoader PlatformLoader;

	static DllLoader()
	{
		switch (PlatformHelper.CurrentPlatform)
		{
		case Platform.Unknown:
			PlatformLoader = new UnixDllLoader();
			break;
		case Platform.Linux:
			PlatformLoader = new LinuxDllLoader();
			break;
		case Platform.MacOS:
			PlatformLoader = new MacDllLoader();
			break;
		case Platform.Windows:
			PlatformLoader = new WindowsDllLoader();
			break;
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public abstract nint LoadLibrary(string libraryName);

	protected abstract nint LoadFunctionPointer(nint libraryHandle, string functionName);

	public virtual TDelegate LoadFunction<TDelegate>(nint libraryHandle, string functionName)
	{
		nint num = LoadFunctionPointer(libraryHandle, functionName);
		if (num != 0)
		{
			return Marshal.GetDelegateForFunctionPointer<TDelegate>(num);
		}
		return default(TDelegate);
	}
}
