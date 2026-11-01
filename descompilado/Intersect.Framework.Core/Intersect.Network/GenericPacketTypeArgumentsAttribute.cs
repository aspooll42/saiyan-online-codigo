using System;

namespace Intersect.Network;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GenericPacketTypeArgumentsAttribute : Attribute
{
	public Type[] TypeArguments { get; }

	public GenericPacketTypeArgumentsAttribute(params Type[] typeArguments)
	{
		TypeArguments = typeArguments;
	}
}
