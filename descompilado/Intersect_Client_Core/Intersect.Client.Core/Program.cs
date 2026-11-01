using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using Intersect.Client.ThirdParty;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Extensions;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.Core;

internal static class Program
{
	public static string OpenALLink { get; set; } = string.Empty;

	public static string OpenGLLink { get; set; } = string.Empty;

	[STAThread]
	internal static void Main(Assembly entryAssembly, string[] args)
	{
		bool flag = args.Contains("--debugger");
		while (flag && !Debugger.IsAttached)
		{
			System.Console.WriteLine("Waiting for debugger, sleeping 5000ms...");
			Thread.Sleep(5000);
		}
		CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
		if (OperatingSystem.IsMacOS())
		{
			Environment.CurrentDirectory = AppContext.BaseDirectory;
		}
		CarteGraphique.DemanderLaCartePuissante();
		ExportDependencies();
		Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "MonoGame.Framework.Client.dll"));
		try
		{
			Bootstrapper.Start(entryAssembly, args);
		}
		catch (NoSuitableGraphicsDeviceException exception)
		{
			try
			{
				string text = exception.AsFullStackString();
				Console.Error.WriteLine(text);
				File.WriteAllText("graphics-error.txt", text);
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
			}
			finally
			{
				if (!string.IsNullOrEmpty(OpenGLLink))
				{
					BrowserUtils.Open(OpenGLLink);
				}
				Environment.Exit(-1);
			}
		}
		catch (NoAudioHardwareException exception2)
		{
			try
			{
				string text2 = exception2.AsFullStackString();
				Console.Error.WriteLine(text2);
				File.WriteAllText("audio-error.txt", text2);
			}
			catch (Exception value2)
			{
				Console.Error.WriteLine(value2);
			}
			finally
			{
				if (!string.IsNullOrEmpty(OpenALLink))
				{
					BrowserUtils.Open(OpenALLink);
				}
				Environment.Exit(-1);
			}
		}
		catch (Exception exception3)
		{
			ClientContext.DispatchUnhandledException(exception3, isTerminating: true, wait: true);
		}
	}

	private static void ClearDlls()
	{
		DeleteIfExists("libopenal.so.1");
		DeleteIfExists("libSDL2-2.0.so.0");
		DeleteIfExists("SDL2.dll");
		DeleteIfExists("soft_oal.dll");
		DeleteIfExists("libopenal.1.dylib");
		DeleteIfExists("libSDL2-2.0.0.dylib");
		DeleteIfExists("openal32.dll");
		DeleteIfExists("MonoGame.Framework.Client.dll.config");
		DeleteIfExists("MonoGame.Framework.Client.dll");
		DeleteIfExists("libsdkencryptedappticket.dylib");
		DeleteIfExists("libsteam_api.dylib");
		DeleteIfExists("libsdkencryptedappticket.so");
		DeleteIfExists("libsteam_api.so");
		DeleteIfExists("sdkencryptedappticket64.dll");
		DeleteIfExists("steam_api64.dll");
	}

	private static string ReadProcessOutput(string name)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			FileName = name
		};
		try
		{
			using Process process = new Process
			{
				StartInfo = startInfo
			};
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return text.Trim();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Error reading process output from '{Name}'", name);
			return string.Empty;
		}
	}

	private static void ExportDependencies()
	{
		ClearDlls();
		OperatingSystem oSVersion = Environment.OSVersion;
		PlatformID platformID = oSVersion.Platform;
		if (platformID == PlatformID.Unix)
		{
			string text = ReadProcessOutput("uname");
			if (text != null && text.Contains("Darwin"))
			{
				platformID = PlatformID.MacOSX;
			}
		}
		if (!Environment.Is64BitProcess)
		{
			throw new PlatformNotSupportedException("x86 (32-bit) systems are not supported.");
		}
		switch (platformID)
		{
		case PlatformID.Win32S:
		case PlatformID.Win32Windows:
		case PlatformID.Win32NT:
		case PlatformID.WinCE:
			ExportDependency("SDL2.dll", "x64");
			ExportDependency("soft_oal.dll", "x64");
			if (Steam.SupportedAttribute.IsPresent(typeof(Program).Assembly))
			{
				ExportDependency("sdkencryptedappticket64.dll", "runtimes/win-x64/native");
				ExportDependency("steam_api64.dll", "runtimes/win-x64/native");
			}
			break;
		case PlatformID.MacOSX:
			ExportDependency("libopenal.1.dylib");
			ExportDependency("libSDL2.dylib");
			ExportDependency("openal32.dll");
			ExportDependency("MonoGame.Framework.dll.config", null, "MonoGame.Framework.Client.dll.config");
			if (Steam.SupportedAttribute.IsPresent(typeof(Program).Assembly))
			{
				ExportDependency("libsdkencryptedappticket.dylib", "runtimes/osx/native");
				ExportDependency("libsteam_api.dylib", "runtimes/osx/native");
			}
			break;
		default:
			ExportDependency("libopenal.so.1");
			ExportDependency("libSDL2-2.0.so.0");
			ExportDependency("openal32.dll");
			ExportDependency("MonoGame.Framework.dll.config", null, "MonoGame.Framework.Client.dll.config");
			if (Steam.SupportedAttribute.IsPresent(typeof(Program).Assembly))
			{
				ExportDependency("libsdkencryptedappticket.so", "runtimes/linux-x64/native");
				ExportDependency("libsteam_api.so", "runtimes/linux-x64/native");
			}
			break;
		case PlatformID.Xbox:
			break;
		}
		ExportDependency("MonoGame.Framework.dll", "", "MonoGame.Framework.Client.dll");
	}

	private static void ExportDependency(string filename, string? folder = null, string? nameoverride = null)
	{
		string text = (string.IsNullOrEmpty(nameoverride) ? filename : nameoverride);
		if (!DeleteIfExists(text))
		{
			return;
		}
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text2 = folder?.Trim().Replace('/', '.').Replace('\\', '.')
			.Replace('-', '_') ?? string.Empty;
		if (text2.Length > 0)
		{
			text2 += ".";
		}
		string text3 = "Intersect.Client.Resources." + text2 + filename;
		if (executingAssembly.GetManifestResourceNames().Contains(text3))
		{
			Console.WriteLine("Resource: " + text3);
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(text3);
			using FileStream fileStream = new FileStream(text, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			byte[] array = new byte[stream.Length];
			stream.Read(array, 0, (int)stream.Length);
			fileStream.Write(array, 0, array.Length);
			return;
		}
		ApplicationContext.Context.Value?.Logger.LogWarning("Was looking for '" + text3 + "' but only the following resources were found:\n" + string.Join("\n\t", executingAssembly.GetManifestResourceNames()));
		Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("Intersect Client.g.resources");
		ResourceSet resourceSet = new ResourceSet(manifestResourceStream);
		text = Path.Combine(ClientConfiguration.ResourcesDirectory, folder, filename.Split('.')[0].Split('-')[0]);
		text = text.ToLower();
		IDictionaryEnumerator enumerator = resourceSet.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Console.WriteLine(enumerator.Key);
			if (enumerator.Key == null || enumerator.Key.ToString().Trim() != text.Trim())
			{
				continue;
			}
			using FileStream fileStream2 = new FileStream(string.IsNullOrEmpty(nameoverride) ? filename : nameoverride, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			UnmanagedMemoryStream unmanagedMemoryStream = (UnmanagedMemoryStream)enumerator.Value;
			if (unmanagedMemoryStream != null)
			{
				byte[] buffer = new byte[unmanagedMemoryStream.Length];
				int count = unmanagedMemoryStream.Read(buffer, 0, (int)unmanagedMemoryStream.Length);
				fileStream2.Write(buffer, 0, count);
			}
		}
	}

	private static bool DeleteIfExists(string filename)
	{
		try
		{
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
