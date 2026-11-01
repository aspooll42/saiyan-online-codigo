using System;
using System.Numerics;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.GameObjects;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Entities.Projectiles;

public class Projectile : Entity
{
	private bool _isDisposing;

	private bool _isLoaded;

	private readonly object _lock = new object();

	private ProjectileDescriptor? _myBase;

	private Guid _owner;

	private int _rangeBonus;

	private int _quantity;

	private int _spawnCount;

	private int _spawnedAmount;

	private long _spawnTime;

	private int _totalSpawns;

	private Guid _projectileId;

	private ProjectileSpawns?[]? _spawns;

	private Guid _targetId;

	private const float DecalageDesMainsMax = 40f;

	public override bool CanBeAttacked => false;

	public Guid OwnerId => _owner;

	public Projectile(Guid id, ProjectileEntityPacket packet)
		: base(id, packet, EntityType.Projectile)
	{
		base.Vital[0] = 1L;
		base.MaxVital[0] = 1L;
		base.HideName = true;
		base.Passable = true;
		base.IsMoving = true;
	}

	public override void Load(EntityPacket? packet)
	{
		if (_isLoaded)
		{
			return;
		}
		base.Load(packet);
		if (packet == null)
		{
			return;
		}
		ProjectileEntityPacket projectileEntityPacket = (ProjectileEntityPacket)packet;
		_projectileId = projectileEntityPacket.ProjectileId;
		base.DirectionFacing = (Direction)projectileEntityPacket.ProjectileDirection;
		_targetId = projectileEntityPacket.TargetId;
		_owner = projectileEntityPacket.OwnerId;
		_rangeBonus = Math.Max(0, projectileEntityPacket.RangeBonus);
		_myBase = DatabaseObject<ProjectileDescriptor>.Get(_projectileId);
		if (Globals.Entities.TryGetValue(_owner, out Entity value))
		{
			value?.PlaySpellPose(400L);
		}
		if (_myBase != null)
		{
			for (int i = 0; i < 5; i++)
			{
				for (int j = 0; j < 5; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						if (_myBase.SpawnLocations[i, j].Directions[k])
						{
							_totalSpawns++;
						}
					}
				}
			}
			_totalSpawns *= _myBase.Quantity;
		}
		_spawns = new ProjectileSpawns[_totalSpawns];
		_isLoaded = true;
	}

	public override void Dispose()
	{
		if (_isDisposing)
		{
			return;
		}
		lock (_lock)
		{
			_isDisposing = true;
			if (_spawnedAmount == 0)
			{
				Update();
			}
			if (_spawns != null)
			{
				ProjectileSpawns[] spawns = _spawns;
				foreach (ProjectileSpawns projectileSpawns in spawns)
				{
					if (projectileSpawns != null)
					{
						KiBlast.OnEnd(projectileSpawns, _owner, _targetId);
					}
					projectileSpawns?.Anim?.DisposeNextDraw();
				}
			}
			GC.SuppressFinalize(this);
		}
	}

	private int FindSpawnAnimationData()
	{
		if (_myBase == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < _myBase.Animations.Count; i++)
		{
			int spawnRange = _myBase.Animations[i].SpawnRange;
			if (_quantity >= num && _quantity < spawnRange)
			{
				return i;
			}
			num = spawnRange;
		}
		return _myBase.Animations.Count - 1;
	}

	private void AddProjectileSpawns()
	{
		if (_myBase == null || _spawns == null)
		{
			return;
		}
		if (_quantity > 0 && Globals.Entities.TryGetValue(_owner, out Entity value) && value != null && !value.IsDisposed && value.MapId == MapId)
		{
			base.X = value.X;
			base.Y = value.Y;
			base.Z = value.Z;
		}
		int index = FindSpawnAnimationData();
		AnimationDescriptor animationDescriptor = DatabaseObject<AnimationDescriptor>.Get(_myBase.Animations[index].AnimationId);
		(float X, float Y) tuple = DecalageDesMains();
		float item = tuple.X;
		float item2 = tuple.Y;
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				for (int k = 0; k < 8; k++)
				{
					if (_myBase.SpawnLocations[i, j].Directions[k])
					{
						ProjectileSpawns projectileSpawns = new ProjectileSpawns(FindProjectileRotationDir(base.DirectionFacing, (Direction)k), (byte)(base.X + FindProjectileRotation(base.DirectionFacing, i - 2, j - 2, isXAxis: true)), (byte)(base.Y + FindProjectileRotation(base.DirectionFacing, i - 2, j - 2, isXAxis: false)), base.Z, MapId, animationDescriptor, _myBase.Animations[index].AutoRotate, _myBase, this)
						{
							RangeBonus = _rangeBonus,
						};
						ProjectileSpawns projectileSpawns2 = projectileSpawns;
						float departX = item;
						float departY = item2;
						projectileSpawns.DepartX = departX;
						projectileSpawns2.DepartY = departY;
						_spawns[_spawnedAmount] = projectileSpawns;
						if (Collided(_spawnedAmount))
						{
							TryRemoveSpawn(_spawnedAmount);
							_spawnCount--;
						}
						_spawnedAmount++;
						_spawnCount++;
					}
				}
			}
		}
		_quantity++;
		_spawnTime = Timing.Global.Milliseconds + _myBase.Delay;
	}

	private (float X, float Y) DecalageDesMains()
	{
		if (!Globals.Entities.TryGetValue(_owner, out Entity value) || value == null || value.IsDisposed || !value.PositionMonde(out var mondeX, out var mondeY) || !Intersect.Client.Maps.MapInstance.TryGet(MapId, out MapInstance instance))
		{
			return (X: 0f, Y: 0f);
		}
		float num = (float)(instance.GridX * DeplacementLibre.LargeurCarte + base.X * Options.Instance.Map.TileWidth) + (float)Options.Instance.Map.TileWidth / 2f;
		float num2 = (float)(instance.GridY * DeplacementLibre.HauteurCarte + base.Y * Options.Instance.Map.TileHeight) + (float)Options.Instance.Map.TileHeight / 2f;
		float num3 = mondeX - num;
		float num4 = mondeY - num2;
		if (MathF.Abs(num3) > 40f || MathF.Abs(num4) > 40f)
		{
			return (X: 0f, Y: 0f);
		}
		return (X: num3, Y: num4);
	}

	private static int FindProjectileRotation(Direction direction, int x, int y, bool isXAxis)
	{
		if (isXAxis)
		{
			switch (direction)
			{
			case Direction.Up:
				return x;
			case Direction.Down:
				return -x;
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				return y;
			case Direction.Right:
			case Direction.UpRight:
			case Direction.DownRight:
				return -y;
			default:
				return x;
			}
		}
		switch (direction)
		{
		case Direction.Up:
			return y;
		case Direction.Down:
			return -y;
		case Direction.Left:
		case Direction.UpLeft:
		case Direction.DownLeft:
			return -x;
		case Direction.Right:
		case Direction.UpRight:
		case Direction.DownRight:
			return x;
		default:
			return y;
		}
	}

	private static Direction FindProjectileRotationDir(Direction entityDir, Direction projectionDir)
	{
		return (Direction)ProjectileDescriptor.ProjectileRotationDir[(int)((int)entityDir * 8 + projectionDir)];
	}

	private static float GetRange(Direction direction, float range, bool isXAxis)
	{
		if (isXAxis)
		{
			switch (direction)
			{
			case Direction.Left:
			case Direction.UpLeft:
			case Direction.DownLeft:
				return 0f - range;
			case Direction.Right:
			case Direction.UpRight:
			case Direction.DownRight:
				return range;
			default:
				return 0f;
			}
		}
		switch (direction)
		{
		case Direction.Up:
		case Direction.UpLeft:
		case Direction.UpRight:
			return 0f - range;
		case Direction.Down:
		case Direction.DownRight:
		case Direction.DownLeft:
			return range;
		default:
			return 0f;
		}
	}

	private float GetDisplacement(long spawnTime)
	{
		if (_myBase == null)
		{
			return 0f;
		}
		long num = Timing.Global.Milliseconds - spawnTime;
		float num2 = (float)num / (float)_myBase.Speed;
		float value = num2 * (float)Options.Instance.Map.TileHeight * (float)_myBase.Range;
		int num3 = Options.Instance.Map.TileHeight * (_myBase.Range + _rangeBonus);
		return Math.Clamp(value, 0f, num3);
	}

	public override bool Update()
	{
		if (_myBase == null || _spawns == null)
		{
			return false;
		}
		lock (_lock)
		{
			Guid mapId = MapId;
			byte y = base.Y;
			if (!_isDisposing && _quantity < _myBase.Quantity && _spawnTime < Timing.Global.Milliseconds)
			{
				AddProjectileSpawns();
			}
			if (base.IsMoving)
			{
				for (int i = 0; i < _spawnedAmount; i++)
				{
					ProjectileSpawns projectileSpawns = _spawns[i];
					if (projectileSpawns == null || !Intersect.Client.Maps.MapInstance.TryGet(projectileSpawns.SpawnMapId, out MapInstance instance))
					{
						continue;
					}
					float num = (float)(instance.X + projectileSpawns.SpawnX * Options.Instance.Map.TileWidth) + (float)Options.Instance.Map.TileWidth / 2f + projectileSpawns.DepartX;
					float num2 = (float)(instance.Y + projectileSpawns.SpawnY * Options.Instance.Map.TileHeight) + (float)Options.Instance.Map.TileHeight / 2f + projectileSpawns.DepartY;
					Entity value = null;
					bool flag = _targetId != Guid.Empty && _targetId != _owner && Globals.Entities.TryGetValue(_targetId, out value) && value != null && !value.IsDisposed;
					if ((_myBase.HomingBehavior || _myBase.DirectShotBehavior) && (flag || projectileSpawns.FlightStarted))
					{
						Fly(projectileSpawns, num, num2, flag ? value : null);
						projectileSpawns.OffsetX = projectileSpawns.FlightX - num;
						projectileSpawns.OffsetY = projectileSpawns.FlightY - num2;
						projectileSpawns.Anim.SetRotation((float)(Math.Atan2(projectileSpawns.FlightHeadingY, projectileSpawns.FlightHeadingX) * (180.0 / Math.PI) + 90.0));
						if (_myBase.DirectShotBehavior)
						{
							_targetId = Guid.Empty;
						}
					}
					else
					{
						projectileSpawns.OffsetX = GetRange(projectileSpawns.Dir, GetDisplacement(projectileSpawns.SpawnTime), isXAxis: true);
						projectileSpawns.OffsetY = GetRange(projectileSpawns.Dir, GetDisplacement(projectileSpawns.SpawnTime), isXAxis: false);
						projectileSpawns.Anim.SetRotation(toggle: false);
					}
					float num3 = num + projectileSpawns.OffsetX;
					float num4 = num2 + projectileSpawns.OffsetY;
					Direction dir = (projectileSpawns.AutoRotate ? projectileSpawns.Dir : Direction.Up);
					projectileSpawns.Anim.SetPosition(num3, num4, base.X, base.Y, MapId, dir, projectileSpawns.Z);
					projectileSpawns.Anim.Update();
					KiBlast.OnFlight(projectileSpawns, num3, num4, _owner, _targetId);
				}
			}
			CheckForCollision();
		}
		return true;
	}

	private void Fly(ProjectileSpawns spawn, float baseX, float baseY, Entity? target)
	{
		if (_myBase == null)
		{
			return;
		}
		long milliseconds = Timing.Global.Milliseconds;
		int tileHeight = Options.Instance.Map.TileHeight;
		float num = (float)(_myBase.Range * tileHeight) / (float)Math.Max(1, _myBase.Speed);
		float num2 = (float)(_myBase.Range + _rangeBonus) * (float)tileHeight;
		if (!spawn.FlightStarted)
		{
			spawn.FlightStarted = true;
			spawn.FlightX = baseX;
			spawn.FlightY = baseY;
			spawn.FlightAt = milliseconds;
			float num3;
			float num4;
			if (target == null)
			{
				float range = GetRange(spawn.Dir, 1f, isXAxis: true);
				float range2 = GetRange(spawn.Dir, 1f, isXAxis: false);
				num3 = range2;
				num4 = range;
			}
			else
			{
				float num5 = TargetPoint(target).X - baseX;
				float range2 = TargetPoint(target).Y - baseY;
				num3 = range2;
				num4 = num5;
			}
			float num6 = MathF.Sqrt(num4 * num4 + num3 * num3);
			if (num6 < 0.5f)
			{
				float range3 = GetRange(spawn.Dir, 1f, isXAxis: true);
				num3 = GetRange(spawn.Dir, 1f, isXAxis: false);
				num4 = range3;
				num6 = MathF.Max(1f, MathF.Sqrt(num4 * num4 + num3 * num3));
			}
			spawn.FlightHeadingX = num4 / num6;
			spawn.FlightHeadingY = num3 / num6;
		}
		long num7 = Math.Clamp(milliseconds - spawn.FlightAt, 0L, 100L);
		spawn.FlightAt = milliseconds;
		if (num7 == 0L || spawn.FlightTravelled >= num2)
		{
			return;
		}
		if (target != null && _myBase.HomingBehavior)
		{
			(float X, float Y) tuple = TargetPoint(target);
			float item = tuple.X;
			float item2 = tuple.Y;
			float num8 = item - spawn.FlightX;
			float num9 = item2 - spawn.FlightY;
			float num10 = MathF.Sqrt(num8 * num8 + num9 * num9);
			if (num10 > 0.5f)
			{
				float num11 = MathF.Min(1f, (float)num7 / 45f);
				float num12 = spawn.FlightHeadingX + (num8 / num10 - spawn.FlightHeadingX) * num11;
				float num13 = spawn.FlightHeadingY + (num9 / num10 - spawn.FlightHeadingY) * num11;
				float num14 = MathF.Sqrt(num12 * num12 + num13 * num13);
				if (num14 > 0.001f)
				{
					spawn.FlightHeadingX = num12 / num14;
					spawn.FlightHeadingY = num13 / num14;
				}
			}
		}
		float num15 = MathF.Min(num * (float)num7, num2 - spawn.FlightTravelled);
		spawn.FlightX += spawn.FlightHeadingX * num15;
		spawn.FlightY += spawn.FlightHeadingY * num15;
		spawn.FlightTravelled += num15;
	}

	private static (float X, float Y) TargetPoint(Entity target)
	{
		FloatRect worldPos = target.WorldPos;
		if (worldPos.Width > 0f && worldPos.Height > 0f)
		{
			return (X: worldPos.X + worldPos.Width / 2f, Y: worldPos.Y + worldPos.Height - 16f);
		}
		Vector2 origin = target.Origin;
		return (X: origin.X, Y: origin.Y - (float)Options.Instance.Map.TileHeight / 2f);
	}

	public void CheckForCollision()
	{
		if (_myBase == null || _spawns == null || (_spawnCount == 0 && _quantity > _myBase.Quantity))
		{
			Globals.Entities[base.Id].Dispose();
			return;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		for (int i = 0; i < _spawnedAmount && i < _spawns.Length; i++)
		{
			ProjectileSpawns projectileSpawns = _spawns[i];
			if (projectileSpawns == null || !Intersect.Client.Maps.MapInstance.TryGet(projectileSpawns.SpawnMapId, out MapInstance instance))
			{
				continue;
			}
			float num;
			float num2;
			if (projectileSpawns.FlightStarted)
			{
				float flightHeadingX = projectileSpawns.FlightHeadingX;
				num = projectileSpawns.FlightHeadingY;
				num2 = flightHeadingX;
			}
			else
			{
				float range = GetRange(projectileSpawns.Dir, 1f, isXAxis: true);
				num = GetRange(projectileSpawns.Dir, 1f, isXAxis: false);
				num2 = range;
				float num3 = MathF.Sqrt(num2 * num2 + num * num);
				if (num3 > 0f)
				{
					num2 /= num3;
					num /= num3;
				}
			}
			int x = projectileSpawns.SpawnX + (int)MathF.Floor((projectileSpawns.OffsetX + num2 * 8f) / (float)tileWidth + 0.5f);
			int y = projectileSpawns.SpawnY + (int)MathF.Floor((projectileSpawns.OffsetY + num * 8f) / (float)tileHeight + 0.5f);
			MapInstance map;
			bool flag = TryResolveTile(ref x, ref y, instance, out map);
			float num4 = (projectileSpawns.FlightStarted ? projectileSpawns.FlightTravelled : GetDisplacement(projectileSpawns.SpawnTime));
			projectileSpawns.Distance = (int)(num4 / (float)tileHeight + 0.001f);
			if (flag && map.Id == projectileSpawns.MapId && x == projectileSpawns.X && y == projectileSpawns.Y && projectileSpawns.Distance < _myBase.Range + _rangeBonus)
			{
				continue;
			}
			if (flag)
			{
				projectileSpawns.X = x;
				projectileSpawns.Y = y;
				projectileSpawns.MapId = map.Id;
			}
			bool flag2 = !flag || projectileSpawns.Distance >= _myBase.Range + _rangeBonus || Collided(i);
			if (!flag2 && map.Attributes != null && projectileSpawns.X >= 0 && projectileSpawns.Y >= 0 && projectileSpawns.X < map.Attributes.GetLength(0) && projectileSpawns.Y < map.Attributes.GetLength(1))
			{
				MapAttribute mapAttribute = map.Attributes[projectileSpawns.X, projectileSpawns.Y];
				if (mapAttribute != null && mapAttribute.Type == MapAttributeType.ZDimension)
				{
					MapZDimensionAttribute mapZDimensionAttribute = (MapZDimensionAttribute)mapAttribute;
					if (mapZDimensionAttribute.BlockedLevel > 0 && projectileSpawns.Z == mapZDimensionAttribute.BlockedLevel - 1)
					{
						flag2 = true;
					}
					if (mapZDimensionAttribute.GatewayTo > 0)
					{
						projectileSpawns.Z = mapZDimensionAttribute.GatewayTo - 1;
					}
				}
			}
			if (flag2)
			{
				TryRemoveSpawn(i);
				_spawnCount--;
			}
		}
	}

	private static bool TryResolveTile(ref int x, ref int y, MapInstance start, out MapInstance map)
	{
		map = start;
		int mapWidth = Options.Instance.Map.MapWidth;
		int mapHeight = Options.Instance.Map.MapHeight;
		for (int i = 0; i < 4; i++)
		{
			if (x < 0)
			{
				if (!Intersect.Client.Maps.MapInstance.TryGet(map.Left, out MapInstance instance))
				{
					return false;
				}
				map = instance;
				x += mapWidth;
				continue;
			}
			if (x >= mapWidth)
			{
				if (!Intersect.Client.Maps.MapInstance.TryGet(map.Right, out MapInstance instance2))
				{
					return false;
				}
				map = instance2;
				x -= mapWidth;
				continue;
			}
			if (y < 0)
			{
				if (!Intersect.Client.Maps.MapInstance.TryGet(map.Up, out MapInstance instance3))
				{
					return false;
				}
				map = instance3;
				y += mapHeight;
				continue;
			}
			if (y >= mapHeight)
			{
				if (!Intersect.Client.Maps.MapInstance.TryGet(map.Down, out MapInstance instance4))
				{
					return false;
				}
				map = instance4;
				y -= mapHeight;
				continue;
			}
			return true;
		}
		if (x >= 0 && x < mapWidth && y >= 0)
		{
			return y < mapHeight;
		}
		return false;
	}

	public override void HoldForHitStop(long milliseconds)
	{
		base.HoldForHitStop(milliseconds);
		if (milliseconds <= 0 || _spawns == null)
		{
			return;
		}
		lock (_lock)
		{
			_spawnTime += milliseconds;
			ProjectileSpawns[] spawns = _spawns;
			foreach (ProjectileSpawns projectileSpawns in spawns)
			{
				if (projectileSpawns != null)
				{
					projectileSpawns.SpawnTime += milliseconds;
					projectileSpawns.TransmissionTimer += milliseconds;
					projectileSpawns.FlightAt += milliseconds;
				}
			}
		}
	}

	private bool Collided(int i)
	{
		if (Globals.Me == null || _spawns == null)
		{
			return true;
		}
		bool result = false;
		IEntity blockedBy = null;
		ProjectileSpawns projectileSpawns = _spawns[i];
		if (projectileSpawns == null)
		{
			return true;
		}
		switch (Globals.Me.IsTileBlocked(new Point(projectileSpawns.X, projectileSpawns.Y), base.Z, projectileSpawns.MapId, ref blockedBy, projectileSpawns.ProjectileDescriptor.IgnoreActiveResources, projectileSpawns.ProjectileDescriptor.IgnoreExhaustedResources, ignoreNpcAvoids: true, projectileTrigger: true))
		{
		case -1:
			return result;
		case -6:
			if (blockedBy != null && blockedBy.Id != _owner && Globals.Entities.ContainsKey(blockedBy.Id) && blockedBy is Resource)
			{
				result = true;
			}
			break;
		case -2:
			if (!projectileSpawns.ProjectileDescriptor.IgnoreMapBlocks && !Casse.LaissePasser(projectileSpawns.MapId, projectileSpawns.X, projectileSpawns.Y, Options.Instance.Casse.Percant(projectileSpawns.ProjectileDescriptor.Id)))
			{
				result = true;
			}
			break;
		case -3:
			if (!projectileSpawns.ProjectileDescriptor.IgnoreZDimension)
			{
				result = true;
			}
			break;
		case -5:
			result = true;
			break;
		}
		return result;
	}

	public override void Draw()
	{
		if (Intersect.Client.Maps.MapInstance.Get(MapId) != null)
		{
			Globals.GridMaps.ContainsKey(MapId);
		}
	}

	public ProjectileSpawns? SpawnAt(int spawnIndex)
	{
		if (spawnIndex < 0 || spawnIndex >= _spawnedAmount || _spawns == null || spawnIndex >= _spawns.Length)
		{
			return null;
		}
		return _spawns[spawnIndex];
	}

	public void SpawnDead(int spawnIndex, Guid? touche = null)
	{
		if (spawnIndex < _spawnedAmount)
		{
			ProjectileSpawns?[]? spawns = _spawns;
			if (((spawns != null) ? spawns[spawnIndex] : null) != null)
			{
				TryRemoveSpawn(spawnIndex, touche);
			}
		}
	}

	private void TryRemoveSpawn(int spawnIndex, Guid? touche = null)
	{
		if (spawnIndex < _spawnedAmount)
		{
			ProjectileSpawns?[]? spawns = _spawns;
			if (((spawns != null) ? spawns[spawnIndex] : null) != null)
			{
				KiBlast.OnEnd(_spawns[spawnIndex], _owner, _targetId, touche);
				_spawns[spawnIndex].Dispose();
				_spawns[spawnIndex] = null;
			}
		}
	}
}
