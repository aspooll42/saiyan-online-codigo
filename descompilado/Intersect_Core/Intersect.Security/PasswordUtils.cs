using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Intersect.Security;

public class PasswordUtils
{
	[return: NotNull]
	public static string ComputePasswordHash(string? password)
	{
		return BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty))).Replace("-", string.Empty);
	}

	public static bool IsValidClientPasswordHash([NotNullWhen(true)] string? hashToValidate)
	{
		if (hashToValidate != null && hashToValidate.Length == 64)
		{
			return hashToValidate.All(char.IsAsciiHexDigit);
		}
		return false;
	}
}
