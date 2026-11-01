using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Core;
using Intersect.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intersect.Framework.Core.GameObjects.Variables;

public class VariableValue
{
	private dynamic mValue;

	public VariableDataType Type { get; set; }

	[NotMapped]
	public dynamic Value
	{
		get
		{
			return mValue;
		}
		set
		{
			SetValue(value);
		}
	}

	[JsonIgnore]
	public JObject Json
	{
		get
		{
			JObject jObject = new JObject();
			jObject.Add("Type", (byte)Type);
			jObject.Add("Value", Value);
			return jObject;
		}
		set
		{
			if (value.TryGetValue("Type", out JToken value2))
			{
				if (value2.Type != JTokenType.Integer)
				{
					throw new InvalidCastException("Expected 'Integer' for 'Type', received '" + value2.Type.ToString() + "'.");
				}
				if (value.TryGetValue("Value", out JToken value3))
				{
					Type = (VariableDataType)value2.Value<byte>();
					Value = ((value3.Type == JTokenType.Null) ? null : value3.Value<object>());
				}
			}
		}
	}

	[JsonIgnore]
	public bool Boolean
	{
		get
		{
			return (CanConvertTo<bool>(Value)) ? Convert.ToBoolean(Value) : ((object)false);
		}
		set
		{
			Type = VariableDataType.Boolean;
			mValue = value;
		}
	}

	[JsonIgnore]
	public long Integer
	{
		get
		{
			return (CanConvertTo<long>(Value)) ? Convert.ToInt64(Value) : ((object)0);
		}
		set
		{
			Type = VariableDataType.Integer;
			mValue = value;
		}
	}

	[JsonIgnore]
	public double Number
	{
		get
		{
			return (CanConvertTo<double>(Value)) ? Convert.ToDouble(Value) : ((object)0.0);
		}
		set
		{
			Type = VariableDataType.Number;
			mValue = value;
		}
	}

	[JsonIgnore]
	public string? String
	{
		get
		{
			return (CanConvertTo<string>(Value)) ? Convert.ToString(Value) : "";
		}
		set
		{
			Type = VariableDataType.String;
			mValue = value;
		}
	}

	public void SetValue(object value)
	{
		if (!(value is bool boolean))
		{
			if (!(value is long integer))
			{
				if (!(value is double number))
				{
					if (value is string text)
					{
						String = text;
					}
				}
				else
				{
					Number = number;
				}
			}
			else
			{
				Integer = integer;
			}
		}
		else
		{
			Boolean = boolean;
		}
		mValue = value;
	}

	public override string ToString()
	{
		return Value?.ToString() ?? "No Representation";
	}

	public string ToString(VariableDataType forceType)
	{
		if (Value == null)
		{
			switch (forceType)
			{
			case VariableDataType.Boolean:
				Boolean = false;
				break;
			case VariableDataType.Integer:
				Integer = 0L;
				break;
			case VariableDataType.Number:
				Number = 0.0;
				break;
			case VariableDataType.String:
				String = string.Empty;
				break;
			}
		}
		return Value?.ToString();
	}

	public static bool TryParse(string value, out JObject jObject)
	{
		try
		{
			jObject = JObject.Parse(value);
			JObject jObject2 = jObject;
			if (jObject2 != null && jObject2.Type != JTokenType.Null)
			{
				return true;
			}
			ApplicationContext.Context.Value?.Logger.LogWarning(new ArgumentNullException("jObject", "Invalid variable value stored in the database."), "Invalid variable value stored in the database:\n{Value}", value);
		}
		catch (Exception)
		{
			jObject = null;
		}
		return false;
	}

	public static bool CanConvertTo<T>(object input)
	{
		try
		{
			Convert.ChangeType(input, typeof(T));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static implicit operator VariableValue(bool value)
	{
		return new VariableValue
		{
			Type = VariableDataType.Boolean,
			Boolean = value
		};
	}

	public static implicit operator bool(VariableValue variableValue)
	{
		return variableValue.Boolean;
	}

	public static implicit operator VariableValue(long value)
	{
		return new VariableValue
		{
			Type = VariableDataType.String,
			Integer = value
		};
	}

	public static implicit operator long(VariableValue variableValue)
	{
		return variableValue.Integer;
	}

	public static implicit operator VariableValue(double value)
	{
		return new VariableValue
		{
			Type = VariableDataType.Number,
			Number = value
		};
	}

	public static implicit operator double(VariableValue variableValue)
	{
		return variableValue.Number;
	}

	public static implicit operator VariableValue(string value)
	{
		return new VariableValue
		{
			Type = VariableDataType.String,
			String = value
		};
	}

	public static implicit operator string(VariableValue variableValue)
	{
		return variableValue.String;
	}
}
