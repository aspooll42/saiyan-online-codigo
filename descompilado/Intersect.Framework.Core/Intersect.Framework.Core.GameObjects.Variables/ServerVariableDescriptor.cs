using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Variables;

public class ServerVariableDescriptor : VariableDescriptor<ServerVariableDescriptor>, IVariableDescriptor, IDatabaseObject, INamedObject, IObject, IFolderable
{
	[NotMapped]
	[JsonIgnore]
	public VariableValue Value { get; set; } = new VariableValue();

	[NotMapped]
	[JsonProperty("Value")]
	public dynamic ValueData
	{
		get
		{
			return Value.Value;
		}
		set
		{
			Value.Value = (object)value;
		}
	}

	[Column("Value")]
	[JsonIgnore]
	public string Json
	{
		get
		{
			return Value.Json.ToString(Formatting.None);
		}
		private set
		{
			if (VariableValue.TryParse(value, out var jObject))
			{
				Value.Json = jObject;
			}
		}
	}

	[JsonConstructor]
	public ServerVariableDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Global Variable";
	}

	public ServerVariableDescriptor()
	{
		base.Name = "New Global Variable";
	}
}
