using System;
using System.Linq;
using System.Reflection;

namespace Intersect.Client.Framework.Content;

public static class ContentTypesExtensions
{
	public static Type GetAssetType(this ContentType contentType)
	{
		string text = contentType.ToString();
		MemberInfo memberInfo = typeof(ContentType).GetMember(text).FirstOrDefault();
		if (memberInfo == null)
		{
			throw new InvalidOperationException("ContentType missing expected member: " + text);
		}
		object obj = memberInfo.GetCustomAttributes(typeof(AssetTypeAttribute), inherit: true).FirstOrDefault();
		if (obj is AssetTypeAttribute assetTypeAttribute)
		{
			return assetTypeAttribute.Type;
		}
		throw new InvalidOperationException($"{"ContentType"} missing {"AssetTypeAttribute"} on member: {text}");
	}
}
