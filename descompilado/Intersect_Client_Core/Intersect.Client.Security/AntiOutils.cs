using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Intersect.Client.Framework.File_Management;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Security;

public static class AntiOutils
{
	private static readonly string[] OutilsSurveilles = new string[38]
	{
		"dnspy", "dnspy-x86", "dnspyex", "x64dbg", "x32dbg", "ollydbg", "windbg", "ida", "ida64", "idag",
		"idaq", "idaw", "de4dot", "ilspy", "dotpeek", "justdecompile", "reflector", "scyllahide", "megadumper", "extremedumper",
		"reclass", "reclass.net", "x64dbg.exe", "cheatengine", "cheatengine-x86_64", "cheatengine-i386", "artmoney", "squalr", "renderdoc", "renderdoccmd",
		"qrenderdoc", "ninjaripper", "texmod", "3dripper", "gameassassin", "nsight", "processhacker", "systeminformer"
	};

	private static volatile bool _dejaFerme;

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsDebuggerPresent();

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CheckRemoteDebuggerPresent(nint hProcess, ref bool isDebuggerPresent);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBoxW(nint hWnd, string texte, string titre, uint type);

	public static void Verifier()
	{
		if (_dejaFerme || !Coffre.Actif)
		{
			return;
		}
		try
		{
			string text = DebogueurPresent();
			if (text != null)
			{
				Fermer(text);
				return;
			}
			string text2 = OutilEnCours();
			if (text2 != null)
			{
				Fermer(text2);
			}
		}
		catch
		{
		}
	}

	private static string? DebogueurPresent()
	{
		if (Debugger.IsAttached)
		{
			return "débogueur attaché";
		}
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}
		if (IsDebuggerPresent())
		{
			return "débogueur présent";
		}
		bool isDebuggerPresent = false;
		if (CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent) && isDebuggerPresent)
		{
			return "débogueur distant";
		}
		return null;
	}

	private static string? OutilEnCours()
	{
		Process[] processes = Process.GetProcesses();
		foreach (Process process in processes)
		{
			string processName;
			try
			{
				processName = process.ProcessName;
			}
			catch
			{
				continue;
			}
			finally
			{
				process.Dispose();
			}
			string text = processName.ToLowerInvariant();
			string[] outilsSurveilles = OutilsSurveilles;
			foreach (string text2 in outilsSurveilles)
			{
				if (text == text2 || text == text2 + ".exe")
				{
					return "outil d'analyse : " + processName;
				}
			}
		}
		return null;
	}

	private static void Fermer(string raison)
	{
		if (_dejaFerme)
		{
			return;
		}
		_dejaFerme = true;
		try
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("Fermeture anti-analyse : {Raison}", raison);
		}
		catch
		{
		}
		try
		{
			if (OperatingSystem.IsWindows())
			{
				MessageBoxW(IntPtr.Zero, "Un outil d'analyse a été détecté. Pour protéger le jeu, Saiyan Online va se fermer.", "Saiyan Online", 16u);
			}
		}
		catch
		{
		}
		Environment.Exit(0);
	}
}
