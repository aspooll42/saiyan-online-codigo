using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Intersect.Client.Classes.MonoGame.Graphics;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Items;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Compression;
using Intersect.Config;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Casse;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Lighting;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Framework.Core.Serialization;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Intersect.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Intersect.Client.Maps;

public class MapInstance : MapDescriptor, IGameObject<Guid, MapInstance>, IMapInstance
{
	public delegate void MapLoadedDelegate(MapInstance map);

	private readonly record struct TuileAnimee(int X, int Y, TuilesAnimees.Animation Animation, int Dx, int Dy);

	private readonly record struct TuileVent(GenreVent Genre, int X, int Y, IGameTexture Planche, int SourceX, int SourceY, ModeleArbre? Modele, int CaseX, int CaseY);

	private int _casseVersion = -1;

	private long _casseReessaiA;

	private readonly HashSet<int> _casseObjets = new HashSet<int>();

	private readonly Dictionary<string, bool[,]> _casseCases = new Dictionary<string, bool[,]>();

	private readonly Dictionary<string, List<int>> _casseParCouche = new Dictionary<string, List<int>>();

	public const int MapAnimationFrames = 3;

	private const int XpAnimationFrameTileWidth = 3;

	private const int VxAnimationFrameTileWidth = 2;

	private static MapControllers sLookup;

	private long _weatherParticleSpawnTime;

	private Dictionary<MapAttribute, Animation> mAttributeAnimInstances = new Dictionary<MapAttribute, Animation>();

	private Dictionary<MapAttribute, Entity> mAttributeCritterInstances = new Dictionary<MapAttribute, Entity>();

	protected float mCurFogIntensity;

	private List<Event> mEvents = new List<Event>();

	protected float mFogCurrentX;

	protected float mFogCurrentY;

	protected long mFogUpdateTime = -1L;

	private long _lastUpdateTime;

	protected float mOverlayIntensity;

	protected long mOverlayUpdateTime = -1L;

	protected float mPanoramaIntensity;

	protected long mPanoramaUpdateTime = -1L;

	private bool mTexturesFound;

	private readonly Dictionary<string, Dictionary<object, GameTileBuffer[]>> _tileBuffersPerTexturePerLayer = new Dictionary<string, Dictionary<object, GameTileBuffer[]>>();

	private readonly Dictionary<string, GameTileBuffer[][]> _tileBuffersPerLayer = new Dictionary<string, GameTileBuffer[][]>();

	private Task? _vboCompute;

	private int _versionAuDela;

	private readonly int _width = Options.Instance.Map.MapWidth;

	private readonly int _height = Options.Instance.Map.MapHeight;

	private readonly int _tileWidth = Options.Instance.Map.TileWidth;

	private readonly int _tileHeight = Options.Instance.Map.TileHeight;

	private readonly int _tileHalfWidth = Options.Instance.Map.TileWidth / 2;

	private readonly int _tileHalfHeight = Options.Instance.Map.TileHeight / 2;

	private int _gridY;

	private int _gridX;

	private const int TailleObjet = 22;

	private static readonly int[] RayonsHalo = new int[6] { 0, 0, 9, 11, 13, 15 };

	private static readonly int[] NombresEtincelles = new int[6] { 0, 0, 0, 0, 4, 6 };

	private static readonly int[] HauteursTrait = new int[6] { 0, 0, 0, 18, 34, 54 };

	private const int Flottement = 6;

	private static readonly Dictionary<string, (IGameTexture Icone, IGameTexture Blanc)?> IconesAuSol = new Dictionary<string, (IGameTexture, IGameTexture)?>();

	private static readonly Dictionary<int, IGameTexture?> Halos = new Dictionary<int, IGameTexture>();

	private static readonly HashSet<int> PointsOnde = new HashSet<int>();

	private static readonly List<FloatRect> PlaquesPosees = new List<FloatRect>();

	private readonly Dictionary<Guid, long> _apparitionsObjets = new Dictionary<Guid, long>();

	private bool _objetsDejaVus;

	private readonly Dictionary<string, List<TuileAnimee>> _tuilesAnimees = new Dictionary<string, List<TuileAnimee>>();

	private int _versionAnimees;

	private readonly Dictionary<string, List<TuileVent>> _tuilesVent = new Dictionary<string, List<TuileVent>>();

	private readonly Dictionary<string, bool[,]> _casesVent = new Dictionary<string, bool[,]>();

	private const int PassesArbre = 7;

	internal string CasseSignature { get; private set; } = string.Empty;

	internal IReadOnlyCollection<int> CasseObjets => _casseObjets;

	public static Dictionary<Guid, long> MapRequests { get; set; } = new Dictionary<Guid, long>();

	public List<IWeatherParticle> _removeParticles { get; set; } = new List<IWeatherParticle>();

	public List<IWeatherParticle> _weatherParticles { get; set; } = new List<IWeatherParticle>();

	public List<IActionMessage> ActionMessages { get; set; } = new List<IActionMessage>();

	IReadOnlyList<IActionMessage> IMapInstance.ActionMessages => ActionMessages;

	public List<IMapSound> AttributeSounds { get; set; } = new List<IMapSound>();

	IReadOnlyList<IMapSound> IMapInstance.AttributeSounds => AttributeSounds;

	public ConcurrentDictionary<Guid, MapAnimation> LocalAnimations { get; set; } = new ConcurrentDictionary<Guid, MapAnimation>();

	IReadOnlyList<IMapAnimation> IMapInstance.Animations => LocalAnimations.Values.ToList();

	public Dictionary<Guid, Entity> LocalEntities { get; } = new Dictionary<Guid, Entity>();

	IReadOnlyList<IEntity> IMapInstance.Entities => LocalEntities.Values.ToList();

	public Dictionary<Guid, Critter> LocalCritters { get; set; } = new Dictionary<Guid, Critter>();

	IReadOnlyList<IEntity> IMapInstance.Critters => LocalCritters.Values.ToList();

	public List<Guid> LocalEntitiesToDispose { get; set; } = new List<Guid>();

	public Dictionary<int, List<IMapItemInstance>> MapItems { get; set; } = new Dictionary<int, List<IMapItemInstance>>();

	IReadOnlyList<IMapItemInstance> IMapInstance.Items => MapItems.Values.SelectMany((List<IMapItemInstance> x) => x).ToList();

	public bool IsDisposed { get; private set; }

	public bool IsLoaded { get; private set; }

	public bool[] CameraHolds { get; set; } = new bool[4];

	public int GridX
	{
		get
		{
			return _gridX;
		}
		set
		{
			if (value != _gridX)
			{
				_gridX = value;
				X = _gridX * _width * _tileWidth;
			}
		}
	}

	public int GridY
	{
		get
		{
			return _gridY;
		}
		set
		{
			if (value != _gridY)
			{
				_gridY = value;
				Y = _gridY * _height * _tileHeight;
			}
		}
	}

	public IMapSound? BackgroundSound { get; set; }

	public new static MapControllers Lookup => sLookup ?? (sLookup = new MapControllers(DatabaseObject<MapDescriptor>.Lookup));

	public int X { get; private set; }

	public int Y { get; private set; }

	public static event MapLoadedDelegate? MapLoaded;

	private void NoterSignatureCasse(byte[] tuiles)
	{
		CasseSignature = CasseDonnees.Signature(tuiles);
		_casseVersion = -1;
	}

	internal void CasseChangee()
	{
		_casseVersion = -1;
	}

	private void MettreAJourCasse()
	{
		long milliseconds = Timing.Global.Milliseconds;
		Casse.NoterVue(base.Id, milliseconds);
		int num = Casse.VersionDuVoisinage(this);
		if (num == _casseVersion && (_casseReessaiA == 0L || milliseconds < _casseReessaiA))
		{
			return;
		}
		CasseCarte casseCarte = Casse.CarteDe(this);
		if (casseCarte == null && _casseObjets.Count == 0)
		{
			return;
		}
		_casseVersion = num;
		_casseReessaiA = 0L;
		HashSet<int> hashSet = new HashSet<int>();
		bool flag = false;
		if (casseCarte != null)
		{
			foreach (int key3 in Casse.ObjetsTouches(base.Id).Keys)
			{
				if (key3 >= 0 && key3 < casseCarte.Objets.Length)
				{
					hashSet.Add(key3);
				}
			}
			List<Casse.Zone> list = Casse.Zones(this, casseCarte, milliseconds);
			if (list.Count > 0)
			{
				int tileWidth = Options.Instance.Map.TileWidth;
				for (int i = 0; i < casseCarte.Objets.Length; i++)
				{
					CasseObjet casseObjet = casseCarte.Objets[i];
					CasseGenre genre = casseObjet.Genre;
					bool flag2 = (uint)(genre - 3) <= 1u;
					if (!flag2 || hashSet.Contains(i))
					{
						continue;
					}
					float x = casseObjet.X * tileWidth;
					float y = casseObjet.Y * tileWidth;
					float x2 = (casseObjet.X + casseObjet.Largeur) * tileWidth;
					float y2 = (casseObjet.Y + casseObjet.Hauteur) * tileWidth;
					foreach (Casse.Zone item in list)
					{
						if (Casse.Touche(item, x, y, x2, y2))
						{
							hashSet.Add(i);
							break;
						}
					}
				}
			}
			List<Casse.SillonVu> list2 = Casse.SillonsDe(base.Id);
			if (list2.Count > 0)
			{
				int tileWidth2 = Options.Instance.Map.TileWidth;
				for (int j = 0; j < casseCarte.Objets.Length; j++)
				{
					CasseObjet casseObjet2 = casseCarte.Objets[j];
					CasseGenre genre = casseObjet2.Genre;
					bool flag2 = (uint)(genre - 3) <= 1u;
					if (flag2 && !hashSet.Contains(j) && Casse.PresDUnSillon(list2, (float)(casseObjet2.X * tileWidth2) - 40f, casseObjet2.Y * tileWidth2, (float)((casseObjet2.X + casseObjet2.Largeur) * tileWidth2) + 40f, (float)((casseObjet2.Y + casseObjet2.Hauteur) * tileWidth2) + 90f))
					{
						hashSet.Add(j);
					}
				}
			}
			bool flag3 = true;
			foreach (int item2 in hashSet)
			{
				if (!_casseObjets.Contains(item2) && !Casse.Preparer(this, casseCarte, item2))
				{
					flag3 = false;
				}
			}
			if (!flag3)
			{
				_casseReessaiA = milliseconds + 16;
				return;
			}
			flag = Casse.ZonesIncompletes(base.Id);
		}
		if (flag)
		{
			_casseReessaiA = milliseconds + 100;
		}
		if (hashSet.SetEquals(_casseObjets))
		{
			return;
		}
		_casseObjets.Clear();
		_casseObjets.UnionWith(hashSet);
		_casseCases.Clear();
		_casseParCouche.Clear();
		if (casseCarte != null)
		{
			foreach (int casseObjet3 in _casseObjets)
			{
				(string, int, int)[] cases = casseCarte.Objets[casseObjet3].Cases;
				for (int k = 0; k < cases.Length; k++)
				{
					var (key, num2, num3) = cases[k];
					if (num2 < 0 || num3 < 0 || num2 >= _width || num3 >= _height)
					{
						continue;
					}
					if (!_casseCases.TryGetValue(key, out bool[,] value))
					{
						value = (_casseCases[key] = new bool[_width, _height]);
					}
					value[num2, num3] = true;
					if (!_casseParCouche.TryGetValue(key, out List<int> value2))
					{
						value2 = (_casseParCouche[key] = new List<int>());
					}
					if (value2.Count != 0)
					{
						List<int> list4 = value2;
						if (list4[list4.Count - 1] == casseObjet3)
						{
							continue;
						}
					}
					value2.Add(casseObjet3);
				}
				if (casseCarte.Objets[casseObjet3].Rocher)
				{
					string key2 = Casse.CoucheDesEboulis(casseCarte.Objets[casseObjet3]);
					if (!_casseParCouche.TryGetValue(key2, out List<int> value3))
					{
						value3 = (_casseParCouche[key2] = new List<int>());
					}
					if (!value3.Contains(casseObjet3))
					{
						value3.Add(casseObjet3);
					}
				}
			}
		}
		DetruireTampons(oublierSolsPeints: false);
	}

	internal bool CasseDessine(int objet)
	{
		return _casseObjets.Contains(objet);
	}

	private bool CasseExclut(string couche, int x, int y)
	{
		if (_casseCases.TryGetValue(couche, out bool[,] value) && x >= 0 && y >= 0 && x < _width && y < _height)
		{
			return value[x, y];
		}
		return false;
	}

	private void DessinerSolsBrules(string couche)
	{
		LayerOptions layers = Options.Instance.Map.Layers;
		List<string> lowerLayers = layers.LowerLayers;
		if (couche == (layers.All.FirstOrDefault() ?? "Ground"))
		{
			Casse.DessinerSillons(this);
			if (lowerLayers.Count < 2)
			{
				Casse.DessinerTas(this);
			}
		}
		if (lowerLayers.Count > 1)
		{
			if (couche == lowerLayers[lowerLayers.Count - 1])
			{
				Casse.DessinerSillonsDessus(this);
				Casse.DessinerTas(this);
			}
		}
	}

	private void DessinerCasse(string couche)
	{
		if (_casseObjets.Count == 0 || !_casseParCouche.TryGetValue(couche, out List<int> value))
		{
			return;
		}
		CasseCarte casseCarte = Casse.CarteDe(this);
		if (casseCarte == null)
		{
			return;
		}
		foreach (int item in value)
		{
			Casse.DessinerObjet(this, casseCarte, item, couche);
		}
	}

	public void MarkLoadFinished()
	{
		ApplicationContext.CurrentContext.Logger.LogDebug("Done loading map {Id} ({Name}) @ ({GridX}, {GridY})", base.Id, base.Name, GridX, GridY);
		MapInstance.MapLoaded?.Invoke(this);
	}

	public MapInstance(Guid id)
		: base(id)
	{
	}

	public void Load(string json)
	{
		LireDonnees(json);
		FinirChargement();
	}

	internal void LireDonnees(string json)
	{
		LocalEntitiesToDispose.AddRange(LocalEntities.Keys.ToArray());
		JsonConvert.PopulateObject(json, this, new JsonSerializerSettings
		{
			SerializationBinder = new IntersectTypeSerializationBinder(),
			ObjectCreationHandling = ObjectCreationHandling.Replace
		});
	}

	internal void FinirChargement()
	{
		IsLoaded = true;
		Autotiles = new MapAutotiles(this);
		MapLoaded -= HandleMapLoaded;
		MapLoaded += HandleMapLoaded;
		MapRequests.Remove(base.Id);
	}

	public void LoadTileData(byte[] packet)
	{
		NoterSignatureCasse(packet);
		Layers = JsonConvert.DeserializeObject<Dictionary<string, Tile[,]>>(LZ4.UnPickleString(packet), base.mJsonSerializerSettings);
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!Layers.ContainsKey(item))
			{
				Layers.Add(item, new Tile[_width, _height]);
			}
		}
	}

	private void CacheTextures()
	{
		if (mTexturesFound || !GameContentManager.Current.TilesetsLoaded)
		{
			return;
		}
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!Layers.TryGetValue(item, out var value))
			{
				continue;
			}
			for (int i = 0; i < _width; i++)
			{
				for (int j = 0; j < _height; j++)
				{
					Tile tile = value[i, j];
					if (!(tile.TilesetId == default(Guid)) && DatabaseObject<TilesetDescriptor>.TryGet(tile.TilesetId, out var databaseObject))
					{
						IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Tileset, databaseObject.Name);
						value[i, j].TilesetTexture = texture;
					}
				}
			}
		}
		mTexturesFound = true;
	}

	public void Update(bool isLocal)
	{
		long milliseconds = Timing.Global.Milliseconds;
		if (!isLocal)
		{
			if (milliseconds > _lastUpdateTime)
			{
				Dispose();
			}
			HideActiveAnimations();
			return;
		}
		_lastUpdateTime = milliseconds + 10000;
		UpdateMapAttributes();
		if (BackgroundSound == null && !string.IsNullOrWhiteSpace(base.Sound))
		{
			BackgroundSound = Audio.AddMapSound(base.Sound, -1, -1, base.Id, loop: true, 0, 10);
		}
		Guid key;
		foreach (KeyValuePair<Guid, MapAnimation> localAnimation in LocalAnimations)
		{
			localAnimation.Deconstruct(out key, out var value);
			Guid key2 = key;
			MapAnimation mapAnimation = value;
			if (mapAnimation.IsDisposed)
			{
				LocalAnimations.TryRemove(key2, out value);
			}
			else
			{
				mapAnimation.Update();
			}
		}
		Entity value2;
		foreach (KeyValuePair<Guid, Entity> localEntity in LocalEntities)
		{
			localEntity.Deconstruct(out key, out value2);
			Entity entity = value2;
			entity.Update();
		}
		foreach (KeyValuePair<MapAttribute, Entity> mAttributeCritterInstance in mAttributeCritterInstances)
		{
			mAttributeCritterInstance.Deconstruct(out var _, out value2);
			Entity entity2 = value2;
			entity2.Update();
		}
		foreach (Guid item in LocalEntitiesToDispose)
		{
			LocalEntities.Remove(item);
		}
		LocalEntitiesToDispose.Clear();
	}

	public bool InView()
	{
		IMapInstance mapInstance = Globals.Me.MapInstance;
		if (Globals.MapGridWidth == 0L || Globals.MapGridHeight == 0L || mapInstance == null)
		{
			return true;
		}
		int gridX = mapInstance.GridX;
		int gridY = mapInstance.GridY;
		for (int i = gridX - 1; i <= gridX + 1; i++)
		{
			for (int j = gridY - 1; j <= gridY + 1; j++)
			{
				if (i >= 0 && i < Globals.MapGridWidth && j >= 0 && j < Globals.MapGridHeight && Globals.MapGrid[i, j] == base.Id)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void HandleMapLoaded(MapInstance map)
	{
		if (map == this || Math.Abs(map.GridX - GridX) > 1 || Math.Abs(map.GridY - GridY) > 1)
		{
			return;
		}
		HashSet<GameTileBuffer> hashSet = new HashSet<GameTileBuffer>();
		if (Globals.GridMaps.ContainsKey(map.Id) && Globals.GridMaps.ContainsKey(base.Id) && IsLoaded)
		{
			MapDescriptor[,] surroundingMaps = GenerateAutotileGrid();
			if (map.GridX == GridX - 1)
			{
				if (map.GridY == GridY - 1)
				{
					hashSet.UnionWith(CheckAutotile(0, 0, surroundingMaps));
				}
				else if (map.GridY == GridY)
				{
					for (int i = 0; i < _height; i++)
					{
						hashSet.UnionWith(CheckAutotile(0, i, surroundingMaps));
					}
				}
				else if (map.GridY == GridY + 1)
				{
					hashSet.UnionWith(CheckAutotile(0, _height - 1, surroundingMaps));
				}
			}
			else if (map.GridX == GridX)
			{
				if (map.GridY == GridY - 1)
				{
					for (int j = 0; j < _width; j++)
					{
						hashSet.UnionWith(CheckAutotile(j, 0, surroundingMaps));
					}
				}
				else if (map.GridY == GridY + 1)
				{
					for (int k = 0; k < _width; k++)
					{
						hashSet.UnionWith(CheckAutotile(k, _height - 1, surroundingMaps));
					}
				}
			}
			else if (map.GridX == GridX + 1)
			{
				if (map.GridY == GridY - 1)
				{
					hashSet.UnionWith(CheckAutotile(_width - 1, _height, surroundingMaps));
				}
				else if (map.GridY == GridY)
				{
					for (int l = 0; l < _height; l++)
					{
						hashSet.UnionWith(CheckAutotile(_width - 1, l, surroundingMaps));
					}
				}
				else if (map.GridY == GridY + 1)
				{
					hashSet.UnionWith(CheckAutotile(_width - 1, _height - 1, surroundingMaps));
				}
			}
			foreach (string item in Options.Instance.Map.Layers.All)
			{
				if (!Layers.TryGetValue(item, out var value))
				{
					continue;
				}
				for (int m = 0; m < _width; m++)
				{
					for (int n = 0; n < _height; n++)
					{
						if (value[m, n].Autotile == 4)
						{
							hashSet.UnionWith(CheckAutotile(m, n, surroundingMaps));
						}
					}
				}
			}
		}
		foreach (GameTileBuffer item2 in hashSet)
		{
			item2.SetData();
		}
	}

	private GameTileBuffer[] CheckAutotile(int x, int y, MapDescriptor[,] surroundingMaps)
	{
		int x2 = X;
		int y2 = Y;
		List<GameTileBuffer> list = new List<GameTileBuffer>();
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (!Autotiles.UpdateAutoTile(x, y, item, surroundingMaps) || !_tileBuffersPerTexturePerLayer.TryGetValue(item, out Dictionary<object, GameTileBuffer[]> value) || !Layers.TryGetValue(item, out var value2) || EstTuileVent(item, x, y))
			{
				continue;
			}
			Tile tile = value2[x, y];
			if (tile.TilesetTexture == null)
			{
				continue;
			}
			IGameTexture gameTexture = (IGameTexture)tile.TilesetTexture;
			if (tile.X < 0 || tile.Y < 0 || tile.X * _tileWidth >= gameTexture.Width || tile.Y * _tileHeight >= gameTexture.Height)
			{
				continue;
			}
			object texture = gameTexture.GetTexture();
			if (!value.TryGetValue(texture, out var value3))
			{
				continue;
			}
			int num = x * _tileWidth + x2;
			int num2 = y * _tileHeight + y2;
			for (int i = 0; i < 3; i++)
			{
				GameTileBuffer gameTileBuffer = value3[i];
				DrawAutoTile(item, num, num2, 1, x, y, i, gameTexture, gameTileBuffer, update: true);
				DrawAutoTile(item, num + _tileHalfWidth, num2, 2, x, y, i, gameTexture, gameTileBuffer, update: true);
				DrawAutoTile(item, num, num2 + _tileHalfHeight, 3, x, y, i, gameTexture, gameTileBuffer, update: true);
				DrawAutoTile(item, num + _tileHalfWidth, num2 + _tileHalfHeight, 4, x, y, i, gameTexture, gameTileBuffer, update: true);
				if (!list.Contains(gameTileBuffer))
				{
					list.Add(gameTileBuffer);
				}
			}
		}
		return list.ToArray();
	}

	public new MapDescriptor[,] GenerateAutotileGrid()
	{
		MapDescriptor[,] array = new MapDescriptor[3, 3];
		if (Globals.MapGrid != null && Globals.GridMaps.ContainsKey(base.Id))
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int num = GridX + i;
					int num2 = GridY + j;
					if (num >= 0 && num2 >= 0 && num < Globals.MapGridWidth && num2 < Globals.MapGridHeight)
					{
						if (i == 0 && j == 0)
						{
							array[i + 1, j + 1] = this;
						}
						else
						{
							array[i + 1, j + 1] = Lookup.Get<MapInstance>(Globals.MapGrid[num, num2]);
						}
					}
				}
			}
		}
		return array;
	}

	private void UpdateMapAttributes()
	{
		MapAttribute[,] attributes = base.Attributes;
		if (attributes == null)
		{
			return;
		}
		for (int i = 0; i < _height; i++)
		{
			for (int j = 0; j < _width; j++)
			{
				MapAttribute mapAttribute = attributes[j, i];
				if (mapAttribute == null)
				{
					continue;
				}
				switch (mapAttribute.Type)
				{
				case MapAttributeType.Animation:
				{
					AnimationDescriptor animationDescriptor2 = DatabaseObject<AnimationDescriptor>.Get(((MapAnimationAttribute)mapAttribute).AnimationId);
					if (animationDescriptor2 != null)
					{
						if (!mAttributeAnimInstances.ContainsKey(mapAttribute))
						{
							Animation animation = new Animation(animationDescriptor2, loopForever: true);
							animation.SetPosition(X + j * _tileWidth + _tileHalfWidth, Y + i * _tileHeight + _tileHalfHeight, j, i, base.Id, Direction.Up);
							mAttributeAnimInstances.Add(mapAttribute, animation);
						}
						mAttributeAnimInstances[mapAttribute].Update();
					}
					break;
				}
				case MapAttributeType.Critter:
				{
					MapCritterAttribute mapCritterAttribute = (MapCritterAttribute)mapAttribute;
					string sprite = mapCritterAttribute.Sprite;
					AnimationDescriptor animationDescriptor = DatabaseObject<AnimationDescriptor>.Get(mapCritterAttribute.AnimationId);
					if (animationDescriptor != null || !TextUtils.IsNone(sprite))
					{
						if (!mAttributeCritterInstances.ContainsKey(mapAttribute))
						{
							Critter critter = new Critter(this, (byte)j, (byte)i, mapCritterAttribute);
							LocalCritters.Add(critter.Id, critter);
							mAttributeCritterInstances.Add(mapAttribute, critter);
						}
						mAttributeCritterInstances[mapAttribute].Update();
					}
					break;
				}
				}
			}
		}
	}

	private void ClearMapAttributes()
	{
		foreach (KeyValuePair<MapAttribute, Animation> mAttributeAnimInstance in mAttributeAnimInstances)
		{
			mAttributeAnimInstance.Value.Dispose();
		}
		foreach (KeyValuePair<MapAttribute, Entity> mAttributeCritterInstance in mAttributeCritterInstances)
		{
			mAttributeCritterInstance.Value.Dispose();
		}
		LocalCritters.Clear();
		mAttributeCritterInstances.Clear();
		mAttributeAnimInstances.Clear();
	}

	public void CreateMapSounds()
	{
		ClearAttributeSounds();
		for (int i = 0; i < _width; i++)
		{
			for (int j = 0; j < _height; j++)
			{
				MapAttribute[,] attributes = base.Attributes;
				MapAttribute mapAttribute = ((attributes != null) ? attributes[i, j] : null);
				if (mapAttribute != null && mapAttribute.Type == MapAttributeType.Sound && !TextUtils.IsNone(((MapSoundAttribute)mapAttribute).File))
				{
					MapSound item = Audio.AddMapSound(((MapSoundAttribute)mapAttribute).File, i, j, base.Id, loop: true, ((MapSoundAttribute)mapAttribute).LoopInterval, ((MapSoundAttribute)mapAttribute).Distance);
					AttributeSounds?.Add(item);
				}
			}
		}
	}

	private void ClearAttributeSounds()
	{
		AttributeSounds?.ForEach(Audio.StopSound);
		AttributeSounds?.Clear();
	}

	public IAnimation? AddTileAnimation(Guid animationDescriptorId, int tileX, int tileY, Direction dir = Direction.None, IEntity? owner = null, AnimationSource source = default(AnimationSource))
	{
		if (!DatabaseObject<AnimationDescriptor>.TryGet(animationDescriptorId, out var databaseObject))
		{
			return null;
		}
		return AddTileAnimation(databaseObject, tileX, tileY, dir, owner, source);
	}

	public IAnimation? AddTileAnimation(AnimationDescriptor animationDescriptor, int tileX, int tileY, Direction dir = Direction.None, IEntity? owner = null, AnimationSource source = default(AnimationSource))
	{
		MapAnimation mapAnimation = new MapAnimation(animationDescriptor, tileX, tileY, dir, owner as Entity, source);
		LocalAnimations.TryAdd(mapAnimation.Id, mapAnimation);
		mapAnimation.SetPosition(X + tileX * _tileWidth + _tileHalfWidth, Y + tileY * _tileHeight + _tileHalfHeight, tileX, tileY, base.Id, dir);
		return mapAnimation;
	}

	private void HideActiveAnimations()
	{
		foreach (Entity item in LocalEntities.Values.ToList())
		{
			item.ClearAnimations();
		}
		foreach (KeyValuePair<Guid, MapAnimation> localAnimation in LocalAnimations)
		{
			localAnimation.Value?.Dispose();
		}
		LocalAnimations?.Clear();
		ClearMapAttributes();
	}

	public void BuildVBOs()
	{
		lock (_tileBuffersPerLayer)
		{
			if (_vboCompute != null)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			_versionAnimees = TuilesAnimees.Version;
			_versionAuDela = Meteo.VersionAuDela;
			Dictionary<string, GameTileBuffer[][]> dictionary = new Dictionary<string, GameTileBuffer[][]>();
			foreach (string item in Options.Instance.Map.Layers.All)
			{
				GameTileBuffer[][] array = DrawMapLayer(item, X, Y);
				if (array == null)
				{
					continue;
				}
				dictionary[item] = array;
				for (int i = 0; i < 3; i++)
				{
					GameTileBuffer[] array2 = array[i];
					GameTileBuffer[] array3 = array2;
					foreach (GameTileBuffer gameTileBuffer in array3)
					{
						gameTileBuffer.SetData();
					}
				}
			}
			DateTime utcNow2 = DateTime.UtcNow;
			TimeSpan timeSpan = utcNow2 - utcNow;
			ApplicationContext.Context.Value?.Logger.LogInformation($"Built VBO for map {base.Id} '{base.Name}' in {timeSpan.TotalMilliseconds}ms");
			foreach (var (key, value) in dictionary)
			{
				_tileBuffersPerLayer[key] = value;
			}
		}
	}

	public void DestroyVBOs()
	{
		DetruireTampons(oublierSolsPeints: true);
	}

	private void DetruireTampons(bool oublierSolsPeints)
	{
		foreach (string item in Options.Instance.Map.Layers.All)
		{
			if (_tileBuffersPerTexturePerLayer.Remove(item, out Dictionary<object, GameTileBuffer[]> value))
			{
				value.Clear();
			}
			if (!_tileBuffersPerLayer.TryGetValue(item, out GameTileBuffer[][] value2))
			{
				continue;
			}
			GameTileBuffer[][] array = value2;
			foreach (GameTileBuffer[] array2 in array)
			{
				GameTileBuffer[] array3 = array2;
				foreach (GameTileBuffer gameTileBuffer in array3)
				{
					gameTileBuffer.Dispose();
				}
			}
		}
		_tileBuffersPerTexturePerLayer.Clear();
		_tileBuffersPerLayer.Clear();
		_tuilesAnimees.Clear();
		if (oublierSolsPeints)
		{
			SolsPeints.Oublier(base.Id);
		}
		OublierToutVent();
	}

	public void Draw(int layer)
	{
		if (!IsLoaded)
		{
			return;
		}
		CacheTextures();
		if (!mTexturesFound)
		{
			return;
		}
		VerifierAnimees();
		if (_versionAuDela != Meteo.VersionAuDela && _tileBuffersPerLayer.Count > 0)
		{
			DetruireTampons(oublierSolsPeints: false);
		}
		MettreAJourCasse();
		if (_tileBuffersPerLayer.Count < 1)
		{
			BuildVBOs();
		}
		List<string> list = ((layer > 1) ? Options.Instance.Map.Layers.UpperLayers : ((layer != 1) ? Options.Instance.Map.Layers.LowerLayers : Options.Instance.Map.Layers.MiddleLayers));
		List<string> list2 = list;
		foreach (string item in list2)
		{
			if (!_tileBuffersPerLayer.TryGetValue(item, out GameTileBuffer[][] value) || value[Globals.AnimationFrame] == null)
			{
				DessinerCasse(item);
				DessinerSolsBrules(item);
				continue;
			}
			GameTileBuffer[] array = value[Globals.AnimationFrame];
			bool flag = Globals.Database?.AffichageCompatible ?? false;
			GameTileBuffer[] array2 = array;
			foreach (GameTileBuffer gameTileBuffer in array2)
			{
				if (flag && gameTileBuffer is MonoTileBuffer monoTileBuffer)
				{
					monoTileBuffer.DessinerTuileParTuile();
				}
				else
				{
					Graphics.Renderer?.DrawTileBuffer(gameTileBuffer);
				}
			}
			DessinerAnimees(item);
			Meteo.DessinerChutesDeLaCouche(this, item);
			DessinerVent(item);
			DessinerCasse(item);
			IGameTexture gameTexture = SolsPeints.Texture(base.Id, item);
			if (gameTexture != null)
			{
				Graphics.DrawGameTexture(gameTexture, X, Y);
			}
			DessinerSolsBrules(item);
		}
	}

	public void DrawItemsAndLights()
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		SuivreApparitions(millisecondsUtc);
		CapsulePilier.Dessiner(this, millisecondsUtc);
		foreach (KeyValuePair<int, List<IMapItemInstance>> mapItem in MapItems)
		{
			mapItem.Deconstruct(out var key, out var value);
			int num = key;
			List<IMapItemInstance> list = value;
			int tileX = num % _width;
			int tileY = (int)Math.Floor((float)num / (float)_width);
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				IMapItemInstance mapItemInstance = list[num2];
				if (DatabaseObject<ItemDescriptor>.TryGet(mapItemInstance.ItemId, out var databaseObject))
				{
					IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, databaseObject.Icon);
					if (texture != null)
					{
						DessinerObjetAuSol(mapItemInstance, databaseObject, texture, tileX, tileY, millisecondsUtc);
					}
				}
			}
		}
		for (int i = 0; i < base.Lights.Count; i++)
		{
			LightDescriptor lightDescriptor = base.Lights[i];
			double num3 = lightDescriptor.Size;
			float num4 = (float)(X + (lightDescriptor.TileX * _tileWidth + lightDescriptor.OffsetX)) + (float)_tileWidth / 2f;
			float num5 = (float)(Y + (lightDescriptor.TileY * _tileHeight + lightDescriptor.OffsetY)) + (float)_tileHeight / 2f;
			if (!Casse.TombeIci(this, num4 - (float)X, num5 - (float)Y))
			{
				Graphics.AddLight((int)num4, (int)num5, (int)num3, CaveLife.Breathe(this, i, lightDescriptor.Intensity), lightDescriptor.Expand, lightDescriptor.Color);
			}
		}
	}

	public void DrawItemNames()
	{
		Player me = Globals.Me;
		if (me == null || MapItems.Count == 0)
		{
			return;
		}
		int sourisX = -1;
		int sourisY = -1;
		if (!Intersect.Client.Interface.Interface.DoesMouseHitInterface())
		{
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
			int x = (int)(vector.X - (float)X) / _tileWidth;
			int y = (int)(vector.Y - (float)Y) / _tileHeight;
			Guid mapId = base.Id;
			if (me.TryGetRealLocation(ref x, ref y, ref mapId) && mapId == base.Id)
			{
				sourisX = x;
				sourisY = y;
			}
		}
		DessinerNomsObjets(me, sourisX, sourisY);
	}

	private void DrawAutoTile(string layerName, int destX, int destY, int quarterNum, int x, int y, int forceFrame, IGameTexture tileset, GameTileBuffer buffer, bool update = false, Tile? layerTile = null, Autotile? layerAutoTile = null)
	{
		if (!layerTile.HasValue)
		{
			if (!Layers.TryGetValue(layerName, out var value))
			{
				return;
			}
			layerTile = value[x, y];
		}
		if (layerAutoTile == null)
		{
			if (!Autotiles.Layers.TryGetValue(layerName, out var value2))
			{
				return;
			}
			layerAutoTile = value2[x, y];
		}
		Point16i point16i = layerAutoTile.QuarterTile[quarterNum];
		int num = 0;
		int num2 = 0;
		switch (layerTile.Value.Autotile)
		{
		case 5:
			num = (forceFrame - 1) * _tileHeight;
			break;
		case 3:
			num2 = forceFrame * _tileWidth * 2;
			break;
		case 7:
			num2 = forceFrame * _tileWidth * 3;
			break;
		case 4:
			num = -_tileHeight;
			break;
		}
		if (update)
		{
			if (!buffer.TryUpdateTile(tileset, destX, destY, point16i.X + num2, point16i.Y + num, _tileHalfWidth, _tileHalfHeight))
			{
				throw new Exception("Failed to update tile to VBO!");
			}
		}
		else if (!buffer.TryAddTile(tileset, destX, destY, point16i.X + num2, point16i.Y + num, _tileHalfWidth, _tileHalfHeight))
		{
			throw new Exception("Failed to add tile to VBO!");
		}
	}

	private GameTileBuffer[][]? DrawMapLayer(string layerName, int xOffset = 0, int yOffset = 0)
	{
		if (!Layers.TryGetValue(layerName, out var value))
		{
			return null;
		}
		if (!Autotiles.Layers.TryGetValue(layerName, out var value2))
		{
			return null;
		}
		Dictionary<object, GameTileBuffer[]> dictionary = new Dictionary<object, GameTileBuffer[]>();
		OublierVent(layerName);
		OublierAnimees(layerName);
		for (int i = 0; i < _width; i++)
		{
			for (int j = 0; j < _height; j++)
			{
				Tile tile = value[i, j];
				if (!(tile.TilesetTexture is IGameTexture gameTexture) || tile.X < 0 || tile.Y < 0 || tile.X * _tileWidth >= gameTexture.Width || tile.Y * _tileHeight >= gameTexture.Height)
				{
					continue;
				}
				object texture = gameTexture.GetTexture();
				if (texture == null || CasseExclut(layerName, i, j) || Meteo.AuDelaRemplace(this, gameTexture, tile.X, tile.Y) || (value2[i, j].RenderState == 1 && EnregistrerAnimee(layerName, i, j, tile)) || EnregistrerVent(layerName, i, j, tile, gameTexture))
				{
					continue;
				}
				GameTileBuffer[] array;
				if (dictionary.TryGetValue(texture, out var value3))
				{
					array = value3;
				}
				else
				{
					array = new GameTileBuffer[3];
					for (int k = 0; k < 3; k++)
					{
						array[k] = Graphics.Renderer.CreateTileBuffer();
					}
					dictionary.Add(texture, array);
				}
				int num = i * _tileWidth + xOffset;
				int num2 = j * _tileHeight + yOffset;
				Autotile autotile = value2[i, j];
				switch (autotile.RenderState)
				{
				case 1:
				{
					for (int m = 0; m < 3; m++)
					{
						GameTileBuffer gameTileBuffer = array[m];
						if (!gameTileBuffer.TryAddTile(gameTexture, num, num2, tile.X * _tileWidth, tile.Y * _tileHeight, _tileWidth, _tileHeight))
						{
							throw new Exception("Failed to add VBO!");
						}
					}
					break;
				}
				case 2:
				{
					for (int l = 0; l < 3; l++)
					{
						GameTileBuffer buffer = array[l];
						DrawAutoTile(layerName, num, num2, 1, i, j, l, gameTexture, buffer, update: false, tile, autotile);
						DrawAutoTile(layerName, num + _tileHalfWidth, num2, 2, i, j, l, gameTexture, buffer, update: false, tile, autotile);
						DrawAutoTile(layerName, num, num2 + _tileHalfHeight, 3, i, j, l, gameTexture, buffer, update: false, tile, autotile);
						DrawAutoTile(layerName, num + _tileHalfWidth, num2 + _tileHalfHeight, 4, i, j, l, gameTexture, buffer, update: false, tile, autotile);
					}
					break;
				}
				}
			}
		}
		GameTileBuffer[][] array2 = new GameTileBuffer[3][];
		for (int n = 0; n < 3; n++)
		{
			array2[n] = new GameTileBuffer[dictionary.Count];
		}
		GameTileBuffer[][] array3 = dictionary.Values.ToArray();
		for (int num3 = 0; num3 < array3.Length; num3++)
		{
			GameTileBuffer[] array4 = array3[num3];
			for (int num4 = 0; num4 < 3; num4++)
			{
				GameTileBuffer gameTileBuffer2 = array4[num4];
				array2[num4][num3] = gameTileBuffer2;
			}
		}
		_tileBuffersPerTexturePerLayer.Add(layerName, dictionary);
		return array2;
	}

	public void DrawFog()
	{
		if (Globals.Me == null || Lookup.Get(Globals.Me.MapId) == null || string.IsNullOrWhiteSpace(base.Fog))
		{
			return;
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Fog, base.Fog);
		if (texture == null)
		{
			return;
		}
		float num = Math.Min(Timing.Global.MillisecondsUtc - mFogUpdateTime, 1000L);
		mFogUpdateTime = Timing.Global.MillisecondsUtc;
		mCurFogIntensity = ((base.Id == Globals.Me.MapId) ? Math.Min(1f, mCurFogIntensity + num / 2000f) : Math.Max(0f, mCurFogIntensity - num / 2000f));
		mFogCurrentX += num / 1000f * (float)base.FogXSpeed * 2f;
		mFogCurrentY += num / 1000f * (float)base.FogYSpeed * 2f;
		mFogCurrentX %= texture.Width;
		mFogCurrentY %= texture.Height;
		byte b = (byte)((float)base.FogTransparency * mCurFogIntensity);
		if (b == 0)
		{
			return;
		}
		int num2 = _width * _tileWidth * 3 / texture.Width;
		int num3 = _height * _tileHeight * 3 / texture.Height;
		float num4 = (float)Math.Round(mFogCurrentX);
		float num5 = (float)Math.Round(mFogCurrentY);
		FloatRect worldViewport = Graphics.WorldViewport;
		float num6 = (float)X - (float)(_width * _tileWidth) * 1f + num4;
		float num7 = (float)Y - (float)(_height * _tileHeight) * 1f + num5;
		Color renderColor = new Color(b, 255, 255, 255);
		for (int i = -1; i <= num2; i++)
		{
			float num8 = num6 + (float)(i * texture.Width);
			if (num8 + (float)texture.Width < worldViewport.Left || num8 > worldViewport.Right)
			{
				continue;
			}
			for (int j = -1; j <= num3; j++)
			{
				float num9 = num7 + (float)(j * texture.Height);
				if (!(num9 + (float)texture.Height < worldViewport.Top) && !(num9 > worldViewport.Bottom))
				{
					Graphics.DrawGameTexture(texture, new FloatRect(0f, 0f, texture.Width, texture.Height), new FloatRect(num8, num9, texture.Width, texture.Height), renderColor);
				}
			}
		}
	}

	public void DrawWeather()
	{
		if (Globals.Me == null || Lookup.Get(Globals.Me.MapId) == null)
		{
			return;
		}
		AnimationDescriptor animationDescriptor = DatabaseObject<AnimationDescriptor>.Get(base.WeatherAnimationId);
		if (animationDescriptor == null || base.WeatherIntensity == 0)
		{
			return;
		}
		_removeParticles.Clear();
		if ((base.WeatherXSpeed != 0 || base.WeatherYSpeed != 0) && Globals.Me.MapInstance == this && Timing.Global.MillisecondsUtc > _weatherParticleSpawnTime)
		{
			_weatherParticles.Add(new WeatherParticle(_removeParticles, base.WeatherXSpeed, base.WeatherYSpeed, animationDescriptor));
			int num = 25 + (int)(475f * (1f - (float)base.WeatherIntensity / 100f));
			num = (int)((float)num * (480000f / (float)(Graphics.Renderer.ScreenWidth * Graphics.Renderer.ScreenHeight)));
			_weatherParticleSpawnTime = Timing.Global.MillisecondsUtc + num;
		}
		foreach (IWeatherParticle weatherParticle in _weatherParticles)
		{
			weatherParticle.Update();
		}
		foreach (IWeatherParticle removeParticle in _removeParticles)
		{
			removeParticle.Dispose();
			_weatherParticles.Remove(removeParticle);
		}
	}

	private void ClearWeather()
	{
		foreach (IWeatherParticle weatherParticle in _weatherParticles)
		{
			weatherParticle.Dispose();
		}
		_weatherParticles.Clear();
	}

	public void GridSwitched()
	{
		mPanoramaIntensity = 1f;
		mCurFogIntensity = 1f;
	}

	public void DrawPanorama()
	{
		float num = Timing.Global.MillisecondsUtc - mPanoramaUpdateTime;
		mPanoramaUpdateTime = Timing.Global.MillisecondsUtc;
		if (base.Id == Globals.Me.MapId)
		{
			if (mPanoramaIntensity != 1f)
			{
				mPanoramaIntensity += num / 2000f;
				if (mPanoramaIntensity > 1f)
				{
					mPanoramaIntensity = 1f;
				}
			}
		}
		else if (mPanoramaIntensity != 0f)
		{
			mPanoramaIntensity -= num / 2000f;
			if (mPanoramaIntensity < 0f)
			{
				mPanoramaIntensity = 0f;
			}
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Image, base.Panorama);
		if (texture != null)
		{
			Graphics.DrawFullScreenTexture(texture, mPanoramaIntensity);
		}
	}

	public void DrawOverlayGraphic()
	{
		float num = Timing.Global.MillisecondsUtc - mOverlayUpdateTime;
		mOverlayUpdateTime = Timing.Global.MillisecondsUtc;
		if (base.Id == Globals.Me.MapId)
		{
			if (mOverlayIntensity != 1f)
			{
				mOverlayIntensity += num / 2000f;
				if (mOverlayIntensity > 1f)
				{
					mOverlayIntensity = 1f;
				}
			}
		}
		else if (mOverlayIntensity != 0f)
		{
			mOverlayIntensity -= num / 2000f;
			if (mOverlayIntensity < 0f)
			{
				mOverlayIntensity = 0f;
			}
		}
		IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Image, base.OverlayGraphic);
		if (texture != null)
		{
			Graphics.DrawFullScreenTexture(texture, mOverlayIntensity);
		}
	}

	public void CompareEffects(IMapInstance oldMap)
	{
		if (!(oldMap is MapInstance mapInstance))
		{
			return;
		}
		if (mapInstance.Fog == base.Fog)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Fog, base.Fog);
			if (texture == null)
			{
				return;
			}
			mFogUpdateTime = mapInstance.mFogUpdateTime;
			float num = (float)mapInstance.FogTransparency / (float)base.FogTransparency;
			mCurFogIntensity = num * mapInstance.mCurFogIntensity;
			mFogCurrentX = mapInstance.mFogCurrentX;
			mFogCurrentY = mapInstance.mFogCurrentY;
			float value = X - oldMap.X;
			float value2 = Y - oldMap.Y;
			mFogCurrentX += _tileWidth * _width % texture.Width * -Math.Sign(value);
			mFogCurrentY += _tileHeight * _height % texture.Height * -Math.Sign(value2);
			mapInstance.mCurFogIntensity = 0f;
		}
		if (mapInstance.Panorama == base.Panorama)
		{
			mPanoramaIntensity = mapInstance.mPanoramaIntensity;
			mPanoramaUpdateTime = mapInstance.mPanoramaUpdateTime;
			mapInstance.mPanoramaIntensity = 0f;
		}
		if (mapInstance.OverlayGraphic == base.OverlayGraphic)
		{
			mOverlayIntensity = mapInstance.mOverlayIntensity;
			mOverlayUpdateTime = mapInstance.mOverlayUpdateTime;
			mapInstance.mOverlayIntensity = 0f;
		}
	}

	public void DrawActionMsgs()
	{
		for (int num = ActionMessages.Count - 1; num > -1; num--)
		{
			IActionMessage actionMessage = ActionMessages[num];
			int num2 = Math.Clamp(1000 - (int)(actionMessage.TransmissionTimer - Timing.Global.MillisecondsUtc), 0, 1000);
			float num3 = (float)num2 / 1000f;
			float num4 = (float)_tileHeight * 1.5f * (1f - (1f - num3) * (1f - num3));
			bool flag = actionMessage.Text.Length > 0 && actionMessage.Text.All(delegate(char character)
			{
				bool flag6 = char.IsDigit(character);
				bool flag7 = flag6;
				if (!flag7)
				{
					bool flag8 = ((character == '+' || character == '-') ? true : false);
					flag7 = flag8;
				}
				return flag7;
			});
			float num5 = (flag ? 1f : 0.7f);
			float num6 = ((num2 < 120) ? (0.3f * (1f - (float)num2 / 120f)) : 0f);
			float scale = num5 + num6;
			int num7 = ((num2 > 650) ? ((int)((float)(255 * (1000 - num2)) / 350f)) : 255);
			bool flag2 = ((!flag) ? actionMessage.Text.Contains("crit", StringComparison.OrdinalIgnoreCase) : (actionMessage is ActionMessage actionMessage2 && CombatFeedback.WasCritical(actionMessage2.EntityId, actionMessage2.CreatedAt)));
			if (flag2)
			{
				scale += (flag ? (0.25f + num6 * 0.5f) : 0.2f);
			}
			Color color = actionMessage.Color;
			bool flag3 = actionMessage.Text.StartsWith('-');
			bool flag4 = Globals.Me != null && actionMessage.EntityId == Globals.Me.Id;
			bool flag5 = Globals.Me != null && actionMessage.EntityId != Guid.Empty && actionMessage.EntityId == Globals.Me.TargetId;
			if (flag3 && flag4)
			{
				color = new Color(255, 232, 64, 64);
				scale += 0.05f;
			}
			else if (!flag4 && !flag5 && actionMessage.EntityId != Guid.Empty)
			{
				num7 = num7 * 3 / 5;
			}
			if (flag2 && !flag4)
			{
				color = new Color(255, 255, 140, 40);
			}
			Color fontColor = new Color(num7, color.R, color.G, color.B);
			Color borderColor = new Color(num7, 11, 14, 23);
			int x = X + actionMessage.X * _tileWidth + actionMessage.XOffset;
			float y = (float)(Y + actionMessage.Y * _tileHeight) - num4;
			float textWidth = Graphics.Renderer.MeasureText(actionMessage.Text, Graphics.ActionMsgFont, Graphics.ActionMsgFontSize, scale).X;
			TexteMonde.Lisse(delegate
			{
				Graphics.Renderer.DrawString(actionMessage.Text, Graphics.ActionMsgFont, Graphics.ActionMsgFontSize, (float)x - textWidth / 2f, y, scale, fontColor, worldPos: true, null, borderColor);
			});
			actionMessage.TryRemove();
		}
	}

	public void AddEvent(Guid evtId, EventEntityPacket packet)
	{
		if (IsLoaded)
		{
			if (LocalEntities.ContainsKey(evtId))
			{
				LocalEntities[evtId].Load(packet);
				return;
			}
			Event obj = new Event(evtId, packet);
			LocalEntities.Add(evtId, obj);
			mEvents.Add(obj);
		}
	}

	public new static MapInstance? Get(Guid id)
	{
		return Lookup.Get<MapInstance>(id);
	}

	public static bool TryGet(Guid id, out MapInstance instance)
	{
		instance = Lookup.Get<MapInstance>(id);
		if (instance == null)
		{
			return false;
		}
		return true;
	}

	public override void Delete()
	{
		if (Lookup != null)
		{
			Lookup.Delete(this);
		}
	}

	public void Dispose(bool prep = true, bool killentities = true)
	{
		IsDisposed = true;
		ApplicationContext.CurrentContext.Logger.LogDebug("Disposing map {Id} ({Name}) @ ({GridX}, {GridY})", base.Id, base.Name, GridX, GridY);
		IsLoaded = false;
		MapLoaded -= HandleMapLoaded;
		foreach (Event mEvent in mEvents)
		{
			mEvent.Dispose();
		}
		mEvents.Clear();
		if (killentities)
		{
			foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
			{
				if (entity.Value.MapId == base.Id)
				{
					Globals.EntitiesToDispose.Add(entity.Key);
				}
			}
			foreach (KeyValuePair<Guid, Entity> localEntity in LocalEntities)
			{
				localEntity.Value.Dispose();
			}
		}
		HideActiveAnimations();
		ClearWeather();
		ClearMapAttributes();
		ClearAttributeSounds();
		DestroyVBOs();
		Delete();
	}

	public static bool MapNotRequested(Guid mapId)
	{
		if (!MapRequests.TryGetValue(mapId, out var value))
		{
			return true;
		}
		return value < Timing.Global.Milliseconds;
	}

	public static void UpdateMapRequestTime(params Guid[] mapIds)
	{
		foreach (Guid guid in mapIds)
		{
			if (!(guid == default(Guid)))
			{
				MapRequests[guid] = Timing.Global.Milliseconds + 2000;
			}
		}
	}

	public bool PrepareLocalMapTextures()
	{
		if (!IsLoaded)
		{
			return false;
		}
		CacheTextures();
		return mTexturesFound;
	}

	public static void NouvelleImageObjets()
	{
		PlaquesPosees.Clear();
	}

	private static Color CouleurRarete(int rarete)
	{
		if (!CustomColors.Items.Rarities.TryGetValue(rarete, out var value) || !(value != null))
		{
			return new Color(255, 242, 242, 242);
		}
		return value;
	}

	private static Color Eclaircir(Color c, float k, int alpha)
	{
		return new Color(alpha, (int)((float)(int)c.R + (float)(255 - c.R) * k), (int)((float)(int)c.G + (float)(255 - c.G) * k), (int)((float)(int)c.B + (float)(255 - c.B) * k));
	}

	private (float X, float Sol) PiedObjet(int tileX, int tileY)
	{
		return (X: (float)(X + tileX * _tileWidth) + (float)_tileWidth / 2f, Sol: (float)(Y + tileY * _tileHeight) + (float)_tileHeight / 2f + 9f);
	}

	private void SuivreApparitions(long maintenant)
	{
		int num = 0;
		foreach (List<IMapItemInstance> value in MapItems.Values)
		{
			foreach (IMapItemInstance item in value)
			{
				num++;
				if (!_apparitionsObjets.ContainsKey(item.Id))
				{
					_apparitionsObjets[item.Id] = (_objetsDejaVus ? maintenant : long.MinValue);
				}
			}
		}
		_objetsDejaVus = true;
		if (_apparitionsObjets.Count > num + 64)
		{
			HashSet<Guid> gardes = (from o in MapItems.Values.SelectMany((List<IMapItemInstance> l) => l)
				select o.Id).ToHashSet();
			Guid[] array = _apparitionsObjets.Keys.Where((Guid id) => !gardes.Contains(id)).ToArray();
			foreach (Guid key in array)
			{
				_apparitionsObjets.Remove(key);
			}
		}
	}

	private void DessinerObjetAuSol(IMapItemInstance objet, ItemDescriptor descripteur, IGameTexture icone, int tileX, int tileY, long maintenant)
	{
		int num = Math.Clamp(JetDObjet.Rarete(descripteur, objet.ItemProperties), 0, RayonsHalo.Length - 1);
		(float X, float Sol) tuple = PiedObjet(tileX, tileY);
		float item = tuple.X;
		float item2 = tuple.Sol;
		item = MathF.Round(item);
		item2 = MathF.Round(item2);
		float num2 = (float)(objet.Id.GetHashCode() & 0xFFFF) / 65535f * 6.28f;
		long value;
		long num3 = (_apparitionsObjets.TryGetValue(objet.Id, out value) ? value : long.MinValue);
		float num4 = 0f;
		float num5 = 1f;
		bool flag = true;
		if (num3 != long.MinValue)
		{
			float num6 = (float)(maintenant - num3) / 1000f;
			if (num6 < 0.55f)
			{
				float num7 = num6 / 0.55f;
				num4 = 72f * num7 * (1f - num7);
				flag = false;
			}
			else if (num6 < 0.77f)
			{
				float num8 = (num6 - 0.55f) / 0.22f;
				num4 = 28f * num8 * (1f - num8);
				flag = false;
			}
			else if (num6 < 0.91f)
			{
				float num9 = (num6 - 0.77f) / 0.14f;
				num4 = 10f * num9 * (1f - num9);
				flag = false;
			}
			num5 = Math.Clamp((num6 - 0.91f) / 0.4f, 0f, 1f);
		}
		float num10 = MathF.Max(0.35f, 1f - num4 / 40f) * (flag ? (1f - 0.22f * num5) : 1f);
		OmbreObjet(item, item2 + 1f, 7f * num10, 2f * num10, (int)(107f * num10));
		float num11 = (flag ? MathF.Round(6f * num5 + MathF.Sin((float)maintenant * 0.003f + num2) * 2f) : 0f);
		float num12 = item2 - 22f + 2f - num11 - MathF.Round(num4);
		float y = num12 + 1f + 11f;
		if (num5 > 0f && RayonsHalo[num] > 0)
		{
			DessinerAnneauAuSol(item, item2 + 1f, num, maintenant, num2, num5);
			DessinerHalo(item, y, num, maintenant, num2, num5);
		}
		if (num5 > 0f && HauteursTrait[num] > 0)
		{
			DessinerTrait(item, num12, num, maintenant, num2, num5);
		}
		(IGameTexture, IGameTexture)? tuple2 = IconeAuSol(descripteur.Icon, icone);
		if (tuple2.HasValue)
		{
			(IGameTexture, IGameTexture) valueOrDefault = tuple2.GetValueOrDefault();
			Graphics.DrawGameTexture(valueOrDefault.Item1, new FloatRect(0f, 0f, valueOrDefault.Item1.Width, valueOrDefault.Item1.Height), new FloatRect(item - 12f, num12, 24f, 24f), descripteur.Color);
			float num13 = ((float)maintenant / 1000f + num2 * 0.6f) % 3.2f / 0.35f;
			if (flag && num13 < 1f)
			{
				int num14 = (int)(num13 * 26f) - 2;
				int num15 = Math.Max(0, num14);
				int num16 = Math.Min(24, num14 + 2) - num15;
				if (num16 > 0)
				{
					Graphics.DrawGameTexture(valueOrDefault.Item2, new FloatRect(num15, 0f, num16, 24f), new FloatRect(item - 12f + (float)num15, num12, num16, 24f), new Color(140, 255, 255, 240));
				}
			}
		}
		else
		{
			Graphics.DrawGameTexture(icone, new FloatRect(0f, 0f, icone.Width, icone.Height), new FloatRect(item - 11f, num12 + 1f, 22f, 22f), descripteur.Color);
		}
		if (num5 > 0f && NombresEtincelles[num] > 0)
		{
			DessinerEtincelles(item, y, num, maintenant, objet.Id.GetHashCode(), num5);
		}
	}

	private static void DessinerAnneauAuSol(float x, float y, int rarete, long maintenant, float phase, float allume)
	{
		float num = 0.7f + 0.3f * MathF.Sin((float)maintenant * 0.004f + phase);
		Color renderColor = Eclaircir(CouleurRarete(rarete), 0.25f, (int)(220f * num * allume));
		float num2 = ((rarete >= 4) ? 13 : 11);
		float num3 = ((rarete >= 4) ? 5 : 4);
		PointsOnde.Clear();
		for (float num4 = 0f; num4 < (float)Math.PI * 2f; num4 += 0.06f)
		{
			int num5 = (int)MathF.Round(x + MathF.Cos(num4) * num2);
			int num6 = (int)MathF.Round(y + MathF.Sin(num4) * num3);
			if (PointsOnde.Add(num5 * 7919 + num6))
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num5, num6, 1f, 1f), renderColor);
			}
		}
	}

	private static void DessinerTrait(float x, float haut, int rarete, long maintenant, float phase, float allume)
	{
		int num = HauteursTrait[rarete];
		int num2 = ((rarete >= 5) ? 4 : 2);
		float num3 = 0.75f + 0.25f * MathF.Sin((float)maintenant * 0.0035f + phase);
		Color color = Eclaircir(CouleurRarete(rarete), 0.35f, 255);
		for (int i = 0; i < num; i++)
		{
			float num4 = 1f - (float)i / (float)num;
			Color renderColor = new Color((int)(210f * num4 * num4 * num3 * allume), color.R, color.G, color.B);
			Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x - (float)num2 / 2f, haut - 2f - (float)i, num2, 1f), renderColor, null, GameBlendModes.Add);
		}
	}

	private static void OmbreObjet(float x, float y, float rx, float ry, int alpha)
	{
		Color renderColor = new Color(alpha, 40, 36, 58);
		int num = Math.Max(1, (int)MathF.Round(ry));
		for (int i = -num; i <= num; i++)
		{
			float num2 = (float)i / ((float)num + 0.5f);
			int num3 = (int)MathF.Round(rx * MathF.Sqrt(MathF.Max(0f, 1f - num2 * num2)));
			if (num3 > 0)
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(x - (float)num3, y + (float)i, 2 * num3 + 1, 1f), renderColor);
			}
		}
	}

	private static void DessinerHalo(float x, float y, int rarete, long maintenant, float phase, float allume)
	{
		IGameTexture gameTexture = TextureHalo(rarete);
		if (gameTexture == null)
		{
			return;
		}
		int num = RayonsHalo[rarete];
		float num2 = ((rarete == 5) ? (0.72f + 0.28f * MathF.Sin((float)maintenant * 0.0045f + phase)) : (0.8f + 0.2f * MathF.Sin((float)maintenant * 0.0025f + phase)));
		Graphics.DrawGameTexture(gameTexture, new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height), new FloatRect(x - (float)num, y - (float)num, 2 * num + 1, 2 * num + 1), new Color((int)(255f * num2 * allume), 255, 255, 255));
		if (rarete < 4)
		{
			return;
		}
		float num3 = ((rarete == 5) ? 1.6f : 2.8f);
		float num4 = ((float)maintenant / 1000f + phase * 0.37f) % num3 / 1.1f;
		if (num4 >= 1f)
		{
			return;
		}
		float num5 = (float)num * (0.55f + 0.75f * num4);
		Color renderColor = Eclaircir(CouleurRarete(rarete), 0.3f, (int)(153f * (1f - num4) * allume));
		PointsOnde.Clear();
		for (float num6 = 0f; num6 < (float)Math.PI * 2f; num6 += 0.5f / num5)
		{
			int num7 = (int)MathF.Round(x + MathF.Cos(num6) * num5);
			int num8 = (int)MathF.Round(y + MathF.Sin(num6) * num5);
			if (PointsOnde.Add(num7 * 7919 + num8))
			{
				Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num7, num8, 1f, 1f), renderColor);
			}
		}
	}

	private static void DessinerEtincelles(float x, float y, int rarete, long maintenant, int graine, float allume)
	{
		int num = NombresEtincelles[rarete];
		int num2 = RayonsHalo[rarete];
		IGameTexture whitePixel = Graphics.Renderer.WhitePixel;
		for (int i = 0; i < num; i++)
		{
			float num3 = MathF.Abs(MathF.Sin((float)graine * 12.9898f + (float)i * 78.233f) * 43758.547f) % 1f;
			float num4 = num3 * 2f - 1f;
			float num5 = ((float)maintenant / 1000f * (0.45f + 0.08f * (float)i) + num3) % 1f;
			float num6 = MathF.Sin(num5 * (float)Math.PI) * allume;
			float num7 = MathF.Round(x + num4 * (float)num2 * 0.9f);
			float num8 = MathF.Round(y + (float)num2 * 0.35f - num5 * (float)(num2 + 10));
			Color renderColor = Eclaircir(CouleurRarete(rarete), 0.55f, (int)(255f * num6));
			Graphics.DrawGameTexture(whitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num7, num8, 1f, 1f), renderColor);
			if (num6 > 0.75f)
			{
				Graphics.DrawGameTexture(whitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num7 - 1f, num8, 3f, 1f), renderColor);
				Graphics.DrawGameTexture(whitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num7, num8 - 1f, 1f, 3f), renderColor);
			}
		}
	}

	private static IGameTexture? TextureHalo(int rarete)
	{
		if (Halos.TryGetValue(rarete, out IGameTexture value))
		{
			return value;
		}
		IGameTexture gameTexture = null;
		if (Graphics.Renderer is MonoRenderer monoRenderer)
		{
			int num = RayonsHalo[rarete];
			int num2 = 2 * num + 1;
			byte[] array = new byte[num2 * num2 * 4];
			Color color = CouleurRarete(rarete);
			Color color2 = Eclaircir(color, 0.45f, 158);
			Color color3 = Eclaircir(color, 0.2f, 204);
			for (int i = -num; i <= num; i++)
			{
				for (int j = -num; j <= num; j++)
				{
					float num3 = MathF.Sqrt(j * j + i * i) / (float)num;
					Color color4 = ((num3 >= 1f) ? null : ((num3 >= 0.88f) ? color3 : ((num3 < 0.42f) ? color2 : ((num3 < 0.68f) ? new Color(128, color.R, color.G, color.B) : ((((j + i) & 1) == 0) ? new Color(117, color.R, color.G, color.B) : null)))));
					if ((object)color4 != null)
					{
						Color color5 = color4;
						int num4 = ((i + num) * num2 + j + num) * 4;
						array[num4] = color5.R;
						array[num4 + 1] = color5.G;
						array[num4 + 2] = color5.B;
						array[num4 + 3] = color5.A;
					}
				}
			}
			gameTexture = monoRenderer.CreerTexturePixels($"objets/halo-{rarete}", num2, num2, array);
		}
		Halos[rarete] = gameTexture;
		return gameTexture;
	}

	private static (IGameTexture Icone, IGameTexture Blanc)? IconeAuSol(string? nom, IGameTexture icone)
	{
		string text = nom ?? icone.Name ?? "";
		if (IconesAuSol.TryGetValue(text, out (IGameTexture, IGameTexture)? value))
		{
			return value;
		}
		(IGameTexture, IGameTexture)? tuple = null;
		Color[] reduite;
		if (Graphics.Renderer is MonoRenderer monoRenderer && PixelsPlanche.Lire(icone, out Color[] pixels) && pixels.Length == icone.Width * icone.Height)
		{
			reduite = new Color[484];
			for (int i = 0; i < 22; i++)
			{
				for (int j = 0; j < 22; j++)
				{
					reduite[i * 22 + j] = pixels[i * icone.Height / 22 * icone.Width + j * icone.Width / 22];
				}
			}
			byte[] array = new byte[2304];
			byte[] array2 = new byte[2304];
			for (int k = 0; k < 24; k++)
			{
				for (int l = 0; l < 24; l++)
				{
					int num = l - 1;
					int num2 = k - 1;
					int num3 = (k * 24 + l) * 4;
					if (Plein(num, num2))
					{
						Color color = reduite[num2 * 22 + num];
						array[num3] = color.R;
						array[num3 + 1] = color.G;
						array[num3 + 2] = color.B;
						array[num3 + 3] = color.A;
						int num4 = num3 + 1;
						int num5 = num3 + 2;
						byte b;
						array2[num3 + 3] = (b = byte.MaxValue);
						array2[num3] = (array2[num4] = (array2[num5] = b));
					}
					else if (Plein(num - 1, num2) || Plein(num + 1, num2) || Plein(num, num2 - 1) || Plein(num, num2 + 1))
					{
						array[num3] = 11;
						array[num3 + 1] = 14;
						array[num3 + 2] = 23;
						array[num3 + 3] = byte.MaxValue;
					}
				}
			}
			tuple = (monoRenderer.CreerTexturePixels("objets/" + text, 24, 24, array), monoRenderer.CreerTexturePixels("objets/" + text + "/blanc", 24, 24, array2));
		}
		IconesAuSol[text] = tuple;
		return tuple;
		bool Plein(int x, int y)
		{
			if (x >= 0 && y >= 0 && x < 22 && y < 22)
			{
				return reduite[y * 22 + x].A > 40;
			}
			return false;
		}
	}

	private void DessinerNomsObjets(Entity joueur, int sourisX, int sourisY)
	{
		IFont entityNameFont = Graphics.EntityNameFont;
		int taille = Math.Max(7, (int)MathF.Round((float)Graphics.TailleDeRepere(14) * Graphics.WorldScale / MathF.Max(0.25f, Graphics.ZoomDeLaVue)));
		Vector2 center = joueur.Center;
		Color renderColor = new Color(220, 11, 14, 23);
		foreach (KeyValuePair<int, List<IMapItemInstance>> mapItem in MapItems)
		{
			mapItem.Deconstruct(out var key, out var value);
			int num = key;
			List<IMapItemInstance> list = value;
			int num2 = num % _width;
			int num3 = num / _width;
			var (num4, num5) = PiedObjet(num2, num3);
			if ((num2 != sourisX || num3 != sourisY) && MathF.Sqrt((num4 - center.X) * (num4 - center.X) + (num5 - 10f - center.Y) * (num5 - 10f - center.Y)) > 80f)
			{
				continue;
			}
			float num6 = num5 - 22f - 2f - 6f - 2f;
			for (int num7 = list.Count - 1; num7 >= 0; num7--)
			{
				IMapItemInstance mapItemInstance = list[num7];
				if (DatabaseObject<ItemDescriptor>.TryGet(mapItemInstance.ItemId, out var databaseObject))
				{
					string texte = ((mapItemInstance.Quantity > 1) ? Strings.General.MapItemStackable.ToString(databaseObject.Name, Strings.FormatQuantityAbbreviated(mapItemInstance.Quantity)) : databaseObject.Name);
					Color color = CouleurRarete(JetDObjet.Rarete(databaseObject, mapItemInstance.ItemProperties));
					Vector2 vector = TexteMonde.Mesurer(texte, entityNameFont, taille);
					FloatRect plaque = new FloatRect(MathF.Round(num4 - vector.X / 2f) - 3f, MathF.Round(num6 - vector.Y - 1f), vector.X + 6f, vector.Y + 1f);
					while (PlaquesPosees.Any((FloatRect p) => p.X < plaque.X + plaque.Width + 2f && plaque.X < p.X + p.Width + 2f && p.Y < plaque.Y + plaque.Height + 1f && plaque.Y < p.Y + p.Height + 1f))
					{
						plaque.Y -= plaque.Height + 2f;
					}
					PlaquesPosees.Add(plaque);
					num6 = plaque.Y - 2f;
					Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), plaque, renderColor);
					Graphics.DrawGameTexture(Graphics.Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(plaque.X, plaque.Y + plaque.Height - 1f, plaque.Width, 1f), color);
					TexteMonde.Ecrire(texte, entityNameFont, taille, plaque.X + 3f, plaque.Y + 0.5f, color, new Color(255, 11, 14, 23));
				}
			}
		}
	}

	private void VerifierAnimees()
	{
		TuilesAnimees.Relire();
		if (_versionAnimees != TuilesAnimees.Version && _tileBuffersPerLayer.Count > 0)
		{
			DestroyVBOs();
		}
	}

	private void OublierAnimees(string couche)
	{
		_tuilesAnimees.Remove(couche);
	}

	private bool EnregistrerAnimee(string couche, int x, int y, Tile tuile)
	{
		if (!DatabaseObject<TilesetDescriptor>.TryGet(tuile.TilesetId, out var databaseObject) || !TuilesAnimees.Trouver(databaseObject.Name, tuile.X, tuile.Y, out TuilesAnimees.Animation animation, out int dx, out int dy))
		{
			return false;
		}
		if (!_tuilesAnimees.TryGetValue(couche, out List<TuileAnimee> value))
		{
			value = (_tuilesAnimees[couche] = new List<TuileAnimee>());
		}
		value.Add(new TuileAnimee(x, y, animation, dx, dy));
		return true;
	}

	private void DessinerAnimees(string couche)
	{
		if (!_tuilesAnimees.TryGetValue(couche, out List<TuileAnimee> value) || value.Count == 0)
		{
			return;
		}
		FloatRect worldViewport = Graphics.WorldViewport;
		float num = worldViewport.X - (float)_tileWidth;
		float num2 = worldViewport.X + worldViewport.Width + (float)_tileWidth;
		float num3 = worldViewport.Y - (float)_tileHeight;
		float num4 = worldViewport.Y + worldViewport.Height + (float)_tileHeight;
		long milliseconds = Timing.Global.Milliseconds;
		foreach (TuileAnimee item8 in value)
		{
			int num5 = X + item8.X * _tileWidth;
			int num6 = Y + item8.Y * _tileHeight;
			if ((float)(num5 + _tileWidth) < num || (float)num5 > num2 || (float)(num6 + _tileHeight) < num3 || (float)num6 > num4)
			{
				continue;
			}
			(string Planche, int X, int Y) tuple = item8.Animation.Images[0];
			string item = tuple.Planche;
			int item2 = tuple.X;
			int item3 = tuple.Y;
			int num7 = 0;
			int num8 = _tileHeight;
			(int, int, int)? tuple2 = Meteo.DefilementDeChute(item, item2 + item8.Dx, item3 + item8.Dy);
			if (tuple2.HasValue)
			{
				(int, int, int) valueOrDefault = tuple2.GetValueOrDefault();
				IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Tileset, item);
				if (texture != null)
				{
					int num9 = Meteo.GlissementDesChutes(_tileHeight);
					float x = (item2 + item8.Dx) * _tileWidth;
					float num10 = valueOrDefault.Item3 * _tileHeight;
					var (i, _, _) = valueOrDefault;
					int num12;
					for (; i < valueOrDefault.Item2; i += num12)
					{
						int num11 = ((i - num9) % _tileHeight + _tileHeight) % _tileHeight;
						num12 = Math.Min(valueOrDefault.Item2 - i, _tileHeight - num11);
						Graphics.DrawGameTexture(texture, new FloatRect(x, num10 + (float)num11, _tileWidth, num12), new FloatRect(num5, num6 + i, _tileWidth, num12), Color.White);
					}
					if (valueOrDefault.Item1 <= 0)
					{
						int item4 = valueOrDefault.Item2;
						int tileHeight = _tileHeight;
						num8 = tileHeight;
						num7 = item4;
					}
					else
					{
						int tileHeight = valueOrDefault.Item1;
						num8 = tileHeight;
						num7 = 0;
					}
					if (num7 >= num8)
					{
						continue;
					}
				}
			}
			(string Planche, int X, int Y) tuple4 = TuilesAnimees.Image(item8.Animation, item8.Dx, item8.Dy, milliseconds);
			string item5 = tuple4.Planche;
			int item6 = tuple4.X;
			int item7 = tuple4.Y;
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Tileset, item5);
			if (texture2 != null)
			{
				Graphics.DrawGameTexture(texture2, new FloatRect(item6 * _tileWidth, item7 * _tileHeight + num7, _tileWidth, num8 - num7), new FloatRect(num5, num6 + num7, _tileWidth, num8 - num7), Color.White);
			}
		}
	}

	private void OublierVent(string couche)
	{
		_tuilesVent.Remove(couche);
		_casesVent.Remove(couche);
	}

	private void OublierToutVent()
	{
		_tuilesVent.Clear();
		_casesVent.Clear();
	}

	private bool EstTuileVent(string couche, int x, int y)
	{
		if (x >= 0 && y >= 0 && x < _width && y < _height && _casesVent.TryGetValue(couche, out bool[,] value))
		{
			return value[x, y];
		}
		return false;
	}

	internal void AjouterFleurs(List<(float X, float Y)> sortie)
	{
		foreach (List<TuileVent> value in _tuilesVent.Values)
		{
			foreach (TuileVent item in value)
			{
				GenreVent genre = item.Genre;
				if (genre - 2 <= GenreVent.Arbre)
				{
					sortie.Add(((float)(X + item.X * _tileWidth) + (float)_tileWidth / 2f, (float)(Y + item.Y * _tileHeight) + (float)_tileHeight / 2f));
				}
			}
		}
	}

	private bool EnregistrerVent(string couche, int x, int y, Tile tuile, IGameTexture planche)
	{
		try
		{
			if (!Vent.Actif || !DatabaseObject<TilesetDescriptor>.TryGet(tuile.TilesetId, out var databaseObject))
			{
				return false;
			}
			ModeleArbre modele;
			GenreVent genreVent = Vent.Classer(databaseObject.Name, planche, tuile.X, tuile.Y, tuile.Autotile, out modele);
			if (genreVent == GenreVent.Aucun)
			{
				return false;
			}
			if (!_tuilesVent.TryGetValue(couche, out List<TuileVent> value))
			{
				value = (_tuilesVent[couche] = new List<TuileVent>());
				_casesVent[couche] = new bool[_width, _height];
			}
			int caseX = ((modele != null) ? (tuile.X % (modele.Largeur / _tileWidth)) : 0);
			int caseY = ((modele != null) ? (tuile.Y % (modele.Hauteur / _tileHeight)) : 0);
			value.Add(new TuileVent(genreVent, x, y, planche, tuile.X, tuile.Y, modele, caseX, caseY));
			_casesVent[couche][x, y] = true;
			return true;
		}
		catch (Exception exception)
		{
			Vent.Panne(exception);
			return false;
		}
	}

	private void DessinerVent(string couche)
	{
		if (!_tuilesVent.TryGetValue(couche, out List<TuileVent> value) || value.Count == 0)
		{
			return;
		}
		if (Vent.EnPanne)
		{
			DestroyVBOs();
			return;
		}
		try
		{
			int niveau = Vent.NiveauCarte(this);
			FloatRect worldViewport = Graphics.WorldViewport;
			float gauche = worldViewport.X - (float)_tileWidth;
			float droite = worldViewport.X + worldViewport.Width + (float)_tileWidth;
			float haut = worldViewport.Y - (float)_tileHeight;
			float bas = worldViewport.Y + worldViewport.Height + (float)_tileHeight;
			bool flag = false;
			foreach (TuileVent item in value)
			{
				if (ALEcran(item))
				{
					switch (item.Genre)
					{
					case GenreVent.Arbre:
						flag = true;
						break;
					case GenreVent.Fleur:
						DessinerFleur(couche, item, niveau);
						break;
					case GenreVent.Plante:
						DessinerPlante(item, niveau);
						break;
					}
				}
			}
			int num = 0;
			while (flag && num < 7)
			{
				foreach (TuileVent item2 in value)
				{
					if (item2.Genre == GenreVent.Arbre && ALEcran(item2))
					{
						DessinerArbre(item2, niveau, num);
					}
				}
				num++;
			}
			bool ALEcran(TuileVent tuile)
			{
				int num2 = X + tuile.X * _tileWidth;
				int num3 = Y + tuile.Y * _tileHeight;
				if ((float)(num2 + _tileWidth) >= gauche && (float)num2 <= droite && (float)(num3 + _tileHeight) >= haut)
				{
					return (float)num3 <= bas;
				}
				return false;
			}
		}
		catch (Exception exception)
		{
			Vent.Panne(exception);
		}
	}

	private static void Glisser(IGameTexture texture, FloatRect source, float x, float y, float largeur, float hauteur, float decalage)
	{
		Vent.Glisse(decalage, out var gauche, out var droite, out var part);
		float num4;
		float num5;
		float num6;
		if (!(part <= 0.5f))
		{
			float num = droite;
			float num2 = gauche;
			float num3 = 1f - part;
			num4 = num3;
			num5 = num2;
			num6 = num;
		}
		else
		{
			float num7 = gauche;
			float num3 = droite;
			float num2 = part;
			num4 = num2;
			num5 = num3;
			num6 = num7;
		}
		Graphics.DrawGameTexture(texture, source, new FloatRect(x + num6, y, largeur, hauteur), Color.White);
		if (num4 >= 1f / 64f)
		{
			Graphics.DrawGameTexture(texture, source, new FloatRect(x + num5, y, largeur, hauteur), new Color((int)(num4 * 255f), 255, 255, 255));
		}
	}

	private void DessinerArbre(TuileVent tuile, int niveau, int passe)
	{
		ModeleArbre modele = tuile.Modele;
		int origineX = X / _tileWidth + tuile.X - tuile.CaseX;
		int origineY = Y / _tileHeight + tuile.Y - tuile.CaseY;
		Vent.ArbreVivant arbreVivant = Vent.Arbre(modele, origineX, origineY, niveau);
		float decalage = arbreVivant.Position[0];
		float decalage2 = arbreVivant.Position[1];
		float decalage3 = arbreVivant.Position[2];
		int ecranX = X + tuile.X * _tileWidth;
		int ecranY = Y + tuile.Y * _tileHeight;
		int sourceX = tuile.CaseX * _tileWidth;
		int sourceY = tuile.CaseY * _tileHeight;
		switch (passe)
		{
		case 0:
			Calque(3, 0f);
			break;
		case 1:
			Calque(4, 0f);
			break;
		case 2:
			Calque(5, 0f);
			break;
		case 3:
			Calque(2, decalage3);
			break;
		case 4:
			Calque(1, decalage2);
			break;
		case 5:
			Calque(0, decalage);
			break;
		case 6:
		{
			int num = Vent.Reflet(modele, arbreVivant, origineX, niveau);
			if (num >= 0)
			{
				Calque(12 + num, decalage2);
				Calque(6 + num, decalage);
			}
			break;
		}
		}
		void Calque(int calque, float num2)
		{
			if (modele.Plein(calque, tuile.CaseX, tuile.CaseY))
			{
				FloatRect floatRect = new FloatRect(calque * modele.Largeur + sourceX, sourceY, _tileWidth, _tileHeight);
				if (num2 == 0f)
				{
					Graphics.DrawGameTexture(modele.Atlas, floatRect, new FloatRect(ecranX, ecranY, _tileWidth, _tileHeight), Color.White);
				}
				else
				{
					Glisser(modele.Atlas, floatRect, ecranX, ecranY, _tileWidth, _tileHeight, num2);
				}
			}
		}
	}

	private void DessinerFleur(string couche, TuileVent tuile, int niveau)
	{
		if (!Autotiles.Layers.TryGetValue(couche, out var value))
		{
			return;
		}
		(float, float, float, float, float) tuple = Vent.Reglage(niveau);
		int num = X / _tileWidth + tuile.X;
		int num2 = Y / _tileHeight + tuile.Y;
		int num3 = X + tuile.X * _tileWidth;
		int num4 = Y + tuile.Y * _tileHeight;
		double num5 = 0.8 * Math.Sin(Math.PI * 2.0 * Vent.Temps / ((double)tuple.Item3 * 0.55) - (double)num * 0.9 + (double)num2 * 0.35) + 1.2 * (double)Vent.Souffle(num3 + _tileHalfWidth);
		float decalage = (float)((double)tuple.Item2 * num5);
		Autotile autotile = value[tuile.X, tuile.Y];
		if (autotile.RenderState != 2)
		{
			Glisser(tuile.Planche, new FloatRect(tuile.SourceX * _tileWidth, tuile.SourceY * _tileHeight, _tileWidth, _tileHeight), num3, num4, _tileWidth, _tileHeight, decalage);
			return;
		}
		for (int i = 1; i <= 4; i++)
		{
			Point16i point16i = autotile.QuarterTile[i];
			Glisser(tuile.Planche, new FloatRect(point16i.X, point16i.Y, _tileHalfWidth, _tileHalfHeight), num3 + (i - 1) % 2 * _tileHalfWidth, num4 + (i - 1) / 2 * _tileHalfHeight, _tileHalfWidth, _tileHalfHeight, decalage);
		}
	}

	private void DessinerPlante(TuileVent tuile, int niveau)
	{
		(float Arbre, float Fleur, float Periode, float Penche, float Feuilles) r = Vent.Reglage(niveau);
		int num = X / _tileWidth + tuile.X;
		int num2 = Y / _tileHeight + tuile.Y;
		int num3 = X + tuile.X * _tileWidth;
		int num4 = Y + tuile.Y * _tileHeight;
		float vague = (float)(0.8 * Math.Sin(Math.PI * 2.0 * Vent.Temps / ((double)r.Periode * 0.6) - (double)num * 0.9 + (double)num2 * 0.35 + (double)tuile.SourceX) + 1.2 * (double)Vent.Souffle(num3 + _tileHalfWidth));
		int num5 = tuile.SourceX * _tileWidth;
		int num6 = tuile.SourceY * _tileHeight;
		float pied = (float)_tileHeight * 29f / 32f;
		float souplesse = (float)_tileHeight * 24f / 32f;
		int num7 = 0;
		(float, int) tuple = Cle(Decalage(0));
		for (int i = 1; i <= _tileHeight; i++)
		{
			if (i < _tileHeight)
			{
				(float, int) tuple2 = Cle(Decalage(i));
				(float, int) tuple3 = tuple;
				if (tuple2.Item1 == tuple3.Item1 && tuple2.Item2 == tuple3.Item2)
				{
					continue;
				}
			}
			Glisser(tuile.Planche, new FloatRect(num5, num6 + num7, _tileWidth, i - num7), num3, num4 + num7, _tileWidth, i - num7, Decalage(num7));
			num7 = i;
			if (i < _tileHeight)
			{
				tuple = Cle(Decalage(i));
			}
		}
		static (float, int) Cle(float decalage)
		{
			Vent.Glisse(decalage, out var gauche, out var _, out var part);
			return (gauche, (int)MathF.Round(part * 16f));
		}
		float Decalage(int rangee)
		{
			return r.Fleur * 1.3f * vague * Vent.Lisse((pied - (float)rangee) / souplesse);
		}
	}

	MapZone IMapInstance.ZoneType => base.ZoneType;
}
