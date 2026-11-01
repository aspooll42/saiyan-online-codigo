using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Intersect.Framework.Reflection;
using Intersect.Plugins.Interfaces;
using Microsoft.Extensions.Logging;

namespace Intersect.Plugins.Helpers;

internal sealed class CommandLineHelper : PluginHelper, ICommandLineHelper
{
	private readonly Parser mParser;

	private readonly string[] mArgs;

	internal CommandLineHelper(ILogger logger, string[] args, Parser parser)
		: base(logger)
	{
		mArgs = args;
		mParser = parser;
	}

	public TArguments ParseArguments<TArguments>()
	{
		return mParser.ParseArguments<TArguments>(mArgs).MapResult((TArguments arguments) => arguments, HandleErrors<TArguments>);
	}

	private TArguments HandleErrors<TArguments>(IEnumerable<Error> errors)
	{
		List<Error> list = errors?.ToList();
		bool flag = list?.Any((Error error) => error?.StopsProcessing ?? false) ?? false;
		string text = string.Join(", ", (from error in list?.ToList()
			select error?.ToString()) ?? Array.Empty<string>());
		ArgumentException exception = new ArgumentException("Error parsing plugin arguments of type " + typeof(TArguments).FullName + ", received the following errors: " + text);
		if (flag)
		{
			base.Logger.LogCritical(exception, "Failed to parse {ArgumentsType}", typeof(TArguments).GetName(qualified: true));
		}
		else
		{
			base.Logger.LogWarning(exception, "Failed to parse {ArgumentsType}", typeof(TArguments).GetName(qualified: true));
		}
		return default(TArguments);
	}
}
