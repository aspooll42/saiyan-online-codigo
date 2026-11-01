using System;

namespace Intersect.Utilities;

public static class MeasurementExtensions
{
	public static readonly ProvideNumericalFormatString RoundAboveTen = (double timeInUnit) => (!(timeInUnit > 10.0)) ? "0.#" : "0.";

	public static string WithSuffix(this TimeSpan timeSpan, string? numericalFormat = null, ProvideNumericalFormatString? numericalFormatProvider = null, uint truncateDigits = 1u)
	{
		long ticks = timeSpan.Ticks;
		double num;
		string text;
		if (ticks < 36000000000L)
		{
			if (ticks < 10000000)
			{
				if (ticks >= 10)
				{
					if (ticks < 10000)
					{
						num = (double)timeSpan.Ticks / 10.0;
						text = MeasurementFormattingResources.TimeMicrosecondSuffix;
					}
					else
					{
						num = (double)timeSpan.Ticks / 10000.0;
						text = MeasurementFormattingResources.TimeMillisecondSuffix;
					}
				}
				else
				{
					num = timeSpan.Ticks;
					text = MeasurementFormattingResources.TimeNanosecondSuffix;
				}
			}
			else if (ticks < 600000000)
			{
				num = (double)timeSpan.Ticks / 10000000.0;
				text = MeasurementFormattingResources.TimeSecondSuffix;
			}
			else
			{
				num = (double)timeSpan.Ticks / 600000000.0;
				text = MeasurementFormattingResources.TimeMinuteSuffix;
			}
		}
		else if (ticks < 6048000000000L)
		{
			if (ticks < 864000000000L)
			{
				num = (double)timeSpan.Ticks / 36000000000.0;
				text = MeasurementFormattingResources.TimeHourSuffix;
			}
			else
			{
				num = (double)timeSpan.Ticks / 864000000000.0;
				text = MeasurementFormattingResources.TimeDaySuffix;
			}
		}
		else if (ticks < 315360000000000L)
		{
			num = (double)timeSpan.Ticks / 6048000000000.0;
			text = MeasurementFormattingResources.TimeWeekSuffix;
		}
		else
		{
			num = (double)timeSpan.Ticks / 6048000000000.0;
			text = MeasurementFormattingResources.TimeYearSuffix;
		}
		if (numericalFormat == null)
		{
			if (numericalFormatProvider == null)
			{
				numericalFormatProvider = RoundAboveTen;
			}
			numericalFormat = numericalFormatProvider(num);
		}
		if (truncateDigits != 0)
		{
			double num2 = Math.Pow(10.0, truncateDigits);
			num = Math.Truncate(num * num2) / num2;
		}
		return num.ToString(numericalFormat + "'" + text);
	}
}
