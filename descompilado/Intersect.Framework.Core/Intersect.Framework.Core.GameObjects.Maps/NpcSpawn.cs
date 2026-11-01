using System;

namespace Intersect.Framework.Core.GameObjects.Maps;

public class NpcSpawn
{
	public NpcSpawnDirection Direction;

	public Guid NpcId;

	public int X;

	public int Y;

	public Guid HiddenAfterQuestId;

	public NpcSpawn()
	{
	}

	public NpcSpawn(NpcSpawn copy)
	{
		NpcId = copy.NpcId;
		X = copy.X;
		Y = copy.Y;
		Direction = copy.Direction;
		HiddenAfterQuestId = copy.HiddenAfterQuestId;
	}
}
