using System;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using NCalc;

namespace Intersect.Server.Utilities;

public class ExperienceCurve
{
	public const string DEFAULT_EXPERIENCE_FORMULA = "Floor(BaseExp * Pow(Gain, Level - 1))";

	public const string PARAM_BASE_EXP = "BaseExp";

	public const string PARAM_GAIN = "Gain";

	public const string PARAM_LEVEL = "Level";

	public long BaseExperience { get; set; }

	public double Gain { get; set; }

	public ExperienceCurve(string source = "Floor(BaseExp * Pow(Gain, Level - 1))")
	{
		if (!string.IsNullOrEmpty(source))
		{
			BaseExperience = 100L;
			Gain = 1.5;
		}
	}

	protected virtual void Exp(FunctionArgs args)
	{
		if (args.Parameters == null || args.Parameters.Length < 3)
		{
			ApplicationContext.Context.Value?.Logger.LogError("Tried to execute Exp with fewer than three arguments.");
			args.Result = 0L;
		}
		Expression[] parameters = args.Parameters;
		int level = (int)(((parameters == null) ? null : parameters[0]?.Evaluate()) ?? ((object)(-1)));
		Expression[] parameters2 = args.Parameters;
		long baseExperience = (long)(((parameters2 == null) ? null : parameters2[1]?.Evaluate()) ?? ((object)(-1)));
		Expression[] parameters3 = args.Parameters;
		double gain = (double)(((parameters3 == null) ? null : parameters3[2]?.Evaluate()) ?? ((object)(-1)));
		args.Result = Calculate(level, baseExperience, gain);
	}

	public long Calculate(int level)
	{
		return Calculate(level, BaseExperience, Gain);
	}

	public long Calculate(int level, long baseExperience)
	{
		return Calculate(level, baseExperience, Gain);
	}

	public long Calculate(int level, long baseExperience, double gain)
	{
		return (long)Math.Floor((double)baseExperience * Math.Pow(gain, level - 1));
	}
}
