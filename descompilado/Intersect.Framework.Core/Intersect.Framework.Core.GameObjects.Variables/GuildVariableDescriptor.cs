using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Variables;

public class GuildVariableDescriptor : VariableDescriptor<GuildVariableDescriptor>, IVariableDescriptor, IDatabaseObject, INamedObject, IObject, IFolderable
{
	[JsonConstructor]
	public GuildVariableDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Guild Variable";
	}

	public GuildVariableDescriptor()
	{
		base.Name = "New Guild Variable";
	}
}
