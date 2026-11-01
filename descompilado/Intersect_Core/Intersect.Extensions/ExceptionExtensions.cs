using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Intersect.Extensions;

public static class ExceptionExtensions
{
	[return: NotNull]
	public static string AsFullStackString(this Exception exception)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex != exception)
			{
				stringBuilder.Append("Caused by: ");
			}
			stringBuilder.AppendLine(ex.ToString());
		}
		return stringBuilder.ToString();
	}
}
