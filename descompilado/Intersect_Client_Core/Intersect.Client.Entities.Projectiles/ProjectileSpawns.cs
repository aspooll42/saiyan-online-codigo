using System;
using System.Collections.Generic;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.GameObjects;

namespace Intersect.Client.Entities.Projectiles;

public class ProjectileSpawns
{
	public Animation Anim;

	public bool AutoRotate;

	public Direction Dir;

	public int Distance;

	public Guid MapId;

	public float DepartX;

	public float DepartY;

	public float OffsetX;

	public float OffsetY;

	public ProjectileDescriptor ProjectileDescriptor;

	public int RangeBonus;

	public Guid SpawnMapId;

	public long SpawnTime = Timing.Global.Milliseconds;

	public int SpawnX;

	public int SpawnY;

	public long TransmissionTimer = Timing.Global.Milliseconds;

	public int X;

	public int Y;

	public int Z;

	public bool KiStarted;

	public bool KiImpacted;

	public float KiX;

	public float KiY;

	public float KiHeadingX;

	public float KiHeadingY;

	public HashSet<(Guid Carte, int Objet)>? KiDecorSous;

	public float KiStrength = 1f;

	public long KiClashedAt;

	public Guid KiTireur;

	public bool FlightStarted;

	public float FlightX;

	public float FlightY;

	public float FlightHeadingX;

	public float FlightHeadingY;

	public float FlightTravelled;

	public long FlightAt;

	public ProjectileSpawns(Direction dir, int x, int y, int z, Guid mapId, AnimationDescriptor animationDescriptor, bool autoRotate, ProjectileDescriptor projectileDescriptor, Entity parent)
	{
		X = x;
		Y = y;
		SpawnX = X;
		SpawnY = Y;
		Z = z;
		MapId = mapId;
		SpawnMapId = MapId;
		Dir = dir;
		Anim = new Animation(animationDescriptor, loopForever: true, autoRotate, Z, parent)
		{
			SousLesCimes = true
		};
		AutoRotate = autoRotate;
		ProjectileDescriptor = projectileDescriptor;
		TransmissionTimer = Timing.Global.Milliseconds + ProjectileDescriptor.Speed / ProjectileDescriptor.Range;
	}

	public void Dispose()
	{
		Anim.DisposeNextDraw();
	}
}
