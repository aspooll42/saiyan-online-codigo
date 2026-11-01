using System;
using Newtonsoft.Json.Serialization;

namespace Intersect.Framework.Core.Serialization;

public sealed class AggregateContractResolver(IContractResolver fallbackContractResolver, params IRestrictedContractResolver[] contractResolvers) : IContractResolver
{
	private readonly IContractResolver _fallbackContractResolver = fallbackContractResolver;

	private readonly IRestrictedContractResolver[] _contractResolvers = contractResolvers;

	public JsonContract ResolveContract(Type type)
	{
		IRestrictedContractResolver[] contractResolvers = _contractResolvers;
		foreach (IRestrictedContractResolver restrictedContractResolver in contractResolvers)
		{
			if (restrictedContractResolver.SupportsType(type))
			{
				return restrictedContractResolver.ResolveContract(type);
			}
		}
		return fallbackContractResolver.ResolveContract(type);
	}
}
