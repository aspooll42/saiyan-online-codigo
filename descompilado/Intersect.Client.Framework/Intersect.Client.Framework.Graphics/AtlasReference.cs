using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Intersect.Client.Framework.GenericClasses;

namespace Intersect.Client.Framework.Graphics;

public record AtlasReference
{
	public Rectangle Bounds { get; init; }

	public bool IsRotated { get; init; }

	public Rectangle SourceBounds { get; init; }

	public IGameTexture Texture { get; init; }

	public string Name { get; }

	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<AtlasReference, string>> _atlasReferencesByFolderName = new ConcurrentDictionary<string, ConcurrentDictionary<AtlasReference, string>>();

	private static readonly ConcurrentDictionary<string, AtlasReference> _atlasReferences = new ConcurrentDictionary<string, AtlasReference>();

	public AtlasReference(string Name, Rectangle Bounds, bool IsRotated, Rectangle SourceBounds, IGameTexture Texture)
	{
		this.Bounds = Bounds;
		this.IsRotated = IsRotated;
		this.SourceBounds = SourceBounds;
		this.Texture = Texture;
		this.Name = Name.Replace('\\', '/');
	}

	public static void Add(AtlasReference atlasReference)
	{
		string text = atlasReference.Name.Replace('\\', '/');
		_atlasReferences[text] = atlasReference;
		_atlasReferences[text.ToLowerInvariant()] = atlasReference;
		string[] array = text.Split('/');
		if (array.Length < 3)
		{
			throw new InvalidOperationException("Expected 'resources/<folderName>/<assetFileName>' but got '" + text + "'");
		}
		string text2 = array[1];
		if (string.IsNullOrWhiteSpace(text2))
		{
			throw new InvalidOperationException("Invalid (empty/whitespace) segment in '" + text + "'");
		}
		string key = text2.ToLowerInvariant();
		ConcurrentDictionary<AtlasReference, string> orAdd = _atlasReferencesByFolderName.GetOrAdd(key, (string _) => new ConcurrentDictionary<AtlasReference, string>());
		orAdd.AddOrUpdate(atlasReference, text, AssetNameFrom);
	}

	private static string AssetNameFrom(AtlasReference atlasReference, string assetName)
	{
		return assetName;
	}

	public static AtlasReference[] GetAllFor(string folderName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(folderName, "folderName");
		string key = folderName.ToLowerInvariant();
		if (!_atlasReferencesByFolderName.TryGetValue(key, out ConcurrentDictionary<AtlasReference, string> value))
		{
			return Array.Empty<AtlasReference>();
		}
		return value.Keys.ToArray();
	}

	public static bool TryGet(string assetName, [NotNullWhen(true)] out AtlasReference? atlasReference)
	{
		string text = assetName.Replace('\\', '/');
		if (!_atlasReferences.TryGetValue(text, out atlasReference))
		{
			return _atlasReferences.TryGetValue(text.ToLowerInvariant(), out atlasReference);
		}
		return true;
	}

	[CompilerGenerated]
	protected AtlasReference(AtlasReference original)
	{
		Bounds = original.Bounds;
		IsRotated = original.IsRotated;
		SourceBounds = original.SourceBounds;
		Texture = original.Texture;
		Name = original.Name;
	}

	[CompilerGenerated]
	public void Deconstruct(out string Name, out Rectangle Bounds, out bool IsRotated, out Rectangle SourceBounds, out IGameTexture Texture)
	{
		Name = this.Name;
		Bounds = this.Bounds;
		IsRotated = this.IsRotated;
		SourceBounds = this.SourceBounds;
		Texture = this.Texture;
	}
}
