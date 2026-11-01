using System;

namespace Intersect.Extensions;

public static class DateTimeExtensions
{
	public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static DateTime Clone(this DateTime dateTime)
	{
		return new DateTime(dateTime.Ticks, dateTime.Kind);
	}

	public static DateTime ConvertKind(this DateTime dateTime, DateTimeKind kind)
	{
		if (kind == dateTime.Kind)
		{
			return dateTime;
		}
		return kind switch
		{
			DateTimeKind.Local => dateTime.ToLocalTime(), 
			DateTimeKind.Unspecified => new DateTime(dateTime.Ticks, DateTimeKind.Unspecified), 
			DateTimeKind.Utc => dateTime.ToUniversalTime(), 
			_ => throw new ArgumentOutOfRangeException("kind", kind, "Unknown DateTimeKind value."), 
		};
	}

	public static TimeSpan AsUnixTimeSpan(this DateTime dateTime)
	{
		DateTime dateTime2 = dateTime;
		if (dateTime.Kind != DateTimeKind.Utc)
		{
			dateTime2 = dateTime.ToUniversalTime();
		}
		return dateTime2 - UnixEpoch;
	}
}
