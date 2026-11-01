using System;

namespace Intersect.Client.Framework.Graphics;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public sealed class VertexComponentAttribute : Attribute
{
	public VertexComponent Component { get; }

	public VertexComponentFormat Format { get; }

	public int Offset { get; }

	public int UsageIndex { get; }

	public VertexComponentAttribute(int offset, VertexComponent component, VertexComponentFormat format, int usageIndex)
	{
		Component = component;
		Format = format;
		Offset = offset;
		UsageIndex = usageIndex;
	}
}
