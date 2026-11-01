using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Intersect.Client.Utilities;

public static class TextColorParser
{
	private static readonly Regex ColorTagRegex = new Regex("(\\\\c{[^}]*})", RegexOptions.Compiled);

	public static List<ColorizedText> Parse(string text, Color? defaultColor)
	{
		string[] array = (from s in ColorTagRegex.Split(text)
			where !string.IsNullOrWhiteSpace(s)
			select s).ToArray();
		List<ColorizedText> list = new List<ColorizedText>(array.Length);
		Color color = defaultColor;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("\\c{") && text2.EndsWith("}"))
			{
				string text3 = text2;
				string val = text3.Substring(3, text3.Length - 1 - 3).ToLowerInvariant();
				color = Color.FromString(val, defaultColor);
			}
			else
			{
				list.Add(new ColorizedText(text2, color));
			}
		}
		return list;
	}
}
