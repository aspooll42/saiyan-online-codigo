using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Intersect.Localization;

[Serializable]
public class LocalizedString(string value) : Localized()
{
	private readonly string _value = value;

	private static readonly Regex PatternArgument = new Regex("\\{(?<argumentIndex>\\d+)\\}", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

	[JsonIgnore]
	public int ArgumentCount { get; } = CountArguments(value);

	public static implicit operator LocalizedString(string value)
	{
		return new LocalizedString(value);
	}

	public static implicit operator string(LocalizedString localizedString)
	{
		return localizedString._value;
	}

	public override string ToString()
	{
		return value;
	}

	public string ToString(params object[] args)
	{
		try
		{
			return (args != null && args.Length == 0) ? value : string.Format(value, args ?? Array.Empty<object>());
		}
		catch (FormatException)
		{
			return "Format Exception!";
		}
	}

	public static int CountArguments(string formatString)
	{
		HashSet<int> hashSet = new HashSet<int>();
		MatchCollection matchCollection = PatternArgument.Matches(formatString);
		foreach (Match item in matchCollection)
		{
			if (item.Success)
			{
				string value = item.Groups["argumentIndex"].Value;
				if (int.TryParse(value, out var result))
				{
					hashSet.Add(result);
				}
			}
		}
		return hashSet.Count;
	}
}
