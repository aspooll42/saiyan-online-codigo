using System;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Audio;
using Intersect.Client.Framework.Core.Sounds;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Config;

namespace Intersect.Client.Core.Sounds;

public class MapSound : Sound, IMapSound, ISound
{
	private readonly int mDistance;

	private readonly IEntity? mEntity;

	private Guid mMapId;

	private int mX;

	private int mY;

	private readonly MapOptions _mapOptions = Options.Instance.Map;

	private readonly int _tileWidth;

	private readonly int _tileHeight;

	private readonly int _mapPixelWidth;

	private readonly int _mapPixelHeight;

	public float Fade { get; set; } = 1f;

	public bool IgnoresSilence { get; set; }

	private float SilenceFactor
	{
		get
		{
			if (!IgnoresSilence)
			{
				return Math.Clamp(Audio.Silence, 0f, 1f);
			}
			return 1f;
		}
	}

	public MapSound(string filename, int x, int y, Guid mapId, bool loop, int loopInterval, int distance, IEntity? parent = null)
		: base(filename, loop, loopInterval)
	{
		if (!string.IsNullOrEmpty(filename) && mSound != null)
		{
			mDistance = distance;
			mX = x;
			mY = y;
			mMapId = mapId;
			mEntity = parent;
			mSound.Volume = 0;
			_tileWidth = _mapOptions.TileWidth;
			_tileHeight = _mapOptions.TileHeight;
			_mapPixelWidth = _mapOptions.MapWidth * _tileWidth;
			_mapPixelHeight = _mapOptions.MapHeight * _tileHeight;
		}
	}

	public void UpdatePosition(int x, int y, Guid mapId)
	{
		mX = x;
		mY = y;
		mMapId = mapId;
	}

	public override bool Update()
	{
		if (base.Update())
		{
			UpdateSoundVolume();
			return true;
		}
		return false;
	}

	private void UpdateSoundVolume()
	{
		if (mMapId == Guid.Empty)
		{
			GameAudioInstance gameAudioInstance = mSound;
			if (gameAudioInstance != null)
			{
				gameAudioInstance.Volume = 0;
			}
			return;
		}
		MapInstance mapInstance = MapInstance.Get(mMapId);
		if ((mapInstance == null && mEntity != Globals.Me) || Globals.Me == null || Globals.MapGrid == null)
		{
			Stop();
			return;
		}
		if (mX == -1 || mY == -1 || mDistance <= 0)
		{
			GameAudioInstance gameAudioInstance2 = mSound;
			if (gameAudioInstance2 != null)
			{
				gameAudioInstance2.Volume = (int)(100f * Math.Clamp(Fade, 0f, 1f) * SilenceFactor);
			}
			return;
		}
		float num = 0f;
		if (mDistance > 0 && Globals.GridMaps.ContainsKey(mMapId))
		{
			num = 100f - 100f * CalculateSoundDistance() / (float)(mDistance + 1);
			if (num < 0f)
			{
				num = 0f;
			}
		}
		GameAudioInstance gameAudioInstance3 = mSound;
		if (gameAudioInstance3 != null)
		{
			gameAudioInstance3.Volume = (int)(num * Math.Clamp(Fade, 0f, 1f) * SilenceFactor);
		}
	}

	private float CalculateSoundDistance()
	{
		Player me = Globals.Me;
		if (me == null)
		{
			return 0f;
		}
		Guid guid = mMapId;
		if (!MapInstance.TryGet(guid, out MapInstance instance))
		{
			return 0f;
		}
		Guid mapId = me.MapId;
		MapInstance instance2;
		if (mapId == guid)
		{
			instance2 = instance;
		}
		else if (!MapInstance.TryGet(mapId, out instance2))
		{
			return 0f;
		}
		Point point = new Point(instance2.X + me.X * _tileWidth + _tileWidth / 2, instance2.Y + me.Y * _tileHeight + _tileHeight / 2);
		if (mX == -1 || mY == -1 || mDistance == -1)
		{
			Rectangle rect = new Rectangle(instance.X, instance.Y, _mapPixelWidth, _mapPixelHeight);
			return DistancePointToRectangle(point, rect) / ((float)(_tileHeight + _tileWidth) / 2f);
		}
		Point point2 = new Point(instance.X + mX * _tileWidth + _tileWidth / 2, instance.Y + mY * _tileHeight + _tileHeight / 2);
		Point point3 = point - point2;
		return (float)Math.Sqrt(Math.Pow(point3.X, 2.0) + Math.Pow(point3.Y, 2.0)) / ((float)(_tileHeight + _tileWidth) / 2f);
	}

	public static float DistancePointToRectangle(Point point, Rectangle rect)
	{
		if (point.X < rect.X)
		{
			if (point.Y < rect.Y)
			{
				point.X -= rect.X;
				point.Y -= rect.Y;
				return (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
			}
			if (point.Y > rect.Y + rect.Height)
			{
				point.X -= rect.X;
				point.Y -= rect.Y + rect.Height;
				return (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
			}
			return rect.X - point.X;
		}
		if (point.X > rect.X + rect.Width)
		{
			if (point.Y < rect.Y)
			{
				point.X -= rect.X + rect.Width;
				point.Y -= rect.Y;
				return (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
			}
			if (point.Y > rect.Y + rect.Height)
			{
				point.X -= rect.X + rect.Width;
				point.Y -= rect.Y + rect.Height;
				return (float)Math.Sqrt(point.X * point.X + point.Y * point.Y);
			}
			return point.X - (rect.X + rect.Width);
		}
		if (point.Y < rect.Y)
		{
			return rect.Y - point.Y;
		}
		if (point.Y > rect.Y + rect.Height)
		{
			return point.Y - (rect.Y + rect.Height);
		}
		return 0f;
	}
}
