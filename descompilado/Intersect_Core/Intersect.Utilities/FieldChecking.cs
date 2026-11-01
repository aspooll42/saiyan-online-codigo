using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Intersect.Utilities;

public static class FieldChecking
{
	public const string PATTERN_EMAIL_ADDRESS = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$";

	public const string PATTERN_PASSWORD = "^[-_=\\+`~!@#\\$%\\^&\\*()\\[\\]{}\\\\|;\\:'\",<\\.>/\\?a-zA-Z0-9]{4,64}$";

	public const string PATTERN_USERNAME = "^[a-zA-Z0-9]{2,24}$";

	public const string PATTERN_GUILDNAME = "^[a-zA-Z0-9 ]{3,20}$";

	public static bool IsWellformedEmailAddress([NotNullWhen(true)] string? email, string emailRegex)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return false;
		}
		try
		{
			string text = emailRegex;
			if (string.IsNullOrEmpty(text))
			{
				text = "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$";
			}
			return Regex.IsMatch(email, text);
		}
		catch (ArgumentException)
		{
			return Regex.IsMatch(email, "^(([\\w-]+\\.)+[\\w-]+|([a-zA-Z]{1}|[\\w-]{2,}))@((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|([a-zA-Z]+[\\w-]+\\.)+[a-zA-Z]{2,4})$");
		}
	}

	public static bool IsValidUsername([NotNullWhen(true)] string? username, string usernameRegex)
	{
		if (string.IsNullOrWhiteSpace(username))
		{
			return false;
		}
		try
		{
			string text = usernameRegex;
			if (string.IsNullOrEmpty(text))
			{
				text = "^[a-zA-Z0-9]{2,24}$";
			}
			return Regex.IsMatch(username.Trim(), text);
		}
		catch (ArgumentException)
		{
			return Regex.IsMatch(username.Trim(), "^[a-zA-Z0-9]{2,24}$");
		}
	}

	public static bool IsValidPassword([NotNullWhen(true)] string? password, string passwordRegex)
	{
		if (string.IsNullOrWhiteSpace(password))
		{
			return false;
		}
		try
		{
			string text = passwordRegex;
			if (string.IsNullOrEmpty(text))
			{
				text = "^[-_=\\+`~!@#\\$%\\^&\\*()\\[\\]{}\\\\|;\\:'\",<\\.>/\\?a-zA-Z0-9]{4,64}$";
			}
			return Regex.IsMatch(password.Trim(), text);
		}
		catch (ArgumentException)
		{
			return Regex.IsMatch(password.Trim(), "^[-_=\\+`~!@#\\$%\\^&\\*()\\[\\]{}\\\\|;\\:'\",<\\.>/\\?a-zA-Z0-9]{4,64}$");
		}
	}

	public static bool IsValidGuildName(string guildName, string guildNameRegex)
	{
		if (guildName == null)
		{
			return false;
		}
		try
		{
			string text = guildNameRegex;
			if (string.IsNullOrEmpty(text))
			{
				text = "^[a-zA-Z0-9 ]{3,20}$";
			}
			return Regex.IsMatch(guildName.Trim(), text);
		}
		catch (ArgumentException)
		{
			return Regex.IsMatch(guildName.Trim(), "^[a-zA-Z0-9 ]{3,20}$");
		}
	}
}
