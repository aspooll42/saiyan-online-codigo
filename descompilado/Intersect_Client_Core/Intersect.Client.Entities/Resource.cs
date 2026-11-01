using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Resources;
using Intersect.Models;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Entities;

public class Resource : Entity, IResource, IEntity, IDisposable
{
	private FloatRect _renderBoundsDest = FloatRect.Empty;

	private FloatRect _renderBoundsSrc = FloatRect.Empty;

	private bool _waitingForTilesets;

	private bool _recalculateRenderBounds;

	private bool _isDead;

	private int _maximumHealthForStates;

	private ResourceStateDescriptor? _currentState;

	private ResourceDescriptor? _descriptor;

	private AnimationDescriptor? _animationDescriptor;

	private IAnimation? _activeAnimation;

	private IAnimation? _stateAnimation;

	private readonly int _tileWidth = Options.Instance.Map.TileWidth;

	private readonly int _tileHeight = Options.Instance.Map.TileHeight;

	private readonly int _mapHeight = Options.Instance.Map.MapHeight;

	private static readonly Color VertRecoltable = new Color(255, 118, 224, 128);

	private static readonly Color RougeTropHaut = new Color(255, 240, 96, 84);

	private static readonly Color GrisLibre = new Color(255, 190, 198, 214);

	private static readonly Dictionary<IGameTexture, bool[]?> Masques = new Dictionary<IGameTexture, bool[]>();

	private const int Tolerance = 2;

	private FloatRect _hautSource = FloatRect.Empty;

	private int _hautLigne;

	private Color? _couleurSurvol;

	private FloatRect _lisereSource = FloatRect.Empty;

	private List<(int X, int Y)>? _lisere;

	public override bool CanBeAttacked => !IsDead;

	public ResourceStateDescriptor? CurrentState => _currentState;

	public ResourceDescriptor? Descriptor
	{
		get
		{
			return _descriptor;
		}
		set
		{
			if (value != _descriptor)
			{
				_descriptor = value;
				UpdateCurrentState();
			}
		}
	}

	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (value != _isDead)
			{
				_isDead = value;
			}
		}
	}

	public override string Sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (!(value == _sprite) && Descriptor != null)
			{
				_sprite = value;
				ReloadSpriteTexture();
			}
		}
	}

	internal FloatRect RenderSource => _renderBoundsSrc;

	internal FloatRect RenderDestination => _renderBoundsDest;

	protected override bool ShouldDrawHpBar => false;

	internal bool EstAbaissee => EstPlancheDeRecolte(CurrentState);

	internal Color CouleurDuLisere => _couleurSurvol ?? VertRecoltable;

	public bool CibleDeRecolteAuClic { get; set; }

	public Resource(Guid id, ResourceEntityPacket packet)
		: base(id, packet, EntityType.Resource)
	{
		mRenderPriority = 0;
	}

	private void ReloadSpriteTexture()
	{
		if (Descriptor == null)
		{
			return;
		}
		switch (_currentState?.TextureType)
		{
		case ResourceTextureSource.Tileset:
			if (GameContentManager.Current.TilesetsLoaded)
			{
				base.Texture = GameContentManager.Current.GetTexture(TextureType.Tileset, _currentState?.TextureName);
			}
			else
			{
				_waitingForTilesets = true;
			}
			break;
		case ResourceTextureSource.Resource:
			base.Texture = GameContentManager.Current.GetTexture(TextureType.Resource, _currentState?.TextureName);
			break;
		case ResourceTextureSource.Animation:
		{
			if (_stateAnimation?.Descriptor?.Id == _currentState.AnimationId)
			{
				return;
			}
			_stateAnimation?.Dispose();
			_stateAnimation = null;
			IMapInstance mapInstance = base.MapInstance;
			if (mapInstance == null)
			{
				return;
			}
			AnimationDescriptor animationDescriptor = _animationDescriptor;
			if (animationDescriptor == null)
			{
				return;
			}
			IAnimation animation = mapInstance.AddTileAnimation(animationDescriptor, base.X, base.Y, Direction.Up);
			if (animation != null && !animation.IsDisposed)
			{
				animation.InfiniteLoop = true;
			}
			_stateAnimation = animation;
			break;
		}
		}
		if (base.Texture == null)
		{
			base.Texture = Graphics.Renderer.WhitePixel;
		}
		_recalculateRenderBounds = true;
	}

	public override void Load(EntityPacket? packet)
	{
		base.Load(packet);
		_recalculateRenderBounds = true;
		if (!(packet is ResourceEntityPacket resourceEntityPacket))
		{
			return;
		}
		bool isDead = IsDead;
		bool flag = _descriptor != null;
		IsDead = resourceEntityPacket.IsDead;
		if (flag && isDead && !IsDead)
		{
			Recolte.SurRepousse(this);
		}
		Guid resourceId = resourceEntityPacket.ResourceId;
		bool flag2 = flag && !isDead && resourceEntityPacket.IsDead;
		if (!DatabaseObject<ResourceDescriptor>.TryGet(resourceId, out var databaseObject))
		{
			if (flag2)
			{
				ApplicationContext.CurrentContext.Logger.LogError("Unable to play resource exhaustion animation because resource {EntityId} ({EntityName}) is missing the descriptor ({DescriptorId})", base.Id, base.Name, resourceId);
			}
			return;
		}
		_maximumHealthForStates = (int)(databaseObject.UseExplicitMaxHealthForResourceStates ? databaseObject.MaxHp : base.MaxVital[0]);
		Descriptor = databaseObject;
		if (!flag2)
		{
			return;
		}
		Recolte.SurEpuisement(this);
		IMapInstance mapInstance = base.MapInstance;
		if (mapInstance != null)
		{
			IAnimation animation = mapInstance.AddTileAnimation(databaseObject.DeathAnimationId, base.X, base.Y, Direction.Up);
			if (animation != null && !animation.IsDisposed)
			{
				animation.Finished += OnAnimationDisposedOrFinished;
				animation.Disposed += OnAnimationDisposedOrFinished;
			}
			_activeAnimation = animation;
		}
		else
		{
			ApplicationContext.CurrentContext.Logger.LogError("Unable to play resource exhaustion animation because resource {EntityId} ({EntityName}) has no reference to the map instance for map {MapId}", base.Id, base.Name, MapId);
		}
	}

	private void OnAnimationDisposedOrFinished(IAnimation animation)
	{
		if (_activeAnimation == animation)
		{
			_activeAnimation = null;
			animation.Disposed -= OnAnimationDisposedOrFinished;
			animation.Finished -= OnAnimationDisposedOrFinished;
		}
	}

	public void UpdateCurrentState()
	{
		if (Descriptor == null)
		{
			return;
		}
		Dictionary<Guid, ResourceStateDescriptor> states = Descriptor.States;
		double currentHealthPercentage = Math.Floor((float)base.Vital[0] / (float)_maximumHealthForStates * 100f);
		ResourceStateDescriptor currentState = _currentState;
		if (currentState == null || !(currentHealthPercentage >= (double?)currentState?.MinimumHealth) || !(currentHealthPercentage <= (double?)currentState?.MaximumHealth))
		{
			currentState = states.Values.FirstOrDefault((ResourceStateDescriptor s) => currentHealthPercentage >= (double)s.MinimumHealth && currentHealthPercentage <= (double)s.MaximumHealth);
			ResourceStateDescriptor? currentState2 = _currentState;
			if (currentState2 != null && currentState2.TextureType == ResourceTextureSource.Animation && (currentState == null || currentState.TextureType != ResourceTextureSource.Animation))
			{
				_stateAnimation?.Dispose();
				_stateAnimation = null;
			}
			_currentState = currentState;
			if (currentState != null && currentState.TextureType == ResourceTextureSource.Animation && currentState.AnimationId != Guid.Empty)
			{
				DatabaseObject<AnimationDescriptor>.TryGet(currentState.AnimationId, out _animationDescriptor);
			}
			ReloadSpriteTexture();
		}
	}

	public override void Dispose()
	{
		if (base.RenderList != null)
		{
			base.RenderList.Remove(this);
			base.RenderList = null;
		}
		ClearAnimations();
		GC.SuppressFinalize(this);
		mDisposed = true;
	}

	public override bool Update()
	{
		if (mDisposed)
		{
			base.LatestMap = null;
			return false;
		}
		ResourceDescriptor descriptor = Descriptor;
		if (descriptor != null && descriptor.IsDeleted)
		{
			DatabaseObject<ResourceDescriptor>.TryGet(descriptor.Id, out var databaseObject);
			Descriptor = databaseObject;
		}
		if (!Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			base.LatestMap = instance;
			Globals.EntitiesToDispose.Add(base.Id);
			return false;
		}
		if (!instance.InView())
		{
			base.LatestMap = instance;
			return AttendreHorsDeVue();
		}
		_horsDeVueDepuis = 0L;
		if (_recalculateRenderBounds)
		{
			CalculateRenderBounds();
		}
		if (!Graphics.WorldViewport.IntersectsWith(_renderBoundsDest))
		{
			if (base.RenderList != null)
			{
				base.RenderList.Remove(this);
			}
			return true;
		}
		bool flag = base.Update();
		if (!flag && base.RenderList != null)
		{
			base.RenderList.Remove(this);
		}
		return flag;
	}

	public override HashSet<Entity>? DetermineRenderOrder(HashSet<Entity>? renderList, IMapInstance? map)
	{
		ResourceStateDescriptor currentState = CurrentState;
		if (currentState == null || !currentState.RenderBelowEntities)
		{
			if (!EstAbaissee)
			{
				return base.DetermineRenderOrder(renderList, map);
			}
			return RangerUneRangeePlusBas(renderList, map);
		}
		renderList?.Remove(this);
		if (map == null)
		{
			return null;
		}
		if (Globals.MapGrid == null)
		{
			return null;
		}
		if (Globals.Me?.MapInstance == null)
		{
			return null;
		}
		if (Graphics.RenderingEntities == null)
		{
			return null;
		}
		int gridX = Globals.Me.MapInstance.GridX;
		int gridY = Globals.Me.MapInstance.GridY;
		for (int i = gridX - 1; i <= gridX + 1; i++)
		{
			for (int j = gridY - 1; j <= gridY + 1; j++)
			{
				if (i >= 0 && i < Globals.MapGridWidth && j >= 0 && j < Globals.MapGridHeight && Globals.MapGrid[i, j] != Guid.Empty && Globals.MapGrid[i, j] == MapId)
				{
					byte b = mRenderPriority;
					if (base.Z != 0)
					{
						b += 3;
					}
					HashSet<Entity> hashSet = ((j == gridY - 1) ? Graphics.RenderingEntities[b, base.Y] : ((j != gridY) ? Graphics.RenderingEntities[b, _mapHeight * 2 + base.Y] : Graphics.RenderingEntities[b, _mapHeight + base.Y]));
					hashSet.Add(this);
					renderList = hashSet;
					return renderList;
				}
			}
		}
		return renderList;
	}

	private void CalculateRenderBounds()
	{
		if (Descriptor == null)
		{
			return;
		}
		IMapInstance mapInstance = base.MapInstance;
		if (mapInstance == null)
		{
			return;
		}
		if (_waitingForTilesets)
		{
			if (!GameContentManager.Current.TilesetsLoaded)
			{
				return;
			}
			ReloadSpriteTexture();
			_waitingForTilesets = false;
		}
		IGameTexture texture = base.Texture;
		if (texture == null)
		{
			return;
		}
		ResourceStateDescriptor currentState = CurrentState;
		if (currentState == null)
		{
			return;
		}
		_renderBoundsSrc.X = 0f;
		_renderBoundsSrc.Y = 0f;
		switch (currentState.TextureType)
		{
		default:
			return;
		case ResourceTextureSource.Resource:
			_renderBoundsSrc.Width = texture.Width;
			_renderBoundsSrc.Height = texture.Height;
			break;
		case ResourceTextureSource.Tileset:
		{
			ResourceStateDescriptor resourceStateDescriptor = null;
			if (IsDead && currentState != null && currentState.MaximumHealth == 0)
			{
				ResourceStateDescriptor resourceStateDescriptor2 = currentState;
				resourceStateDescriptor = resourceStateDescriptor2;
			}
			else if (currentState != null && currentState.MinimumHealth > 0)
			{
				ResourceStateDescriptor resourceStateDescriptor3 = currentState;
				resourceStateDescriptor = resourceStateDescriptor3;
			}
			_renderBoundsSrc = ((resourceStateDescriptor == null) ? default(FloatRect) : new FloatRect(resourceStateDescriptor.X * _tileWidth, resourceStateDescriptor.Y * _tileHeight, (resourceStateDescriptor.Width + 1) * _tileWidth, (resourceStateDescriptor.Height + 1) * _tileHeight));
			break;
		}
		case ResourceTextureSource.Animation:
			_renderBoundsSrc = default(FloatRect);
			break;
		}
		_renderBoundsDest.Width = _renderBoundsSrc.Width;
		_renderBoundsDest.Height = _renderBoundsSrc.Height;
		_renderBoundsDest.Y = (int)((float)(mapInstance.Y + base.Y * _tileHeight) + base.OffsetY);
		_renderBoundsDest.X = (int)((float)(mapInstance.X + base.X * _tileWidth) + base.OffsetX);
		if (_renderBoundsSrc.Height > (float)_tileHeight)
		{
			_renderBoundsDest.Y -= _renderBoundsSrc.Height - (float)_tileHeight;
		}
		if (_renderBoundsSrc.Width > (float)_tileWidth)
		{
			_renderBoundsDest.X -= (_renderBoundsSrc.Width - (float)_tileWidth) / 2f;
		}
		if (EstPlancheDeRecolte(currentState))
		{
			_renderBoundsDest.Y += _tileHeight;
		}
		_recalculateRenderBounds = false;
	}

	public override void Draw()
	{
		if (base.MapInstance != null && base.Texture != null && _activeAnimation == null)
		{
			DessinerSurbrillance();
			if (!Recolte.DessinerRessource(this, base.Texture, _renderBoundsSrc, _renderBoundsDest))
			{
				Graphics.DrawGameTexture(base.Texture, _renderBoundsSrc, _renderBoundsDest, Intersect.Color.White);
			}
		}
	}

	internal static bool EstPlancheDeRecolte(ResourceStateDescriptor? etat)
	{
		if (etat != null && etat.TextureType == ResourceTextureSource.Tileset)
		{
			string textureName = etat.TextureName;
			if (textureName != null)
			{
				return textureName.Contains("recolte", StringComparison.OrdinalIgnoreCase);
			}
		}
		return false;
	}

	private HashSet<Entity>? RangerUneRangeePlusBas(HashSet<Entity>? renderList, IMapInstance? map)
	{
		renderList?.Remove(this);
		if (map != null)
		{
			Guid[,] mapGrid = Globals.MapGrid;
			if (mapGrid != null)
			{
				HashSet<Entity>[,] renderingEntities = Graphics.RenderingEntities;
				if (renderingEntities != null)
				{
					IMapInstance mapInstance = Globals.Me?.MapInstance;
					if (mapInstance != null)
					{
						int gridX = mapInstance.GridX;
						int gridY = mapInstance.GridY;
						for (int i = Math.Max(0, gridX - 1); i <= Math.Min(mapGrid.GetLength(0) - 1, gridX + 1); i++)
						{
							for (int j = Math.Max(0, gridY - 1); j <= Math.Min(mapGrid.GetLength(1) - 1, gridY + 1); j++)
							{
								if (!(mapGrid[i, j] != MapId))
								{
									int num = mRenderPriority + ((base.Z != 0) ? 3 : 0);
									int num2 = _mapHeight * (j - gridY + 2) + base.Y + 1;
									if (num >= renderingEntities.GetLength(0) || num2 >= renderingEntities.GetLength(1))
									{
										return null;
									}
									HashSet<Entity> hashSet = renderingEntities[num, num2];
									hashSet.Add(this);
									return hashSet;
								}
							}
						}
						return renderList;
					}
				}
			}
		}
		return null;
	}

	internal FloatRect DestinationPour(FloatRect source, bool abaissee)
	{
		IMapInstance mapInstance = base.MapInstance;
		if (mapInstance == null)
		{
			return new FloatRect(0f, 0f, source.Width, source.Height);
		}
		int num = (int)((float)(mapInstance.X + base.X * _tileWidth) + base.OffsetX);
		int num2 = (int)((float)(mapInstance.Y + base.Y * _tileHeight) + base.OffsetY);
		if (source.Height > (float)_tileHeight)
		{
			num2 -= (int)(source.Height - (float)_tileHeight);
		}
		if (source.Width > (float)_tileWidth)
		{
			num -= (int)((source.Width - (float)_tileWidth) / 2f);
		}
		if (abaissee)
		{
			num2 += _tileHeight;
		}
		return new FloatRect(num, num2, source.Width, source.Height);
	}

	internal bool TrouverImageEpuisee(out IGameTexture texture, out FloatRect source, out FloatRect destination)
	{
		texture = null;
		source = default(FloatRect);
		destination = default(FloatRect);
		ResourceStateDescriptor resourceStateDescriptor = Descriptor?.States.Values.FirstOrDefault((ResourceStateDescriptor etat) => etat.MaximumHealth == 0);
		if (resourceStateDescriptor == null || string.IsNullOrEmpty(resourceStateDescriptor.TextureName))
		{
			return false;
		}
		switch (resourceStateDescriptor.TextureType)
		{
		case ResourceTextureSource.Tileset:
		{
			IGameTexture texture3 = Globals.ContentManager.GetTexture(TextureType.Tileset, resourceStateDescriptor.TextureName);
			if (texture3 == null)
			{
				return false;
			}
			texture = texture3;
			source = new FloatRect(resourceStateDescriptor.X * _tileWidth, resourceStateDescriptor.Y * _tileHeight, (resourceStateDescriptor.Width + 1) * _tileWidth, (resourceStateDescriptor.Height + 1) * _tileHeight);
			break;
		}
		case ResourceTextureSource.Resource:
		{
			IGameTexture texture2 = Globals.ContentManager.GetTexture(TextureType.Resource, resourceStateDescriptor.TextureName);
			if (texture2 == null)
			{
				return false;
			}
			texture = texture2;
			source = new FloatRect(0f, 0f, texture2.Width, texture2.Height);
			break;
		}
		default:
			return false;
		}
		destination = DestinationPour(source, EstPlancheDeRecolte(resourceStateDescriptor));
		return true;
	}

	public override void DrawName(Color? textColor, Color? borderColor = null, Color? backgroundColor = null)
	{
		base.IsHovered = false;
		_couleurSurvol = null;
		if (IsDead)
		{
			return;
		}
		ResourceDescriptor descriptor = Descriptor;
		if (descriptor != null && !(_renderBoundsDest.Width <= 0f) && Graphics.Renderer != null)
		{
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.MousePosition);
			base.IsHovered = _renderBoundsDest.Contains(vector.X, vector.Y) && !Intersect.Client.Interface.Interface.DoesMouseHitInterface() && PixelDessineSous(vector.X, vector.Y);
			if (base.IsHovered)
			{
				PoserLaPlaque(descriptor, _renderBoundsDest.X + _renderBoundsDest.Width / 2f, HautDessine() - 6f);
			}
		}
	}

	internal void SurvolerLeCoinDePeche()
	{
		base.IsHovered = false;
		_couleurSurvol = null;
		if (IsDead)
		{
			return;
		}
		ResourceDescriptor descriptor = Descriptor;
		if (descriptor != null && Graphics.Renderer != null && Recolte.CentreDuCoin(this, out var x, out var y))
		{
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.MousePosition);
			base.IsHovered = !Intersect.Client.Interface.Interface.DoesMouseHitInterface() && Recolte.SurLeBouillon(this, vector.X, vector.Y);
			if (base.IsHovered)
			{
				PoserLaPlaque(descriptor, x, y - 13f - 6f);
			}
		}
	}

	private void PoserLaPlaque(ResourceDescriptor fiche, float centreX, float bas)
	{
		string name = (string.IsNullOrWhiteSpace(fiche.Name) ? base.Name : fiche.Name);
		string header = null;
		Color color = GrisLibre;
		int num = Professions.Of(fiche);
		if (num >= 0)
		{
			int num2 = Math.Max(1, fiche.ProfessionLevel);
			string text = Professions.Name(num);
			if (num2 <= 1)
			{
				header = Strings.Ressources.Libre.ToString(text);
			}
			else
			{
				header = Strings.Ressources.Requis.ToString(text, num2);
				int num3 = Globals.Me?.ProfessionLevel(num) ?? 1;
				color = ((num3 >= num2) ? VertRecoltable : RougeTropHaut);
			}
		}
		_couleurSurvol = color;
		NamePlate.DrawAt(name, Intersect.Color.White, header, color, centreX, bas);
	}

	private float HautDessine()
	{
		IGameTexture texture = base.Texture;
		if (texture == null || _renderBoundsSrc.Width <= 0f || _renderBoundsSrc.Height <= 0f)
		{
			return _renderBoundsDest.Y;
		}
		if (_hautSource.X != _renderBoundsSrc.X || _hautSource.Y != _renderBoundsSrc.Y || _hautSource.Width != _renderBoundsSrc.Width || _hautSource.Height != _renderBoundsSrc.Height)
		{
			_hautSource = _renderBoundsSrc;
			_hautLigne = 0;
			if (!Masques.TryGetValue(texture, out bool[] value))
			{
				value = LireMasque(texture);
				Masques[texture] = value;
			}
			if (value != null)
			{
				int width = texture.Width;
				int num = Math.Max(0, (int)_renderBoundsSrc.X);
				int num2 = Math.Min(width, (int)(_renderBoundsSrc.X + _renderBoundsSrc.Width));
				int num3 = Math.Max(0, (int)_renderBoundsSrc.Y);
				int num4 = Math.Min(texture.Height, (int)(_renderBoundsSrc.Y + _renderBoundsSrc.Height));
				for (int i = num3; i < num4; i++)
				{
					if (_hautLigne != 0)
					{
						break;
					}
					for (int j = num; j < num2; j++)
					{
						if (value[i * width + j])
						{
							_hautLigne = i - num3;
							break;
						}
					}
				}
			}
		}
		return _renderBoundsDest.Y + (float)_hautLigne * _renderBoundsDest.Height / _renderBoundsSrc.Height;
	}

	private bool PixelDessineSous(float mondeX, float mondeY)
	{
		IGameTexture texture = base.Texture;
		if (texture == null || _renderBoundsSrc.Width <= 0f || _renderBoundsSrc.Height <= 0f)
		{
			return true;
		}
		if (!Masques.TryGetValue(texture, out bool[] value))
		{
			value = LireMasque(texture);
			Masques[texture] = value;
		}
		if (value == null)
		{
			return true;
		}
		int num = (int)(_renderBoundsSrc.X + (mondeX - _renderBoundsDest.X) * _renderBoundsSrc.Width / _renderBoundsDest.Width);
		int num2 = (int)(_renderBoundsSrc.Y + (mondeY - _renderBoundsDest.Y) * _renderBoundsSrc.Height / _renderBoundsDest.Height);
		int width = texture.Width;
		int height = texture.Height;
		int num3 = Math.Max((int)_renderBoundsSrc.X, num - 2);
		int num4 = Math.Min((int)(_renderBoundsSrc.X + _renderBoundsSrc.Width) - 1, num + 2);
		int num5 = Math.Max((int)_renderBoundsSrc.Y, num2 - 2);
		int num6 = Math.Min((int)(_renderBoundsSrc.Y + _renderBoundsSrc.Height) - 1, num2 + 2);
		for (int i = num5; i <= num6; i++)
		{
			if (i < 0 || i >= height)
			{
				continue;
			}
			for (int j = num3; j <= num4; j++)
			{
				if (j >= 0 && j < width && value[i * width + j])
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool[]? LireMasque(IGameTexture texture)
	{
		try
		{
			if (!texture.TryGetPixels(out Color[] pixels) || pixels.Length != texture.Width * texture.Height)
			{
				return null;
			}
			bool[] array = new bool[pixels.Length];
			for (int i = 0; i < pixels.Length; i++)
			{
				array[i] = pixels[i].A > 40;
			}
			return array;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void DessinerSurbrillance()
	{
		Color color = _couleurSurvol ?? VertRecoltable;
		if (!base.IsHovered && !CibleDeRecolteAuClic)
		{
			return;
		}
		IGameTexture texture = base.Texture;
		if (texture == null || _renderBoundsSrc.Width <= 0f || _renderBoundsSrc.Height <= 0f)
		{
			return;
		}
		GameRenderer renderer = Graphics.Renderer;
		if (renderer == null)
		{
			return;
		}
		if (_lisere == null || _lisereSource.X != _renderBoundsSrc.X || _lisereSource.Y != _renderBoundsSrc.Y || _lisereSource.Width != _renderBoundsSrc.Width || _lisereSource.Height != _renderBoundsSrc.Height)
		{
			_lisereSource = _renderBoundsSrc;
			_lisere = CalculerLisere(texture);
		}
		if (_lisere.Count == 0)
		{
			return;
		}
		float num = _renderBoundsDest.Width / _renderBoundsSrc.Width;
		float num2 = _renderBoundsDest.Height / _renderBoundsSrc.Height;
		Color renderColor = new Color(220, color.R, color.G, color.B);
		FloatRect srcRectangle = new FloatRect(0f, 0f, 1f, 1f);
		foreach (var (num3, num4) in _lisere)
		{
			Graphics.DrawGameTexture(renderer.WhitePixel, srcRectangle, new FloatRect(_renderBoundsDest.X + (float)num3 * num, _renderBoundsDest.Y + (float)num4 * num2, num, num2), renderColor);
		}
	}

	private List<(int X, int Y)> CalculerLisere(IGameTexture texture)
	{
		List<(int, int)> list = new List<(int, int)>();
		if (!Masques.TryGetValue(texture, out bool[] masque))
		{
			masque = LireMasque(texture);
			Masques[texture] = masque;
		}
		if (masque == null)
		{
			return list;
		}
		int largeur = texture.Width;
		int hauteur = texture.Height;
		int x0 = (int)_renderBoundsSrc.X;
		int y0 = (int)_renderBoundsSrc.Y;
		int x1 = x0 + (int)_renderBoundsSrc.Width;
		int y1 = y0 + (int)_renderBoundsSrc.Height;
		for (int i = y0 - 1; i <= y1; i++)
		{
			for (int j = x0 - 1; j <= x1; j++)
			{
				if (!Opaque(j, i) && (Opaque(j - 1, i) || Opaque(j + 1, i) || Opaque(j, i - 1) || Opaque(j, i + 1) || Opaque(j - 1, i - 1) || Opaque(j + 1, i - 1) || Opaque(j - 1, i + 1) || Opaque(j + 1, i + 1)))
				{
					list.Add((j - x0, i - y0));
				}
			}
		}
		return list;
		bool Opaque(int num, int num2)
		{
			if (num >= x0 && num < x1 && num2 >= y0 && num2 < y1 && num >= 0 && num2 >= 0 && num < largeur && num2 < hauteur)
			{
				return masque[num2 * largeur + num];
			}
			return false;
		}
	}
}
