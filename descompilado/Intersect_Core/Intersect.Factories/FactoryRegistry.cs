using System;
using System.Globalization;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Intersect.Properties;
using Microsoft.Extensions.Logging;

namespace Intersect.Factories;

public static class FactoryRegistry<TValue>
{
	public static IFactory<TValue> Factory { get; private set; }

	public static TValue Create(params object[] args)
	{
		if (Factory == null)
		{
			throw new InvalidOperationException("No factory registered for type: " + typeof(TValue).FullName);
		}
		return Factory.Create(args);
	}

	public static bool TryCreate(out TValue value, params object[] args)
	{
		try
		{
			value = Create(args);
			return true;
		}
		catch (InvalidOperationException exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Failed to create {TypeName}", typeof(TValue).GetName(qualified: true));
		}
		catch (Exception exception2)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception2, string.Format(CultureInfo.CurrentUICulture, ExceptionMessages.SwallowingExceptionFromWithQualifiedName, typeof(FactoryRegistry<TValue>).QualifiedGenericName(), "Create"));
		}
		value = default(TValue);
		return false;
	}

	public static bool RegisterFactory(IFactory<TValue> factory, bool overrideExisting = false)
	{
		if (Factory != null && !overrideExisting)
		{
			return false;
		}
		Factory = factory;
		return true;
	}
}
