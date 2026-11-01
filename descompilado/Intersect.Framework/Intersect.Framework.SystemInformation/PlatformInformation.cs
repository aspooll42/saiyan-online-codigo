using System;
using System.Runtime.InteropServices;

namespace Intersect.Framework.SystemInformation;

public static class PlatformInformation
{
	private static string? _runtimeIdentifier;

	public static string RuntimeIdentifier
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_runtimeIdentifier))
			{
				return _runtimeIdentifier;
			}
			Architecture processArchitecture = RuntimeInformation.ProcessArchitecture;
			string text = processArchitecture switch
			{
				Architecture.X86 => "x86", 
				Architecture.X64 => "x64", 
				Architecture.Arm => "arm", 
				Architecture.Arm64 => "arm64", 
				Architecture.Wasm => "wasm", 
				_ => throw new PlatformNotSupportedException(processArchitecture.ToString()), 
			};
			if (OperatingSystem.IsLinux())
			{
				_runtimeIdentifier = "linux-" + text;
			}
			else if (OperatingSystem.IsWindows())
			{
				_runtimeIdentifier = "win-" + text;
			}
			else if (OperatingSystem.IsMacOS())
			{
				_runtimeIdentifier = "osx-" + text;
			}
			else if (OperatingSystem.IsAndroid())
			{
				_runtimeIdentifier = "android-" + text;
			}
			else if (OperatingSystem.IsIOS())
			{
				_runtimeIdentifier = "ios-" + text;
			}
			else
			{
				if (!OperatingSystem.IsBrowser())
				{
					throw new PlatformNotSupportedException(RuntimeInformation.RuntimeIdentifier);
				}
				_runtimeIdentifier = "browser-" + text;
			}
			return _runtimeIdentifier;
		}
	}
}
