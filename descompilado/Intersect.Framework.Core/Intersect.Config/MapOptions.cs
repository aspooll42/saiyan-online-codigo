using System;
using System.Runtime.Serialization;
using Intersect.Framework.Annotations;
using Intersect.Framework.Core.GameObjects.Maps;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Intersect.Config;

[RequiresRestart]
public class MapOptions
{
	public const int DefaultMapWidth = 32;

	public const int DefaultMapHeight = 26;

	private bool _enableDiagonalMovement = true;

	private int _mapWidth = 32;

	private int _mapHeight = 26;

	private int _tileWidth = 32;

	private int _tileHeight = 32;

	private float _diagonalMovementCost = (float)Math.Sqrt(2.0);

	[JsonIgnore]
	public float MinimumWorldScale { get; private set; }

	[JsonIgnore]
	public float MaximumWorldScale { get; private set; }

	public bool DisableExpLossInArenaMaps { get; set; }

	public bool DisablePlayerDropsInArenaMaps { get; set; }

	public bool EnableCrossingDiagonalBlocks { get; set; }

	public bool EnableDiagonalMovement
	{
		get
		{
			return _enableDiagonalMovement;
		}
		set
		{
			_enableDiagonalMovement = value;
			MovementDirections = (_enableDiagonalMovement ? 8 : 4);
		}
	}

	public float DiagonalMovementCost
	{
		get
		{
			return _diagonalMovementCost;
		}
		set
		{
			_diagonalMovementCost = Math.Clamp(value, 1f, 2f);
		}
	}

	public string WorldMapSourceFile { get; set; } = string.Empty;

	[JsonConverter(typeof(StringEnumConverter))]
	public GameBorderStyle GameBorderStyle { get; set; }

	public int ItemAttributeRespawnTime { get; set; } = 15000;

	public LayerOptions Layers { get; set; } = new LayerOptions();

	public int MapHeight
	{
		get
		{
			return _mapHeight;
		}
		set
		{
			if (value != _mapHeight)
			{
				_mapHeight = value;
				RecalculateWorldScale();
			}
		}
	}

	public uint MapItemHeight { get; set; }

	public uint MapItemWidth { get; set; }

	public int MapWidth
	{
		get
		{
			return _mapWidth;
		}
		set
		{
			if (value != _mapWidth)
			{
				_mapWidth = value;
				RecalculateWorldScale();
			}
		}
	}

	[JsonIgnore]
	public int MovementDirections { get; private set; }

	public int TileHeight
	{
		get
		{
			return _tileHeight;
		}
		set
		{
			if (value != _tileHeight)
			{
				_tileHeight = value;
				RecalculateWorldScale();
			}
		}
	}

	public int TileWidth
	{
		get
		{
			return _tileWidth;
		}
		set
		{
			if (value != _tileWidth)
			{
				_tileWidth = value;
				RecalculateWorldScale();
			}
		}
	}

	public int TimeUntilMapCleanup { get; set; } = 30000;

	public bool ZDimensionVisible { get; set; }

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Validate();
	}

	public void Validate()
	{
		if (MapWidth < 10 || MapWidth > 64 || MapHeight < 10 || MapHeight > 64)
		{
			throw new Exception("Config Error: Map size out of bounds! (All values should be > 10 and < 64)");
		}
		EnableDiagonalMovement = _enableDiagonalMovement;
		MapItemWidth = ((MapItemWidth < 1) ? ((uint)TileWidth) : MapItemWidth);
		MapItemHeight = ((MapItemHeight < 1) ? ((uint)TileHeight) : MapItemHeight);
		RecalculateWorldScale();
	}

	private void RecalculateWorldScale()
	{
		float num = 32f / (float)Math.Min(TileWidth, TileHeight);
		float num2 = Math.Min(32f / Math.Max(1f, _mapWidth), 26f / Math.Max(1f, _mapHeight));
		float minimumWorldScale = num * num2;
		MinimumWorldScale = minimumWorldScale;
		MaximumWorldScale = Math.Max(MinimumWorldScale * 4f, num * 4f);
	}
}
