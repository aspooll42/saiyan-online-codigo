using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject(false)]
public class CraftingActivityPacket : IntersectPacket
{
	[Key(0)]
	public Guid EntityId { get; set; }

	[Key(1)]
	public Guid RecipeId { get; set; }

	[Key(2)]
	public int Profession { get; set; } = -1;

	[Key(3)]
	public long Duration { get; set; }

	[Key(4)]
	public int Index { get; set; }

	[Key(5)]
	public int Count { get; set; }

	[Key(6)]
	public bool Finished { get; set; }

	public CraftingActivityPacket()
	{
	}

	public CraftingActivityPacket(Guid entityId, Guid recipeId, int profession, long duration, int index, int count, bool finished)
	{
		EntityId = entityId;
		RecipeId = recipeId;
		Profession = profession;
		Duration = duration;
		Index = index;
		Count = count;
		Finished = finished;
	}
}
