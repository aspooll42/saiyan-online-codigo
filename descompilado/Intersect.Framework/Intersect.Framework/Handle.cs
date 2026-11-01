using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Intersect.Framework.Converters.Json;
using Newtonsoft.Json;

namespace Intersect.Framework;

[Serializable]
[DataContract]
[System.Text.Json.Serialization.JsonConverter(typeof(HandleSystemJsonConverterFactory))]
[Newtonsoft.Json.JsonConverter(typeof(HandleNewtonsoftJsonConverter))]
public record struct Handle<T>(nint Pointer)
{
	public static readonly Handle<T> None;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator nint(Handle<T> handle)
	{
		return handle.Pointer;
	}
}
