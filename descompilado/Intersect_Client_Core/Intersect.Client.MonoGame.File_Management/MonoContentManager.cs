using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Localization;
using Intersect.Client.MonoGame.Audio;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Compression;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.MonoGame.File_Management;

public class MonoContentManager : GameContentManager
{
	public MonoContentManager()
	{
		string fullPath = Path.GetFullPath(ClientConfiguration.ResourcesDirectory);
		if (!Directory.Exists(fullPath))
		{
			ApplicationContext.Context.Value?.Logger.LogError(Strings.Errors.ResourcesNotFound);
			Environment.Exit(1);
		}
		base.ContentWatcher = new ContentWatcher(fullPath);
	}

	public override void LoadTilesets(string[] tilesetnames)
	{
		mTilesetDict.Clear();
		IEnumerable<string> source = Array.Empty<string>();
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "tilesets");
		if (!Coffre.DossierExiste(text))
		{
			if (!Coffre.DossierExiste(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs")))
			{
				Directory.CreateDirectory(text);
			}
		}
		else
		{
			source = from f in Coffre.Fichiers(text)
				select Path.GetFileName(f);
		}
		List<(IGameTexture, string)> list = new List<(IGameTexture, string)>();
		foreach (string t in tilesetnames)
		{
			string text2 = source.FirstOrDefault((string file) => t.Equals(file, StringComparison.InvariantCultureIgnoreCase)) ?? t;
			string assetName = Path.Combine(ClientConfiguration.ResourcesDirectory, "tilesets", t.ToLower()).Replace('\\', '/');
			if (!string.IsNullOrWhiteSpace(t) && (!string.IsNullOrWhiteSpace(text2) || AtlasReference.TryGet(assetName, out AtlasReference _)) && !mTilesetDict.ContainsKey(t.ToLower()))
			{
				string text3 = Path.Combine(ClientConfiguration.ResourcesDirectory, "tilesets", text2);
				IGameTexture gameTexture = Intersect.Client.Core.Graphics.Renderer.LoadTexture(assetName, text3);
				mTilesetDict.Add(t.ToLower(), gameTexture);
				list.Add((gameTexture, text3));
			}
		}
		base.TilesetsLoaded = true;
		(Intersect.Client.Core.Graphics.Renderer as MonoRenderer)?.LireDAvance(list);
	}

	public override void LoadTexturePacks()
	{
		mTexturePackDict.Clear();
		string path = Path.Combine(ClientConfiguration.ResourcesDirectory, "packs");
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		string[] files = Directory.GetFiles(path, "*.meta");
		for (int i = 0; i < files.Length; i++)
		{
			string json = GzipCompression.ReadDecompressedString(files[i]);
			JObject jObject = JObject.Parse(json);
			JArray jArray = (JArray)jObject["frames"];
			string path2 = jObject["meta"]["image"].ToString();
			if (!File.Exists(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", path2)))
			{
				continue;
			}
			IGameTexture gameTexture = Intersect.Client.Core.Graphics.Renderer.LoadTexture(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", path2), Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", path2));
			if (gameTexture == null)
			{
				continue;
			}
			foreach (JToken item in jArray)
			{
				string name = item["filename"].ToString();
				Rectangle bounds = new Rectangle(int.Parse(item["frame"]["x"].ToString()), int.Parse(item["frame"]["y"].ToString()), int.Parse(item["frame"]["w"].ToString()), int.Parse(item["frame"]["h"].ToString()));
				bool isRotated = bool.Parse(item["rotated"].ToString());
				Rectangle sourceBounds = new Rectangle(int.Parse(item["spriteSourceSize"]["x"].ToString()), int.Parse(item["spriteSourceSize"]["y"].ToString()), int.Parse(item["spriteSourceSize"]["w"].ToString()), int.Parse(item["spriteSourceSize"]["h"].ToString()));
				AtlasReference.Add(new AtlasReference(name, bounds, isRotated, sourceBounds, gameTexture));
			}
		}
	}

	public void LoadTextureGroup(string directory, Dictionary<string, IAsset> dict)
	{
		dict.Clear();
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, directory);
		if (!Coffre.DossierExiste(text))
		{
			if (!Coffre.DossierExiste(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs")))
			{
				Directory.CreateDirectory(text);
			}
		}
		else
		{
			string[] array = Coffre.Fichiers(text, "*.png");
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar).ToLower();
				dict.Add(text2, Intersect.Client.Core.Graphics.Renderer.LoadTexture(Path.Combine(text, text2), Path.Combine(text, array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar))));
			}
		}
		AtlasReference[] allFor = AtlasReference.GetAllFor(directory);
		AtlasReference[] array2 = allFor;
		foreach (AtlasReference atlasReference in array2)
		{
			string fileName = Path.GetFileName(atlasReference.Name.ToLower().Replace("\\", "/"));
			if (!dict.ContainsKey(fileName))
			{
				dict.Add(fileName, Intersect.Client.Core.Graphics.Renderer.LoadTexture(Path.Combine(text, fileName), Path.Combine(text, Path.Combine(text, fileName))));
			}
		}
	}

	public override void LoadItems()
	{
		LoadTextureGroup("items", mItemDict);
	}

	public override void LoadEntities()
	{
		LoadTextureGroup("entities", mEntityDict);
	}

	public override void LoadSpells()
	{
		LoadTextureGroup("spells", mSpellDict);
	}

	public override void LoadAnimations()
	{
		LoadTextureGroup("animations", mAnimationDict);
	}

	public override void LoadFaces()
	{
		LoadTextureGroup("faces", mFaceDict);
	}

	public override void LoadImages()
	{
		LoadTextureGroup("images", mImageDict);
	}

	public override void LoadFogs()
	{
		LoadTextureGroup("fogs", mFogDict);
	}

	public override void LoadResources()
	{
		LoadTextureGroup(ClientConfiguration.ResourcesDirectory, mResourceDict);
	}

	public override void LoadPaperdolls()
	{
		LoadTextureGroup("paperdolls", mPaperdollDict);
	}

	public override void LoadGui()
	{
		LoadTextureGroup("gui", mGuiDict);
	}

	public override void LoadMisc()
	{
		LoadTextureGroup("misc", mMiscDict);
	}

	public override void LoadFonts()
	{
		mFontDict.Clear();
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "fonts");
		if (!Coffre.DossierExiste(text))
		{
			Directory.CreateDirectory(text);
		}
		FileInfo[] array = (from filePath in Coffre.Fichiers(text, "*.xnb")
			select new FileInfo(filePath)).ToArray();
		Dictionary<string, Dictionary<int, FileInfo>> dictionary = new Dictionary<string, Dictionary<int, FileInfo>>();
		FileInfo[] array2 = array;
		string key;
		foreach (FileInfo fileInfo in array2)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
			int num2 = fileNameWithoutExtension.LastIndexOf(',');
			if (num2 < 1)
			{
				num2 = fileNameWithoutExtension.LastIndexOf('_');
			}
			if (num2 < 1)
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Ignoring invalid font file name: '{FileName}' located at {FilePath}", fileInfo.Name, fileInfo.FullName);
				continue;
			}
			key = fileNameWithoutExtension;
			int num3 = num2 + 1;
			string text2 = key.Substring(num3, key.Length - num3);
			if (!int.TryParse(text2, out var result))
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Font size '{Size}' in font file name '{FileName}' is not a valid integer", text2, fileNameWithoutExtension);
				continue;
			}
			if (result < 1)
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("Font size '{Size}' in font file name '{FileName}' must be greater than or equal to 1", result, fileNameWithoutExtension);
				continue;
			}
			string text3 = fileNameWithoutExtension.Substring(0, num2).Trim().ToLowerInvariant();
			if (!dictionary.TryGetValue(text3, out var value))
			{
				value = (dictionary[text3] = new Dictionary<int, FileInfo>());
			}
			if (value.TryGetValue(result, out var value2))
			{
				ApplicationContext.CurrentContext.Logger.LogWarning("A font file for size {Size} already exists for the font family {Family}, the first one is located at {OriginalPath} and this one is located at {CurrentPath}", result, text3, value2.FullName, fileInfo.FullName);
			}
			else
			{
				value[result] = fileInfo;
			}
		}
		foreach (KeyValuePair<string, Dictionary<int, FileInfo>> item in dictionary)
		{
			item.Deconstruct(out key, out var value3);
			string text4 = key;
			Dictionary<int, FileInfo> fontSourcesBySize = value3;
			IFont value4 = Intersect.Client.Core.Graphics.Renderer.LoadFont(text4, fontSourcesBySize);
			if (!mFontDict.TryAdd(text4, value4))
			{
				throw new UnreachableException();
			}
		}
	}

	public override void LoadShaders()
	{
		mShaderDict.Clear();
		string[] array = (from resourceName in typeof(MonoContentManager).Assembly.GetManifestResourceNames()
			where resourceName.StartsWith("Intersect.Client.Resources.Shaders.") && resourceName.EndsWith(".xnb")
			select resourceName).ToArray();
		foreach (string text in array)
		{
			string text2 = text.Substring(0, text.Length - 4);
			string key = text2.Substring("Intersect.Client.Resources.Shaders.".Length);
			mShaderDict.Add(key, Intersect.Client.Core.Graphics.Renderer.LoadShader(text));
		}
	}

	public override void LoadSounds()
	{
		mSoundDict.Clear();
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "sounds");
		if (!Coffre.DossierExiste(text))
		{
			if (!Coffre.DossierExiste(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs")))
			{
				Directory.CreateDirectory(text);
			}
		}
		else
		{
			string[] array = Coffre.Fichiers(text, "*.wav");
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar).ToLower();
				mSoundDict.Add(GameContentManager.RemoveExtension(text2), new MonoSoundSource(Path.Combine(text, text2), Path.Combine(text, array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar))));
			}
		}
		if (!File.Exists(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", "sound.index")))
		{
			return;
		}
		base.SoundPacks = new AssetPacker(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", "sound.index"), Path.Combine(ClientConfiguration.ResourcesDirectory, "packs"));
		foreach (string file in base.SoundPacks.FileList)
		{
			if (!mSoundDict.ContainsKey(GameContentManager.RemoveExtension(file).ToLower()))
			{
				mSoundDict.Add(GameContentManager.RemoveExtension(file).ToLower(), new MonoSoundSource(file, file));
			}
		}
	}

	public override void LoadMusic()
	{
		mMusicDict.Clear();
		string text = Path.Combine(ClientConfiguration.ResourcesDirectory, "music");
		if (!Coffre.DossierExiste(text))
		{
			if (!Coffre.DossierExiste(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs")))
			{
				Directory.CreateDirectory(text);
			}
		}
		else
		{
			string[] array = Coffre.Fichiers(text, "*.ogg");
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar).ToLower();
				mMusicDict.Add(GameContentManager.RemoveExtension(text2), new MonoMusicSource(Path.Combine(text, text2), Path.Combine(text, array[i].Replace(text, "").TrimStart(Path.DirectorySeparatorChar))));
			}
		}
		if (!File.Exists(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", "music.index")))
		{
			return;
		}
		base.MusicPacks = new AssetPacker(Path.Combine(ClientConfiguration.ResourcesDirectory, "packs", "music.index"), Path.Combine(ClientConfiguration.ResourcesDirectory, "packs"));
		foreach (string file in base.MusicPacks.FileList)
		{
			if (!mMusicDict.ContainsKey(GameContentManager.RemoveExtension(file).ToLower()))
			{
				mMusicDict.Add(GameContentManager.RemoveExtension(file).ToLower(), new MonoMusicSource(file, file));
			}
		}
	}

	protected override TAsset Load<TAsset>(Dictionary<string, IAsset> lookup, ContentType contentType, string name, Func<Stream> createStream)
	{
		switch (contentType)
		{
		case ContentType.Animation:
		case ContentType.Entity:
		case ContentType.Face:
		case ContentType.Fog:
		case ContentType.Image:
		case ContentType.Interface:
		case ContentType.Item:
		case ContentType.Miscellaneous:
		case ContentType.Paperdoll:
		case ContentType.Resource:
		case ContentType.Spell:
		case ContentType.TextureAtlas:
		case ContentType.Tileset:
		{
			TAsset val3 = Intersect.Client.Core.Graphics.Renderer.CreateTextureFromStreamFactory(name, createStream) as TAsset;
			lookup?.Add(name, val3);
			return val3;
		}
		case ContentType.Font:
			throw new NotImplementedException();
		case ContentType.Shader:
			throw new NotImplementedException();
		case ContentType.Music:
		{
			TAsset val2 = new MonoMusicSource(createStream) as TAsset;
			lookup?.Add(GameContentManager.RemoveExtension(name), val2);
			return val2;
		}
		case ContentType.Sound:
		{
			TAsset val = new MonoSoundSource(createStream, name) as TAsset;
			lookup?.Add(GameContentManager.RemoveExtension(name), val);
			return val;
		}
		default:
			throw new ArgumentOutOfRangeException("contentType", contentType, null);
		}
	}
}
