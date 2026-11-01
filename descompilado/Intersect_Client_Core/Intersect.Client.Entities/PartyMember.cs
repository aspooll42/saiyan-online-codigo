using System;
using Intersect.Client.Framework.Entities;
using Intersect.Enums;

namespace Intersect.Client.Entities;

public class PartyMember : IPartyMember
{
	public Guid Id { get; set; }

	public int Level { get; set; }

	public long[] MaxVital { get; set; } = new long[Enum.GetValues<Vital>().Length];

	public string Name { get; set; }

	public long[] Vital { get; set; } = new long[Enum.GetValues<Vital>().Length];

	public string Sprite { get; set; } = string.Empty;

	public PartyMember(Guid id, string name, long[] vital, long[] maxVital, int level, string sprite)
	{
		Id = id;
		Name = name;
		Vital = vital;
		MaxVital = maxVital;
		Level = level;
		Sprite = sprite;
	}
}
