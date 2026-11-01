using System;
using System.IO;
using System.Runtime.InteropServices;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Intersect.Client.Core;

internal static class CarteGraphique
{
	private const string CleDesPreferences = "Software\\Microsoft\\DirectX\\UserGpuPreferences";

	private const string HautesPerformances = "GpuPreference=2;";

	private static string _preference = "non demandee";

	private const uint GlVendor = 7936u;

	private const uint GlRenderer = 7937u;

	private const uint GlVersion = 7938u;

	public static void DemanderLaCartePuissante()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}
		string processPath = Environment.ProcessPath;
		if (processPath == null || processPath.Length <= 0)
		{
			return;
		}
		if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
		{
			_preference = "lance par dotnet, rien demande";
			return;
		}
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\DirectX\\UserGpuPreferences", writable: true);
			if (registryKey.GetValue(processPath) is string text)
			{
				_preference = "choix du joueur garde (" + text.Trim() + ")";
				return;
			}
			registryKey.SetValue(processPath, "GpuPreference=2;", RegistryValueKind.String);
			_preference = "hautes performances demandees a Windows";
		}
		catch (Exception ex)
		{
			_preference = "impossible a demander (" + ex.GetType().Name + ")";
		}
	}

	[DllImport("opengl32.dll", EntryPoint = "glGetString")]
	private static extern nint GlGetString(uint nom);

	public static void Noter()
	{
		string text = "?";
		if (OperatingSystem.IsWindows())
		{
			try
			{
				text = $"{Lire(7937u)} ({Lire(7936u)}), OpenGL {Lire(7938u)}";
			}
			catch (Exception ex)
			{
				text = "illisible (" + ex.GetType().Name + ")";
			}
		}
		ApplicationContext.Context.Value?.Logger.LogInformation("Carte graphique : {Carte} ; preference de Windows : {Preference} ; synchro : {Synchro}", text, _preference, MonoRenderer.Synchro);
	}

	private static string Lire(uint nom)
	{
		return Marshal.PtrToStringAnsi(GlGetString(nom)) ?? "?";
	}
}
