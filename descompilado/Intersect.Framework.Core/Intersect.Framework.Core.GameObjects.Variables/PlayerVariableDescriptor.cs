using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Variables;

public class PlayerVariableDescriptor : VariableDescriptor<PlayerVariableDescriptor>, IVariableDescriptor, IDatabaseObject, INamedObject, IObject, IFolderable
{
	[JsonConstructor]
	public PlayerVariableDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Player Variable";
	}

	public PlayerVariableDescriptor()
	{
		base.Name = "New Player Variable";
	}
}
