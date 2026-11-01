using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Intersect.Client.MonoGame.Graphics;

public class MonoFont : Font<SpriteFont>, IFont
{
	private sealed class FontReference
	{
		private SpriteFont? _instance;

		public readonly Func<SpriteFont> PlatformObjectFactory;

		public string FontName { get; }

		public int Size { get; }

		public SpriteFont Instance
		{
			get
			{
				try
				{
					if (_instance == null)
					{
						_instance = PlatformObjectFactory();
						_instance.Texture.Name = $"{FontName} @ {Size}pt";
					}
					return _instance;
				}
				catch (Exception innerException)
				{
					throw new ContentLoadException($"Failed to load {Size}pt source for '{FontName}'", innerException);
				}
			}
		}

		public FontReference(string fontName, int size, Func<SpriteFont> platformObjectFactory)
		{
			FontName = fontName;
			PlatformObjectFactory = platformObjectFactory;
			Size = size;
		}
	}

	private readonly SortedDictionary<int, FontReference> _fontsBySize = new SortedDictionary<int, FontReference>();

	public MonoFont(string fontName, IDictionary<int, FileInfo> fontSourcesBySize, ContentManager contentManager)
		: base(fontName, fontSourcesBySize.Keys)
	{
		foreach (KeyValuePair<int, FileInfo> item in fontSourcesBySize)
		{
			var (num2, fileInfo2) = item;
			_fontsBySize[num2] = new FontReference(fontName, num2, delegate
			{
				string assetName = GameContentManager.RemoveExtension(fileInfo2.FullName);
				return contentManager.Load<SpriteFont>(assetName);
			});
		}
	}

	protected override FontSizeRenderer<SpriteFont> CreateRendererFor(int size)
	{
		if (!_fontsBySize.TryGetValue(size, out FontReference value))
		{
			throw new UnreachableException($"Missing reference for size {size} of '{base.Name}'");
		}
		SpriteFont instance = value.Instance;
		return new SpriteFontRenderer(instance);
	}
}
