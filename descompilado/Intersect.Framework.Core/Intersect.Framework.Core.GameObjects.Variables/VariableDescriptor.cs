using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Enums;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Variables;

public abstract class VariableDescriptor<TObject> : DatabaseObject<TObject> where TObject : VariableDescriptor<TObject>, IVariableDescriptor
{
	public VariableDataType DataType { get; set; } = VariableDataType.Boolean;

	public string? Folder { get; set; }

	public string TextId { get; set; }

	protected VariableDescriptor()
	{
	}

	[JsonConstructor]
	protected VariableDescriptor(Guid descriptorId)
		: base(descriptorId)
	{
	}

	public static string[] GetNamesByType(VariableDataType dataType)
	{
		return (from pair in DatabaseObject<TObject>.Lookup
			where pair.Value is IVariableDescriptor variableDescriptor && variableDescriptor.DataType == dataType
			orderby pair.Value.TimeCreated
			select pair.Value.Name).ToArray();
	}

	public static int ListIndex(Guid id, VariableDataType dataType)
	{
		return (from pair in DatabaseObject<TObject>.Lookup
			where pair.Value is IVariableDescriptor variableDescriptor && variableDescriptor.DataType == dataType
			orderby pair.Value.TimeCreated
			select pair).ToList().FindIndex((KeyValuePair<Guid, IDatabaseObject> pair) => pair.Value.Id == id);
	}

	public static Guid IdFromList(int listIndex, VariableDataType dataType)
	{
		if (listIndex < 0 || listIndex > GetNamesByType(dataType).Length)
		{
			return Guid.Empty;
		}
		return (from pair in DatabaseObject<TObject>.Lookup
			where pair.Value is IVariableDescriptor variableDescriptor && variableDescriptor.DataType == dataType
			orderby pair.Value.TimeCreated
			select pair).Skip(listIndex).First().Value.Id;
	}
}
