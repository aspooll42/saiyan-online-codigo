using System;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Events;

public class IntegerVariableMod : VariableMod
{
	public VariableModType ModType { get; set; }

	public long Value { get; set; }

	public long HighValue { get; set; }

	[JsonProperty("DupVariableId")]
	public Guid DuplicateVariableId { get; set; }
}
