using System;
using System.Collections.Generic;
using System.Threading;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Utilities;

public static class Retry
{
	public delegate bool TryParameterlessAction<TResult>(out TResult result);

	private static void DumpExceptions(List<Exception> exceptions, string message)
	{
		foreach (Exception exception in exceptions)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, message);
		}
	}

	public static TResult Execute<TResult>(TryParameterlessAction<TResult> action, TimeSpan retryInterval, long retryCount = -1L, bool suppressExceptions = true, int consecutiveFailuresUntilAbort = 10)
	{
		if (retryInterval.TotalMilliseconds < 1000.0)
		{
			throw new InvalidOperationException("You should not use this class for small intervals.");
		}
		if (retryInterval.TotalMilliseconds < 2000.0)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning("You should probably not be using Retry if you need such short intervals.");
		}
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		TResult result = default(TResult);
		List<Exception> list = new List<Exception>();
		int num = 0;
		long num2 = retryCount;
		while (num2 == -1 || 0 < num2)
		{
			try
			{
				Thread.Sleep(retryInterval);
				if (action(out result))
				{
					return result;
				}
				num = 0;
				list.Clear();
			}
			catch (Exception item)
			{
				list.Add(item);
				if (++num >= consecutiveFailuresUntilAbort)
				{
					DumpExceptions(list, "Failed too many times consecutively, aborting.");
					throw new OperationCanceledException("Failed too many times consecutively, aborting.");
				}
			}
			if (--num2 == 0L)
			{
				break;
			}
		}
		if (list.Count <= 0 || suppressExceptions)
		{
			return result;
		}
		DumpExceptions(list, "Bad Retry state!");
		throw list.FindLast((Exception e) => true) ?? new InvalidOperationException("Bad Retry state!");
	}

	public static TResult Execute<TResult>(TryParameterlessAction<TResult> action, long retryIntervalMs, long retryCount = -1L, bool suppressExceptions = true, int consecutiveFailuresUntilAbort = 10)
	{
		return Execute(action, new TimeSpan(retryIntervalMs * 10000), retryCount, suppressExceptions, consecutiveFailuresUntilAbort);
	}
}
