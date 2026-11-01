using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Intersect.Core;
using Intersect.Extensions;
using Microsoft.Extensions.Logging;

namespace Intersect.IO.Files;

public static class FileSystemHelper
{
	private static readonly string[] SizePrefixes = new string[7] { "", "K", "M", "G", "T", "P", "E" };

	internal static IFileSystem FileSystem { get; set; } = new FileSystem();

	public static string FormatSize(long bytes, bool binary = false)
	{
		double num = (binary ? 1024 : 1000);
		long num2 = Math.Max(0L, (long)Math.Floor(Math.Log(Math.Abs(Math.Max(-9223372036854775807L, bytes)), num)));
		string value = SizePrefixes[num2] + ((binary && num2 != 0L) ? "i" : "");
		double value2 = (double)bytes / Math.Max(1.0, Math.Pow(num, num2));
		return $"{value2:0.###}{value}B";
	}

	public static bool EnsureDirectoryExists(string directoryPath)
	{
		if (FileSystem.Directory.Exists(directoryPath))
		{
			return true;
		}
		if (File.Exists(directoryPath))
		{
			return false;
		}
		try
		{
			return FileSystem.Directory.CreateDirectory(directoryPath).Exists;
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Failed to create {DirectoryPath}", directoryPath);
			return false;
		}
	}

	public static string RelativePath(string from, string to)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(from, "from");
		ArgumentException.ThrowIfNullOrWhiteSpace(to, "to");
		string text = CleanPath(Path.GetFullPath(from));
		Uri uri = new Uri("file://" + text);
		string text2 = CleanPath(Path.GetFullPath(to));
		Uri uri2 = new Uri("file://" + text2);
		Uri uri3 = uri.MakeRelativeUri(uri2);
		return Uri.UnescapeDataString(uri3.ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	public static string CleanPath(string path)
	{
		string text = path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return text.TerminateWith(Path.AltDirectorySeparatorChar, !IsFile(text, doClean: false));
	}

	public static bool IsFile(string path)
	{
		return IsFile(path, doClean: true);
	}

	private static bool IsFile(string path, bool doClean)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("path");
		}
		string text = (doClean ? CleanPath(path) : path);
		if (text.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
		{
			return false;
		}
		if (Path.HasExtension(text))
		{
			return true;
		}
		try
		{
			FileAttributes attributes = FileSystem.File.GetAttributes(text);
			return !attributes.HasFlag(FileAttributes.Directory);
		}
		catch
		{
			return true;
		}
	}

	public static string WriteToTemporaryFolder(string name, Stream stream)
	{
		string text = Path.Combine(Path.GetTempPath(), (Assembly.GetEntryAssembly()?.GetName().Name ?? "Intersect Client"));
		if (!EnsureDirectoryExists(text))
		{
			throw new Exception();
		}
		string text2 = Path.Combine(text, name);
		byte[] array = new byte[4096];
		using FileStream fileStream = File.OpenWrite(text2);
		int count;
		while ((count = stream.Read(array, 0, array.Length)) != 0)
		{
			fileStream.Write(array, 0, count);
		}
		return text2;
	}
}
