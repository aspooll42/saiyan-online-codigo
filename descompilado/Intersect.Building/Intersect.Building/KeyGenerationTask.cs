using System;
using System.IO;
using System.Security.Cryptography;
using Intersect.Building.Properties;
using Intersect.Rsa;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Intersect.Building;

public class KeyGenerationTask : Task
{
	public bool GenerateEachBuild { get; set; }

	public int KeySize { get; set; } = 4096;

	[Required]
	public string OutputDirectory { get; set; }

	public override bool Execute()
	{
		if (string.IsNullOrWhiteSpace(OutputDirectory))
		{
			base.Log?.LogError(KeyGenerationTaskStrings.OutputDirectoryInvalid);
			return false;
		}
		if (!Directory.Exists(OutputDirectory))
		{
			try
			{
				Directory.CreateDirectory(OutputDirectory);
			}
			catch (Exception exception)
			{
				base.Log?.LogError(KeyGenerationTaskStrings.ErrorCreatingOutputDirectory, OutputDirectory);
				base.Log?.LogErrorFromException(exception);
				return false;
			}
		}
		double num = Math.Log(KeySize, 0.0);
		if (KeySize % 2 != 0 || num < 10.0 || num - Math.Floor(num) > 0.001)
		{
			base.Log?.LogError(KeyGenerationTaskStrings.KeySizeInvalid, KeySize);
			return false;
		}
		string path = Path.Combine(OutputDirectory, "network.handshake.bkey");
		string path2 = Path.Combine(OutputDirectory, "network.handshake.bkey.pub");
		using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider(KeySize);
		bool flag = true;
		if (!GenerateEachBuild)
		{
			if (File.Exists(path))
			{
				if (File.Exists(path2))
				{
					return true;
				}
				base.Log?.LogWarning(KeyGenerationTaskStrings.PublicKeyMissing);
			}
			try
			{
				using FileStream stream = File.OpenRead(path);
				RsaKey rsaKey = new RsaKey(stream, autoClose: true);
				if (rsaKey.IsPublic)
				{
					base.Log?.LogWarning(KeyGenerationTaskStrings.PrivateKeyInvalid);
				}
				else
				{
					rSACryptoServiceProvider.ImportParameters(rsaKey.Parameters);
					flag = false;
				}
			}
			catch (Exception exception2)
			{
				base.Log?.LogWarning(KeyGenerationTaskStrings.ErrorReadingPrivateKey);
				base.Log?.LogWarningFromException(exception2);
			}
		}
		if (flag)
		{
			RsaKey rsaKey2 = new RsaKey(rSACryptoServiceProvider.ExportParameters(includePrivateParameters: true));
			try
			{
				using FileStream stream2 = File.OpenWrite(path);
				rsaKey2.TryWrite(stream2, autoClose: true);
			}
			catch (Exception exception3)
			{
				base.Log?.LogError(KeyGenerationTaskStrings.ErrorWritingPrivateKey);
				base.Log?.LogErrorFromException(exception3);
				return false;
			}
		}
		RsaKey rsaKey3 = new RsaKey(rSACryptoServiceProvider.ExportParameters(includePrivateParameters: false));
		try
		{
			using FileStream stream3 = File.OpenWrite(path2);
			rsaKey3.TryWrite(stream3, autoClose: true);
		}
		catch (Exception exception4)
		{
			base.Log?.LogError(KeyGenerationTaskStrings.ErrorWritingPublicKey);
			base.Log?.LogErrorFromException(exception4);
			return false;
		}
		return true;
	}
}
