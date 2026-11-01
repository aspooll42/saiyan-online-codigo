using System;

namespace Intersect.Utilities;

public static class TextUtils
{
	public static string None { get; set; }

	static TextUtils()
	{
		None = "None";
	}

	public static string StripToLower(string source)
	{
		return source?.ToLowerInvariant().Replace(" ", "");
	}

	public static bool IsNone(string str)
	{
		if (string.IsNullOrEmpty(str?.Trim()))
		{
			return true;
		}
		if (!string.Equals("None", StripToLower(str), StringComparison.InvariantCultureIgnoreCase))
		{
			return string.Equals(None, StripToLower(str), StringComparison.InvariantCultureIgnoreCase);
		}
		return true;
	}

	public static string NullToNone(string nullableString)
	{
		if (!IsNone(nullableString))
		{
			return nullableString;
		}
		return None;
	}

	public static string SanitizeNone(string nullableString)
	{
		if (!IsNone(nullableString))
		{
			return nullableString;
		}
		return null;
	}
}
