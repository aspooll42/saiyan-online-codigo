using System;
using Intersect.GameObjects;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.PlayerClass;

public class ClassSpell
{
	[JsonProperty]
	public Guid Id { get; set; }

	public int Level { get; set; }

	public SpellDescriptor Get()
	{
		return DatabaseObject<SpellDescriptor>.Get(Id);
	}
}
