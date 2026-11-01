using System;
using System.IO;
using System.Security.Cryptography;

namespace Intersect.Rsa;

public class RsaKey
{
	private RSAParameters _parameters;

	public RSAParameters Parameters
	{
		get
		{
			return _parameters;
		}
		set
		{
			_parameters = value;
			try
			{
				using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
				rSACryptoServiceProvider.ImportParameters(Parameters);
				IsPublic = rSACryptoServiceProvider.PublicOnly;
			}
			catch
			{
				IsPublic = false;
			}
		}
	}

	public bool IsPublic { get; private set; }

	public RsaKey(RSAParameters parameters)
	{
		Parameters = parameters;
	}

	public RsaKey(Stream stream, bool autoClose = false)
	{
		using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		byte[] array = new byte[stream.Length];
		int num = 0;
		int num2;
		while ((num2 = stream.Read(array, num, array.Length - num)) > 0)
		{
			num += num2;
		}
		if (num != array.Length)
		{
			throw new InvalidOperationException();
		}
		rSACryptoServiceProvider.ImportCspBlob(array);
		Parameters = rSACryptoServiceProvider.ExportParameters(!rSACryptoServiceProvider.PublicOnly);
		if (autoClose)
		{
			stream.Close();
		}
	}

	public bool TryWrite(Stream stream, bool autoClose = false)
	{
		using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		rSACryptoServiceProvider.ImportParameters(Parameters);
		byte[] array = rSACryptoServiceProvider.ExportCspBlob(!rSACryptoServiceProvider.PublicOnly);
		stream.Write(array, 0, array.Length);
		if (autoClose)
		{
			stream.Close();
		}
		return true;
	}
}
