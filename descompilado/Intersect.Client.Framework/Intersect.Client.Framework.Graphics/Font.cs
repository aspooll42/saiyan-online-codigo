using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Intersect.Client.Framework.Graphics;

public abstract class Font<TPlatformObject> : IFont
{
	private readonly ICollection<int> _003CsupportedSizes_003EP;

	private readonly Dictionary<int, FontSizeRenderer<TPlatformObject>> _renderers;

	private readonly ImmutableSortedSet<int> _supportedSizes;

	public string Name { get; }

	public ICollection<int> SupportedSizes => _supportedSizes;

	protected Font(string fontName, ICollection<int> supportedSizes)
	{
		_003CsupportedSizes_003EP = supportedSizes;
		_renderers = new Dictionary<int, FontSizeRenderer<TPlatformObject>>();
		_supportedSizes = _003CsupportedSizes_003EP.ToImmutableSortedSet();
		Name = fontName;
	}

	public int PickBestMatchFor(int size)
	{
		if (_supportedSizes.Contains(size))
		{
			return size;
		}
		int num = 0;
		int num2 = int.MaxValue;
		foreach (int item in _003CsupportedSizes_003EP)
		{
			int num3 = Math.Abs(item - size);
			if (num3 <= num2 && (num3 != num2 || item >= num))
			{
				num = item;
				num2 = num3;
			}
		}
		if (num < 1)
		{
			throw new InvalidOperationException("No suitable font size found for " + Name);
		}
		return num;
	}

	public int GetNextFontSize(int startSize, int direction, int limit = 0)
	{
		int result = startSize;
		int num = 0;
		limit = Math.Abs(limit);
		foreach (int supportedSize in SupportedSizes)
		{
			int num2 = supportedSize - startSize;
			if (num2 == direction)
			{
				int num3 = Math.Abs(num2);
				if (num3 <= limit && num3 >= num)
				{
					result = supportedSize;
					num = num3;
				}
			}
		}
		return result;
	}

	public FontSizeRenderer<TPlatformObject> GetRendererFor(int size)
	{
		if (_renderers.TryGetValue(size, out FontSizeRenderer<TPlatformObject> value))
		{
			return value;
		}
		int num = PickBestMatchFor(size);
		if (_renderers.TryGetValue(num, out value))
		{
			_renderers[size] = value;
			return value;
		}
		value = CreateRendererFor(num);
		_renderers[size] = value;
		_renderers[num] = value;
		return value;
	}

	protected abstract FontSizeRenderer<TPlatformObject> CreateRendererFor(int size);

	public override string ToString()
	{
		return Name;
	}
}
