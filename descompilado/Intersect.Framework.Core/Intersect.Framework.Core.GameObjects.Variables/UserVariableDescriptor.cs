using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Variables;

public class UserVariableDescriptor : VariableDescriptor<UserVariableDescriptor>, IVariableDescriptor, IDatabaseObject, INamedObject, IObject, IFolderable
{
	[JsonConstructor]
	public UserVariableDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New User Variable";
	}

	public UserVariableDescriptor()
	{
		base.Name = "New User Variable";
	}
}
