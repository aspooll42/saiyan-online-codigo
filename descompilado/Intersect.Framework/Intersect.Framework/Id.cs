using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Intersect.Framework.Converters.Json;
using MessagePack;
using Newtonsoft.Json;

namespace Intersect.Framework;

[Serializable]
[DataContract]
[MessagePackObject(false)]
[Newtonsoft.Json.JsonConverter(typeof(IdNewtonsoftJsonConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(IdSystemJsonConverterFactory))]
public record struct Id<T>([property: Key(0)] Guid Guid)
{
	public static readonly Id<T> None;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Id<T> New()
	{
		return new Id<T>(Guid.NewGuid());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static explicit operator Guid(Id<T> id)
	{
		return id.Guid;
	}
}
