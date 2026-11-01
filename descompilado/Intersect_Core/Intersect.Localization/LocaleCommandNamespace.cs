using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Intersect.Localization;

public abstract class LocaleCommandNamespace : LocaleNamespace
{
	[JsonIgnore]
	public ImmutableList<LocaleCommand> CommandList { get; }

	[JsonIgnore]
	public IDictionary<string, LocaleCommand> CommandLookup { get; }

	protected LocaleCommandNamespace()
	{
		List<LocaleCommand> source = (from command in GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public).Select(delegate(MemberInfo member)
			{
				if (member is FieldInfo fieldInfo)
				{
					return fieldInfo.GetValue(this) as LocaleCommand;
				}
				return (member is PropertyInfo propertyInfo) ? (propertyInfo.GetValue(this) as LocaleCommand) : null;
			})
			where command != null
			select command).ToList();
		CommandList = source.ToImmutableList() ?? throw new InvalidOperationException();
		CommandLookup = source.Select(delegate(LocaleCommand command)
		{
			if (command == null)
			{
				throw new InvalidOperationException();
			}
			return new KeyValuePair<string, LocaleCommand>(command.Name, command);
		}).ToImmutableDictionary() ?? throw new InvalidOperationException();
	}
}
