using System;

namespace Intersect.Client.Spells;

public class Spell
{
	public Guid Id { get; set; }

	public int Rank { get; set; } = 1;

	public int Experience { get; set; }

	public Spell Clone()
	{
		return new Spell
		{
			Id = Id,
			Rank = Rank,
			Experience = Experience
		};
	}

	public void Load(Guid spellId)
	{
		Id = spellId;
	}

	public void Load(Guid spellId, int rank, int experience)
	{
		Id = spellId;
		Rank = Math.Max(1, rank);
		Experience = Math.Max(0, experience);
	}
}
