using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.Maps;
using Intersect.Client.General;
using Intersect.Client.Maps;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Models;
using Intersect.Utilities;

namespace Intersect.Client.Entities;

public class Critter : Entity
{
	private readonly MapCritterAttribute mAttribute;

	private long mLastMove = -1L;

	public Critter(MapInstance map, byte x, byte y, MapCritterAttribute att)
		: base(Guid.NewGuid(), null, EntityType.GlobalEntity)
	{
		mAttribute = att;
		Sprite = att.Sprite;
		if (DatabaseObject<AnimationDescriptor>.TryGet(att.AnimationId, out var databaseObject))
		{
			TryAddAnimation(new Animation(databaseObject, loopForever: true));
		}
		MapId = map?.Id ?? default(Guid);
		base.X = x;
		base.Y = y;
		if (mAttribute.Direction == 0)
		{
			base.DirectionFacing = Randomization.NextDirection();
		}
		else
		{
			base.DirectionFacing = (Direction)(mAttribute.Direction - 1);
		}
		base.Passable = !att.BlockPlayers;
	}

	public override bool Update()
	{
		if (base.Update())
		{
			if (mLastMove < Timing.Global.MillisecondsUtc)
			{
				switch (mAttribute.Movement)
				{
				case 0:
					MoveRandomly();
					break;
				case 1:
					base.DirectionFacing = Randomization.NextDirection();
					break;
				}
				mLastMove = Timing.Global.MillisecondsUtc + mAttribute.Frequency + Globals.Random.Next((int)((float)mAttribute.Frequency * 0.5f));
			}
			return true;
		}
		return false;
	}

	private void MoveRandomly()
	{
		DirectionMoving = Randomization.NextDirection();
		sbyte b = (sbyte)base.X;
		sbyte b2 = (sbyte)base.Y;
		IEntity blockedBy = null;
		if (base.IsMoving || base.MoveTimer >= Timing.Global.MillisecondsUtc)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		switch (DirectionMoving)
		{
		case Direction.Up:
			num = 0;
			num2 = -1;
			break;
		case Direction.Down:
			num = 0;
			num2 = 1;
			break;
		case Direction.Left:
			num = -1;
			num2 = 0;
			break;
		case Direction.Right:
			num = 1;
			num2 = 0;
			break;
		case Direction.UpLeft:
			num = -1;
			num2 = -1;
			break;
		case Direction.UpRight:
			num = 1;
			num2 = -1;
			break;
		case Direction.DownLeft:
			num = -1;
			num2 = 1;
			break;
		case Direction.DownRight:
			num = 1;
			num2 = 1;
			break;
		}
		if (num != 0 || num2 != 0)
		{
			int num3 = b + num;
			int num4 = b2 + num2;
			bool flag = -1 == IsTileBlocked(new Point(num3, num4), base.Z, MapId, ref blockedBy, ignoreAliveResources: true, ignoreDeadResources: true, mAttribute.IgnoreNpcAvoids);
			bool flag2 = PlayerOnTile(MapId, num3, num4);
			if (flag && num3 >= 0 && num3 < Options.Instance.Map.MapWidth && num4 >= 0 && num4 < Options.Instance.Map.MapHeight && !LigneBarreMonPas(num3, num4) && (!mAttribute.BlockPlayers || !flag2))
			{
				b += (sbyte)num;
				b2 += (sbyte)num2;
				base.IsMoving = true;
				base.DirectionFacing = DirectionMoving;
				if (num == 0)
				{
					base.OffsetX = 0f;
				}
				else
				{
					base.OffsetX = ((num > 0) ? (-Options.Instance.Map.TileWidth) : Options.Instance.Map.TileWidth);
				}
				if (num2 == 0)
				{
					base.OffsetY = 0f;
				}
				else
				{
					base.OffsetY = ((num2 > 0) ? (-Options.Instance.Map.TileHeight) : Options.Instance.Map.TileHeight);
				}
			}
		}
		if (base.IsMoving)
		{
			base.X = (byte)b;
			base.Y = (byte)b2;
			base.MoveTimer = Timing.Global.MillisecondsUtc + (long)GetMovementTime();
		}
		else if (DirectionMoving != base.DirectionFacing)
		{
			base.DirectionFacing = DirectionMoving;
		}
	}

	public bool PlayerOnTile(Guid mapId, int x, int y)
	{
		foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
		{
			if (entity.Value != null && entity.Value.MapId == mapId && entity.Value.X == x && entity.Value.Y == y && entity.Value is Player)
			{
				return true;
			}
		}
		return false;
	}

	public override HashSet<Entity>? DetermineRenderOrder(HashSet<Entity>? renderList, IMapInstance? map)
	{
		if (mAttribute.Layer == 1)
		{
			return base.DetermineRenderOrder(renderList, map);
		}
		renderList?.Remove(this);
		if (map == null || Globals.Me == null || Globals.Me.MapInstance == null || Globals.MapGrid == null)
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
					if (mAttribute.Layer == 0)
					{
						j--;
					}
					if (mAttribute.Layer == 2)
					{
						j++;
					}
					byte b = mRenderPriority;
					if (base.Z != 0)
					{
						b += 3;
					}
					HashSet<Entity> hashSet = null;
					if (j == gridY - 2)
					{
						hashSet = Graphics.RenderingEntities[b, base.Y];
					}
					else if (j == gridY - 1)
					{
						hashSet = Graphics.RenderingEntities[b, Options.Instance.Map.MapHeight + base.Y];
					}
					else if (j == gridY)
					{
						hashSet = Graphics.RenderingEntities[b, Options.Instance.Map.MapHeight * 2 + base.Y];
					}
					else if (j == gridY + 1)
					{
						hashSet = Graphics.RenderingEntities[b, Options.Instance.Map.MapHeight * 3 + base.Y];
					}
					else if (j == gridY + 2)
					{
						hashSet = Graphics.RenderingEntities[b, Options.Instance.Map.MapHeight * 4 + base.Y];
					}
					hashSet?.Add(this);
					renderList = hashSet;
					return renderList;
				}
			}
		}
		return renderList;
	}

	public override float GetMovementTime()
	{
		return mAttribute.Speed;
	}
}
