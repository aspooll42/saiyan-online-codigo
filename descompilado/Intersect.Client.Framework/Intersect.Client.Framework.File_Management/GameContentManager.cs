using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Intersect.Async;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Compression;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Plugins;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.File_Management;

public abstract class GameContentManager : IContentManager
{
	public enum UI
	{
		Menu,
		InGame,
		Shared,
		Debug
	}

	protected readonly Dictionary<string, IAsset> mAnimationDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mEntityDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mFaceDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mFogDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IFont> mFontDict = new Dictionary<string, IFont>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mGuiDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mImageDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mItemDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mMiscDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mMusicDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mPaperdollDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mResourceDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, GameShader> mShaderDict = new Dictionary<string, GameShader>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mSoundDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<KeyValuePair<UI, string>, string> mUiDict = new Dictionary<KeyValuePair<UI, string>, string>();

	protected readonly Dictionary<string, IAsset> mSpellDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	protected readonly Dictionary<string, IAsset> mTexturePackDict = new Dictionary<string, IAsset>();

	protected readonly Dictionary<string, IAsset> mTilesetDict = new Dictionary<string, IAsset>(StringComparer.OrdinalIgnoreCase);

	private static GameContentManager? _current;

	private static readonly char[] DirectorySeparators = new char[2]
	{
		Path.DirectorySeparatorChar,
		Path.AltDirectorySeparatorChar
	};

	public static GameContentManager Current
	{
		get
		{
			return _current ?? throw new InvalidOperationException("Content manager not initialized");
		}
		private set
		{
			_current = value;
		}
	}

	public Dictionary<ContentType, ICollection<IAsset>> Content => Textures;

	public Dictionary<ContentType, ICollection<IAsset>> Textures => new Dictionary<ContentType, ICollection<IAsset>>
	{
		{
			ContentType.Animation,
			mAnimationDict.Values
		},
		{
			ContentType.Entity,
			mEntityDict.Values
		},
		{
			ContentType.Face,
			mFaceDict.Values
		},
		{
			ContentType.Fog,
			mFogDict.Values
		},
		{
			ContentType.Image,
			mImageDict.Values
		},
		{
			ContentType.Interface,
			mGuiDict.Values
		},
		{
			ContentType.Item,
			mItemDict.Values
		},
		{
			ContentType.Miscellaneous,
			mMiscDict.Values
		},
		{
			ContentType.Paperdoll,
			mPaperdollDict.Values
		},
		{
			ContentType.Resource,
			mResourceDict.Values
		},
		{
			ContentType.Spell,
			mSpellDict.Values
		},
		{
			ContentType.Tileset,
			mTilesetDict.Values
		}
	};

	public IAssetPacker SoundPacks { get; set; }

	public IAssetPacker MusicPacks { get; set; }

	public bool TilesetsLoaded { get; set; }

	public ContentWatcher ContentWatcher { get; protected set; }

	protected GameContentManager()
	{
		Current = this;
	}

	public void LoadAll()
	{
		LoadTexturePacks();
		LoadEntities();
		LoadItems();
		LoadAnimations();
		LoadSpells();
		LoadFaces();
		LoadImages();
		LoadFogs();
		LoadResources();
		LoadPaperdolls();
		LoadMisc();
		LoadGui();
		LoadFonts();
		LoadShaders();
	}

	public abstract void LoadTexturePacks();

	public abstract void LoadTilesets(string[] tilesetnames);

	public abstract void LoadItems();

	public abstract void LoadEntities();

	public abstract void LoadSpells();

	public abstract void LoadAnimations();

	public abstract void LoadFaces();

	public abstract void LoadImages();

	public abstract void LoadFogs();

	public abstract void LoadResources();

	public abstract void LoadPaperdolls();

	public abstract void LoadGui();

	public abstract void LoadMisc();

	public abstract void LoadFonts();

	public abstract void LoadShaders();

	public void LoadAudio()
	{
		LoadSounds();
		LoadMusic();
	}

	public abstract void LoadSounds();

	public abstract void LoadMusic();

	public static string RemoveExtension(string fileNameOrPath)
	{
		int num = fileNameOrPath.LastIndexOfAny(DirectorySeparators);
		int num2 = fileNameOrPath.LastIndexOf('.');
		if (num > num2 || num2 < 0)
		{
			return fileNameOrPath;
		}
		return fileNameOrPath.Substring(0, num2);
	}

	public string[] GetTextureNames(TextureType type)
	{
		return type switch
		{
			TextureType.Tileset => mTilesetDict.Keys.ToArray(), 
			TextureType.Item => mItemDict.Keys.ToArray(), 
			TextureType.Entity => mEntityDict.Keys.ToArray(), 
			TextureType.Spell => mSpellDict.Keys.ToArray(), 
			TextureType.Animation => mAnimationDict.Keys.ToArray(), 
			TextureType.Face => mFaceDict.Keys.ToArray(), 
			TextureType.Image => mImageDict.Keys.ToArray(), 
			TextureType.Fog => mFogDict.Keys.ToArray(), 
			TextureType.Resource => mResourceDict.Keys.ToArray(), 
			TextureType.Paperdoll => mPaperdollDict.Keys.ToArray(), 
			TextureType.Gui => mGuiDict.Keys.ToArray(), 
			TextureType.Misc => mMiscDict.Keys.ToArray(), 
			_ => null, 
		};
	}

	public bool TryGetTexture(TextureType textureType, string textureName, [NotNullWhen(true)] out IGameTexture? texture)
	{
		texture = GetTexture(textureType, textureName);
		return texture != null;
	}

	public virtual IGameTexture? GetTexture(TextureType type, string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		IDictionary<string, IAsset> dictionary;
		switch (type)
		{
		case TextureType.Tileset:
			dictionary = mTilesetDict;
			break;
		case TextureType.Item:
			dictionary = mItemDict;
			break;
		case TextureType.Entity:
			dictionary = mEntityDict;
			break;
		case TextureType.Spell:
			dictionary = mSpellDict;
			break;
		case TextureType.Animation:
			dictionary = mAnimationDict;
			break;
		case TextureType.Face:
			dictionary = mFaceDict;
			break;
		case TextureType.Image:
			dictionary = mImageDict;
			break;
		case TextureType.Fog:
			dictionary = mFogDict;
			break;
		case TextureType.Resource:
			dictionary = mResourceDict;
			break;
		case TextureType.Paperdoll:
			dictionary = mPaperdollDict;
			break;
		case TextureType.Gui:
			dictionary = mGuiDict;
			break;
		case TextureType.Misc:
			dictionary = mMiscDict;
			break;
		default:
			return null;
		}
		if (dictionary == null)
		{
			return null;
		}
		if (dictionary.TryGetValue(name, out var value))
		{
			return value as IGameTexture;
		}
		if (type == TextureType.Entity && name.Contains("-c", StringComparison.OrdinalIgnoreCase))
		{
			string text = Regex.Replace(name, "-c\\d+(?=_|\\.|$)", string.Empty);
			if (!string.Equals(text, name, StringComparison.Ordinal) && dictionary.TryGetValue(text, out var value2))
			{
				return value2 as IGameTexture;
			}
		}
		return null;
	}

	public bool TryGetShader(string shaderName, [NotNullWhen(true)] out GameShader? shader)
	{
		shader = GetShader(shaderName);
		return shader != null;
	}

	public virtual GameShader? GetShader(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		return mShaderDict?.GetValueOrDefault(name);
	}

	public virtual IFont? GetFont(string? name)
	{
		if (name == null)
		{
			return null;
		}
		string key = name.Trim();
		if (mFontDict.TryGetValue(key, out IFont value))
		{
			return value;
		}
		ApplicationContext.CurrentContext.Logger.LogWarning("Missing font '{FontName}'", name);
		return null;
	}

	public virtual GameAudioSource? GetMusic(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (mMusicDict == null)
		{
			return null;
		}
		if (!mMusicDict.TryGetValue(name, out IAsset value))
		{
			return null;
		}
		return value as GameAudioSource;
	}

	public virtual GameAudioSource? GetSound(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (mSoundDict == null)
		{
			return null;
		}
		if (!mSoundDict.TryGetValue(name, out IAsset value))
		{
			return null;
		}
		return value as GameAudioSource;
	}

	public virtual bool GetLayout(UI stage, string name, string resolution, bool skipCache, Action<string, bool> layoutHandler)
	{
		bool isReload = false;
		string path = stage.ToString().ToLowerInvariant();
		if (stage == UI.InGame)
		{
			path = "game";
		}
		string path2 = Path.Combine("gui", "layouts", path);
		if (!string.IsNullOrWhiteSpace(resolution))
		{
			string targetPath = Path.Combine(path2, name + "." + resolution + ".json");
			addCreateOrChangeListener(targetPath, createIfMissing: false);
		}
		string targetPath2 = Path.Combine(path2, name + ".json");
		addCreateOrChangeListener(targetPath2, createIfMissing: true);
		return true;
		void addChangeListener(string path3)
		{
			ContentWatcher?.AddEventListener(Intersect.Client.Framework.Content.ContentWatcher.Event.Change, path3, delegate
			{
				onLayoutReady();
			});
			onLayoutReady();
		}
		void addCreateOrChangeListener(string text, bool createIfMissing)
		{
			string chemin = Path.Combine(ClientConfiguration.ResourcesDirectory, text);
			if (Coffre.Existe(chemin))
			{
				addChangeListener(text);
			}
			else
			{
				if (createIfMissing)
				{
					layoutHandler?.Invoke(string.Empty, arg2: false);
				}
				ContentWatcher?.AddEventListener(Intersect.Client.Framework.Content.ContentWatcher.Event.Create, text, delegate
				{
					addChangeListener(text);
				});
			}
		}
		void onLayoutReady()
		{
			bool cacheHit;
			string layout = GetLayout(stage, name, resolution, skipCache || isReload, out cacheHit);
			layoutHandler(layout, cacheHit);
			isReload = true;
		}
	}

	protected virtual string GetLayout(UI stage, string name, string resolution, bool skipCache, out bool cacheHit)
	{
		cacheHit = false;
		KeyValuePair<UI, string> key = new KeyValuePair<UI, string>(stage, name + "." + resolution + ".json");
		if (!skipCache && mUiDict.TryGetValue(key, out string value))
		{
			cacheHit = true;
			return value;
		}
		string path = stage.ToString().ToLowerInvariant();
		if (stage == UI.InGame)
		{
			path = "game";
		}
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "gui", "layouts", path);
		if (!Coffre.DossierExiste(text))
		{
			Directory.CreateDirectory(text);
		}
		string chemin = Path.Combine(text, name + "." + resolution + ".json");
		if (!string.IsNullOrWhiteSpace(resolution) && Coffre.Existe(chemin))
		{
			value = Coffre.LireTexte(chemin);
			mUiDict[key] = value;
			return value;
		}
		string genericPath = Path.Combine(text, name + ".json");
		if (Coffre.Existe(genericPath))
		{
			value = ExceptionRepeater.Run(() => Coffre.LireTexte(genericPath), 5);
			mUiDict[key] = value;
			return value;
		}
		return string.Empty;
	}

	public virtual string GetUIJson(UI stage, string name, string resolution, out bool cacheHit)
	{
		return GetLayout(stage, name, resolution, skipCache: false, out cacheHit);
	}

	public virtual void SaveUIJson(UI stage, string name, string json, string? resolution)
	{
		string path = stage.ToString().ToLowerInvariant();
		if (stage == UI.InGame)
		{
			path = "game";
		}
		string path2 = Path.Combine("gui", "layouts", path);
		resolution = resolution?.Trim();
		string resourceName;
		if (!string.IsNullOrWhiteSpace(resolution))
		{
			resourceName = Path.Combine(path2, name + "." + resolution?.Trim() + ".json");
			string resourcePath = Path.Combine(ClientConfiguration.ResourcesDirectory, resourceName);
			if (File.Exists(resourcePath))
			{
				ContentWatcher?.Modify(resourceName, delegate
				{
					try
					{
						File.WriteAllText(resourcePath, json);
					}
					catch (Exception exception)
					{
						ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Error occurred saving {ResourcePath}", resourcePath);
					}
				});
				return;
			}
		}
		resourceName = Path.Combine(path2, name + ".json");
		ContentWatcher?.Modify(resourceName, delegate
		{
			string text = Path.Combine(ClientConfiguration.ResourcesDirectory, resourceName);
			try
			{
				File.WriteAllText(text, json);
			}
			catch (Exception exception)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug(exception, "Error occurred saving {ResourcePath}", text);
			}
		});
	}

	protected Dictionary<string, IAsset> GetAssetLookup(ContentType contentType)
	{
		return contentType switch
		{
			ContentType.Animation => mAnimationDict, 
			ContentType.Entity => mEntityDict, 
			ContentType.Face => mFaceDict, 
			ContentType.Fog => mFogDict, 
			ContentType.Image => mImageDict, 
			ContentType.Interface => mGuiDict, 
			ContentType.Item => mItemDict, 
			ContentType.Miscellaneous => mMiscDict, 
			ContentType.Paperdoll => mPaperdollDict, 
			ContentType.Resource => mResourceDict, 
			ContentType.Spell => mSpellDict, 
			ContentType.TextureAtlas => mTexturePackDict, 
			ContentType.Tileset => mTilesetDict, 
			ContentType.Font => throw new NotImplementedException(), 
			ContentType.Shader => throw new NotImplementedException(), 
			ContentType.Music => mMusicDict, 
			ContentType.Sound => mSoundDict, 
			_ => throw new ArgumentOutOfRangeException("contentType", contentType, null), 
		};
	}

	protected abstract TAsset Load<TAsset>(Dictionary<string, IAsset> lookup, ContentType contentType, string assetName, Func<Stream> createStream) where TAsset : class, IAsset;

	public TAsset Find<TAsset>(ContentType contentType, string assetName) where TAsset : class, IAsset
	{
		Dictionary<string, IAsset> assetLookup = GetAssetLookup(contentType);
		if (assetLookup.TryGetValue(assetName, out var value))
		{
			return value as TAsset;
		}
		return null;
	}

	public TAsset Load<TAsset>(ContentType contentType, string assetPath, string assetAlias) where TAsset : class, IAsset
	{
		if (!Coffre.Existe(assetPath))
		{
			throw new FileNotFoundException("Asset does not exist at '" + assetPath + "'.");
		}
		return Load<TAsset>(contentType, assetAlias.ToLower(), () => Coffre.Ouvrir(assetPath));
	}

	public TAsset Load<TAsset>(ContentType contentType, string assetName, Func<Stream> createStream) where TAsset : class, IAsset
	{
		Dictionary<string, IAsset> assetLookup = GetAssetLookup(contentType);
		if (assetLookup.TryGetValue(assetName, out var value))
		{
			return (value as TAsset) ?? throw new InvalidOperationException();
		}
		return Load<TAsset>(assetLookup, contentType, assetName, createStream);
	}

	public TAsset LoadEmbedded<TAsset>(IPluginContext context, ContentType contentType, string assetName) where TAsset : class, IAsset
	{
		string manifestResourceName = context.EmbeddedResources.Resolve(assetName);
		return Load<TAsset>(contentType, assetName, () => context.EmbeddedResources.Read(manifestResourceName));
	}

	public TAsset LoadEmbedded<TAsset>(IPluginContext context, ContentType contentType, string assetName, string assetAlias) where TAsset : class, IAsset
	{
		string text = context.EmbeddedResources.Resolve(assetName);
		return Load<TAsset>(contentType, assetName, assetAlias);
	}
}
