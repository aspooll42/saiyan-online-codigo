using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Intersect.Framework;
using Intersect.Localization;
using MessagePack;

namespace Intersect;

[MessagePackObject(false)]
public class Color : IEquatable<Color>
{
	public enum ChatColor
	{
		Black,
		White,
		Blue,
		Red,
		Green,
		Yellow,
		Orange,
		Purple,
		Gray,
		Cyan,
		Pink
	}

	private static readonly Dictionary<string, Func<Color>> KnownColors;

	private const float RANGE__2_X = 510f;

	[Key(0)]
	public byte A { get; set; }

	[Key(1)]
	public byte R { get; set; }

	[Key(2)]
	public byte G { get; set; }

	[Key(3)]
	public byte B { get; set; }

	public static Color Transparent => new Color(0, 0, 0, 0);

	public static Color White => new Color(255, 255, 255);

	public static Color Black => new Color(0, 0, 0);

	public static Color Red => new Color(255, 0, 0);

	public static Color Green => new Color(0, 255, 0);

	public static Color Blue => new Color(0, 0, 255);

	public static Color Yellow => new Color(255, 255, 0);

	public static Color LightCoral => new Color(240, 128, 128);

	public static Color ForestGreen => new Color(34, 139, 34);

	public static Color Magenta => new Color(255, 0, 255);

	public static Color OrangeRed => new Color(255, 69, 0);

	public static Color Orange => new Color(255, 165, 0);

	public static Color Gray => new Color(128, 128, 128);

	public static Color Cyan => new Color(0, 255, 255);

	public static Color Pink => new Color(255, 192, 203);

	static Color()
	{
		KnownColors = (from p in typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public)
			where p.PropertyType == typeof(Color) && p.GetMethod != null
			select p).ToDictionary((Func<PropertyInfo, string>)((PropertyInfo p) => p.Name.ToLowerInvariant()), (Func<PropertyInfo, Func<Color>>)((PropertyInfo p) => () => (p.GetMethod?.Invoke(null, null) as Color) ?? White));
	}

	public Color()
		: this(0)
	{
	}

	public Color(int argb)
		: this((argb >> 24) & 0xFF, (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF)
	{
	}

	public Color(int r, int g, int b)
		: this(255, r, g, b)
	{
	}

	public Color(int a, int r, int g, int b)
	{
		A = (byte)a;
		R = (byte)r;
		G = (byte)g;
		B = (byte)b;
	}

	public Color(in Color color)
		: this(in color, (int)(color?.A ?? 0))
	{
	}

	public Color(in Color color, in int alpha)
	{
		A = (byte)alpha;
		R = color?.R ?? 0;
		G = color?.G ?? 0;
		B = color?.B ?? 0;
	}

	public static Color FromName(string name, IDictionary<int, LocalizedString> colors)
	{
		if (name == "Black" || name == colors[0])
		{
			return Black;
		}
		if (name == "White" || name == colors[1])
		{
			return White;
		}
		if (name == "Pink" || name == colors[2])
		{
			return Pink;
		}
		if (name == "Blue" || name == colors[3])
		{
			return Blue;
		}
		if (name == "Red" || name == colors[4])
		{
			return Red;
		}
		if (name == "Green" || name == colors[5])
		{
			return Green;
		}
		if (name == "Yellow" || name == colors[6])
		{
			return Yellow;
		}
		if (name == "Orange" || name == colors[7])
		{
			return Orange;
		}
		if (name == "Purple" || name == colors[8])
		{
			return Magenta;
		}
		if (name == "Gray" || name == colors[9])
		{
			return Gray;
		}
		if (name == "Cyan" || name == colors[10])
		{
			return Cyan;
		}
		return White;
	}

	public static Color FromArgb(int a, int r, int g, int b)
	{
		return new Color(a, r, g, b);
	}

	public static Color FromArgb(int r, int g, int b)
	{
		return new Color(255, r, g, b);
	}

	public int ToArgb()
	{
		return (A << 24) + (R << 16) + (G << 8) + B;
	}

	public int ToRgba()
	{
		return (R << 24) + (G << 16) + (B << 8) + A;
	}

	public override string ToString()
	{
		return $"{A},{R},{G},{B}";
	}

	public static Color FromArgb(int argb)
	{
		return FromArgb((argb >> 24) & 0xFF, (argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF);
	}

	public static Color FromRgba(int rgba)
	{
		return FromArgb(rgba & 0xFF, (rgba >> 24) & 0xFF, (rgba >> 16) & 0xFF, (rgba >> 8) & 0xFF);
	}

	public static string ToString(Color clr)
	{
		return clr?.ToString() ?? string.Empty;
	}

	[return: NotNullIfNotNull("defaultColor")]
	public static Color? FromHex(string hexString, Color? defaultColor = null)
	{
		if (string.IsNullOrWhiteSpace(hexString))
		{
			return defaultColor;
		}
		hexString = hexString.Trim();
		if (hexString.StartsWith('#'))
		{
			string text = hexString;
			hexString = text.Substring(1, text.Length - 1);
		}
		string text2 = hexString.Reverse();
		if (!uint.TryParse(text2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return defaultColor;
		}
		int num2;
		int num3;
		int num4;
		int num;
		if (text2.Length <= 4)
		{
			num = (int)((result >> 12) & 0xF);
			num2 = (int)((result >> 8) & 0xF);
			num3 = (int)((result >> 4) & 0xF);
			num4 = (int)(result & 0xF);
			num |= num << 4;
			num2 |= num2 << 4;
			num3 |= num3 << 4;
			num4 |= num4 << 4;
		}
		else
		{
			num = (int)(((result >> 28) & 0xF) | (((result >> 24) & 0xF) << 4));
			num2 = (int)(((result >> 20) & 0xF) | (((result >> 16) & 0xF) << 4));
			num3 = (int)(((result >> 12) & 0xF) | (((result >> 8) & 0xF) << 4));
			num4 = (int)(((result >> 4) & 0xF) | ((result & 0xF) << 4));
		}
		int length = text2.Length;
		if (length > 4)
		{
			if (length < 8)
			{
				goto IL_00f3;
			}
		}
		else if (length < 4)
		{
			goto IL_00f3;
		}
		int num5 = num;
		goto IL_00ff;
		IL_00ff:
		num = num5;
		return new Color(num, num4, num3, num2);
		IL_00f3:
		num5 = 255;
		goto IL_00ff;
	}

	public static Color? FromCsv(string val, Color? defaultColor = null)
	{
		if (string.IsNullOrEmpty(val))
		{
			return defaultColor;
		}
		string[] array = val.Split(",".ToCharArray());
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = int.Parse(array[i]);
		}
		return new Color(array2[0], array2[1], array2[2], array2[3]);
	}

	public static Color? FromString(string val, Color? defaultColor = null)
	{
		if (string.IsNullOrWhiteSpace(val))
		{
			return defaultColor;
		}
		val = val.Trim().ToLowerInvariant();
		if (KnownColors.TryGetValue(val, out var value))
		{
			return value();
		}
		Color color = FromHex(val);
		if ((object)color == null)
		{
			color = FromCsv(val);
		}
		return color ?? defaultColor;
	}

	public static implicit operator Color(string colorString)
	{
		return FromString(colorString);
	}

	public static Color operator *(Color left, Color right)
	{
		return new Color((int)((float)(left.A * right.A) / 255f), (int)((float)(left.R * right.R) / 255f), (int)((float)(left.G * right.G) / 255f), (int)((float)(left.B * right.B) / 255f));
	}

	public static bool operator ==(Color left, Color right)
	{
		return left?.ToArgb() == right?.ToArgb();
	}

	public static bool operator !=(Color left, Color right)
	{
		return left?.ToArgb() != right?.ToArgb();
	}

	public override bool Equals(object obj)
	{
		if (obj is Color other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Color other)
	{
		return ToArgb() == other?.ToArgb();
	}
}
