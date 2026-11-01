using System;
using System.Globalization;

namespace Intersect.Extensions;

public static class StringExtensions
{
	public static string Format(this string format, params object[] args)
	{
		return format.Format(CultureInfo.CurrentUICulture, args);
	}

	public static string Format(this string format, IFormatProvider formatProvider, params object[] args)
	{
		return string.Format(formatProvider, format, args);
	}

	public static string TerminateWith(this string self, char terminateWith, bool condition = true)
	{
		return self.TerminateWith(new char[1] { terminateWith }, condition);
	}

	public static string TerminateWith(this string self, char[] terminateWith, bool condition = true)
	{
		return self.TerminateWith(new string(terminateWith), condition);
	}

	public static string TerminateWith(this string self, string terminateWith, bool condition = true)
	{
		if (!condition)
		{
			return self;
		}
		if (string.IsNullOrWhiteSpace(self))
		{
			return terminateWith;
		}
		if (self.EndsWith(terminateWith))
		{
			return self;
		}
		return self + terminateWith;
	}

	public static string Slice(this string self, in int offset)
	{
		return self.Slice(in offset, -1);
	}

	public static string Slice(this string self, in int offset, in int length)
	{
		if (self == null)
		{
			throw new ArgumentNullException("self");
		}
		if (offset >= self.Length || offset <= -self.Length || length == 0)
		{
			return string.Empty;
		}
		int num = (self.Length + offset) % self.Length;
		int length2 = ((length < 0) ? (self.Length - num) : length);
		return self.Substring(num, length2);
	}
}
