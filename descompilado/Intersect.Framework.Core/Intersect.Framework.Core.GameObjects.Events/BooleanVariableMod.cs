using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Events;

public class BooleanVariableMod : VariableMod
{
	public bool Value { get; set; }

	public VariableType DupVariableType { get; set; }

	[JsonProperty("DupVariableId")]
	public Guid DuplicateVariableId { get; set; }
}
