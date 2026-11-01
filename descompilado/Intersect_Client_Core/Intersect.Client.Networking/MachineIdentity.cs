using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Intersect.Client.Networking;

internal static class MachineIdentity
{
	private const string Salt = "SaiyanOnline:machine:v1:";

	private static string? _id;

	public static string Id => _id ?? (_id = Compute());

	private static string Compute()
	{
		string text = ReadSource();
		byte[] inArray = SHA256.HashData(Encoding.UTF8.GetBytes("SaiyanOnline:machine:v1:" + text));
		return Convert.ToHexString(inArray).ToLowerInvariant();
	}

	private static string ReadSource()
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
				using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
				if (registryKey2?.GetValue("MachineGuid") is string text && !string.IsNullOrWhiteSpace(text))
				{
					return "win:" + text.Trim().ToLowerInvariant();
				}
			}
			else
			{
				string[] array = new string[2] { "/etc/machine-id", "/var/lib/dbus/machine-id" };
				foreach (string path in array)
				{
					if (File.Exists(path))
					{
						string text2 = File.ReadAllText(path).Trim();
						if (text2 != null && text2.Length > 0)
						{
							return "unix:" + text2;
						}
					}
				}
			}
		}
		catch
		{
		}
		return "name:" + Environment.MachineName.ToLowerInvariant() + ":" + Environment.UserName.ToLowerInvariant();
	}
}
