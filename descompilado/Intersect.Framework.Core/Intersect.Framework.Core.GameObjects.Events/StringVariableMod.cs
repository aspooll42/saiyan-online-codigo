namespace Intersect.Framework.Core.GameObjects.Events;

public class StringVariableMod : VariableMod
{
	public VariableModType ModType { get; set; }

	public string Value { get; set; }

	public string Replace { get; set; }
}
