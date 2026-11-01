using System;
using System.Collections.Generic;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using NCalc;

namespace Intersect.Utilities;

public class Formula
{
	public static readonly List<string> SYSTEM_MATH_FUNCTIONS = new List<string>
	{
		"Abs", "Acos", "Asin", "Atan", "BigMul", "Ceiling", "Cos", "Cosh", "DivRem", "Exp",
		"Floor", "IEEERemainder", "Log", "Max", "Min", "Pow", "Round", "Sign", "Sin", "Sinh",
		"Sqrt", "Tan", "Tanh", "Truncate"
	};

	private string mLastSource;

	private string mSource;

	public string Source
	{
		get
		{
			return mSource;
		}
		set
		{
			if (!string.Equals(mSource, value, StringComparison.Ordinal))
			{
				mLastSource = mSource;
				mSource = value;
				if (!Load())
				{
					ApplicationContext.Context.Value?.Logger.LogWarning("Error loading formula from " + mSource + ".");
				}
				else
				{
					ApplicationContext.Context.Value?.Logger.LogDebug("Loaded new formula from " + mSource + ".");
				}
			}
		}
	}

	protected Expression Expression { get; set; }

	protected IDictionary<string, FormulaFunction> Functions { get; }

	protected IDictionary<string, FormulaParameter> Parameters { get; }

	public Formula(string source)
	{
		if (string.IsNullOrEmpty(source))
		{
			throw new ArgumentNullException("source");
		}
		Source = source;
		Functions = new Dictionary<string, FormulaFunction>();
		Parameters = new Dictionary<string, FormulaParameter>();
		if (Expression == null)
		{
			throw new ArgumentNullException("Expression");
		}
	}

	public bool Load()
	{
		if (Source == mLastSource)
		{
			return true;
		}
		Expression = new Expression(Source);
		Expression.EvaluateParameter += delegate(string name, ParameterArgs args)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (!Parameters.TryGetValue(name, out var value))
				{
					ApplicationContext.Context.Value?.Logger.LogError("Tried to access non-existent parameter '" + name + "' in a formula.");
				}
				else if (args == null)
				{
					ApplicationContext.Context.Value?.Logger.LogError("Formula parameter '" + name + "' arguments were null.");
				}
				else
				{
					value(args);
				}
			}
		};
		RegisterParameters();
		Expression.EvaluateFunction += delegate(string name, FunctionArgs args)
		{
			if (!string.IsNullOrEmpty(name))
			{
				if (!Functions.TryGetValue(name, out var value))
				{
					if (!SYSTEM_MATH_FUNCTIONS.Contains(name))
					{
						ApplicationContext.Context.Value?.Logger.LogError("Tried to access non-existent function '" + name + "' in a formula.");
					}
				}
				else if (args == null)
				{
					ApplicationContext.Context.Value?.Logger.LogError("Formula function '" + name + "' arguments were null.");
				}
				else
				{
					value(args);
				}
			}
		};
		RegisterFunctions();
		return true;
	}

	public virtual void RegisterFunctions()
	{
	}

	public bool RegisterFunction(string name, FormulaFunction function, bool shouldOverride = false)
	{
		if (Functions.ContainsKey(name) && !shouldOverride)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("Formula function '" + name + "' already exists, not overriding.");
			return false;
		}
		Functions[name] = function;
		return true;
	}

	public void RegisterCoreMathParameters()
	{
		RegisterParameter("PI", Math.PI);
		RegisterParameter("E", Math.E);
	}

	public virtual void RegisterParameters()
	{
	}

	public bool RegisterParameter(string name, object value, bool shouldOverride = false)
	{
		if (Expression.Parameters == null)
		{
			throw new ArgumentNullException("Parameters");
		}
		if (Expression.Parameters.ContainsKey(name) && !shouldOverride)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("Formula parameter '" + name + "' already exists, not overriding.");
			return false;
		}
		Expression.Parameters[name] = value;
		return true;
	}

	public bool RegisterEvaluatedParameter(string name, object value, bool shouldOverride = false)
	{
		if (Parameters.ContainsKey(name) && !shouldOverride)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug("Formula evaluated parameter '" + name + "' already exists,not overriding.");
			return false;
		}
		Parameters.Add(name, delegate(ParameterArgs args)
		{
			if (args == null)
			{
				ApplicationContext.Context.Value?.Logger.LogError("Formula function '" + name + "' arguments were null.");
			}
			else
			{
				args.Result = value;
			}
		});
		return Parameters.ContainsKey(name);
	}

	public T Evaluate<T>() where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
	{
		return (T)(Convert.ChangeType(Expression.Evaluate(), typeof(T)) ?? ((object)default(T)));
	}
}
