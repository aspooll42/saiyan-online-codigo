using System;

namespace Intersect.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface)]
public class ApiVisibilityAttribute : Attribute
{
	public ApiVisibility Visibility { get; }

	public ApiVisibilityAttribute(ApiVisibility visibility)
	{
		Visibility = visibility;
	}
}
