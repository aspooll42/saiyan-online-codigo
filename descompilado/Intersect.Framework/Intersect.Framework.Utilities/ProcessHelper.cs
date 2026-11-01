using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Intersect.Framework.Utilities;

public static class ProcessHelper
{
	public static bool TryLaunchWithArgs(string executable, params string[] args)
	{
		string text = string.Join(' ', args.Where((string arg) => arg == "--restarting").Take(args.Length - 1).Append("--restarting"));
		try
		{
			Console.WriteLine($"Launching '{executable}' with arguments '{text}' in {Environment.CurrentDirectory}");
			Process.Start(executable, text);
			Environment.Exit(0);
			return true;
		}
		catch (Exception ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
			{
				if (ex2 != ex)
				{
					stringBuilder.Append("Caused by ");
				}
				stringBuilder.AppendLine(ex.Message);
				string stackTrace = ex.StackTrace;
				if (string.IsNullOrEmpty(stackTrace))
				{
					stringBuilder.AppendLine(stackTrace);
				}
			}
			Console.Error.WriteLine(stringBuilder);
			return false;
		}
	}

	public static bool TryRelaunch()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string[] args = commandLineArgs.Skip(1).ToArray();
		return TryRelaunchWithArgs(args);
	}

	public static bool TryRelaunchWithArgs(params string[] args)
	{
		if (!TryGetPathToEntryAssembly(out string pathToEntryAssembly))
		{
			Console.WriteLine("No path to entry assembly");
			return false;
		}
		return TryLaunchWithArgs(pathToEntryAssembly, args);
	}

	public static bool TryGetPathToEntryAssembly([NotNullWhen(true)] out string? pathToEntryAssembly)
	{
		pathToEntryAssembly = Assembly.GetEntryAssembly()?.Location;
		if (string.IsNullOrWhiteSpace(pathToEntryAssembly))
		{
			pathToEntryAssembly = Environment.GetCommandLineArgs().FirstOrDefault();
		}
		return !string.IsNullOrWhiteSpace(pathToEntryAssembly);
	}

	public static bool TryReplaceTargetWithEntryAssembly(string pathToTarget)
	{
		return TryReplaceTargetWithEntryAssembly(new FileInfo(pathToTarget));
	}

	public static bool TryReplaceTargetWithEntryAssembly(FileInfo targetFileInfo)
	{
		if (!TryGetPathToEntryAssembly(out string pathToEntryAssembly))
		{
			Console.Error.WriteLine("Failed to get path to entry assembly");
			return false;
		}
		FileInfo fileInfo = new FileInfo(pathToEntryAssembly);
		try
		{
			fileInfo.CopyTo(targetFileInfo.FullName, overwrite: true);
			return true;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Failed to copy \"{fileInfo.FullName}\" to \"{targetFileInfo.FullName}\"");
			Console.Error.WriteLine(ex.Message);
			return false;
		}
	}
}
