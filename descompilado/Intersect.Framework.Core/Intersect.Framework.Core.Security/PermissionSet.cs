using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Intersect.Core;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.Extensions.Logging;

namespace Intersect.Framework.Core.Security;

[DebuggerDisplay("{Name,nq}")]
[MessagePackObject(false)]
[MessagePackFormatter(typeof(PermissionSetMessagePackFormatter))]
public sealed class PermissionSet
{
	public sealed class PermissionSetMessagePackFormatter : IMessagePackFormatter<PermissionSet>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, PermissionSet? value, MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}
			if (string.IsNullOrWhiteSpace(value.Name))
			{
				throw new ArgumentException("The PermissionSet Name must be non-empty");
			}
			string name = value.Name;
			writer.Write(name);
			writer.Write(value._inheritsFrom?.Name ?? value._inheritsFromName);
			Dictionary<Permission, bool> permissionGranted = value._permissionGranted;
			int count = permissionGranted.Count;
			int num = 0;
			writer.WriteMapHeader(count);
			foreach (KeyValuePair<Permission, bool> item in permissionGranted)
			{
				item.Deconstruct(out var key, out var value2);
				Permission permission = key;
				bool value3 = value2;
				num++;
				writer.Write(permission.Name);
				writer.Write(value3);
			}
			if (num == count)
			{
				return;
			}
			throw new MessagePackSerializationException($"There are supposed to be {count} permissions in the {"PermissionSet"} '{name}' but {num} were written. It appears the set was modified while being serialized.");
		}

		public PermissionSet? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}
			string name = reader.ReadString();
			string inheritsFromName = reader.ReadString();
			if (!reader.TryReadMapHeader(out var count))
			{
				throw new MessagePackSerializationException("Failed to read PermissionSet map header.");
			}
			PermissionSet permissionSet = new PermissionSet(name, inheritsFromName);
			permissionSet._permissionGranted.EnsureCapacity(count);
			while (permissionSet._permissionGranted.Count < count)
			{
				string text = reader.ReadString();
				if (text == null)
				{
					throw new MessagePackSerializationException($"The name of {permissionSet._permissionGranted.Count} is null, this isn't a valid state");
				}
				bool value = reader.ReadBoolean();
				Permission key = text;
				permissionSet._permissionGranted[key] = value;
			}
			UpdatePermissionSet(permissionSet);
			return permissionSet;
		}
	}

	private static string _activePermissionSetName = "default";

	private static PermissionSet? _activePermissionSet;

	private readonly Dictionary<Permission, bool> _permissionGranted = new Dictionary<Permission, bool>();

	private PermissionSet? _inheritsFrom;

	private readonly string? _inheritsFromName;

	private static readonly Dictionary<string, PermissionSet> KnownPermissionSets = new Dictionary<string, PermissionSet>();

	public const string NameDefault = "default";

	public const string NameAdmin = "admin";

	public static PermissionSet ActivePermissionSet
	{
		get
		{
			PermissionSet permissionSet = _activePermissionSet;
			if (permissionSet != null)
			{
				return permissionSet;
			}
			string activePermissionSetName = _activePermissionSetName;
			if (!TryGet(activePermissionSetName, out permissionSet))
			{
				ApplicationContext.CurrentContext.Logger.LogError("Unknown active permission set '{PermissionSetName}'", activePermissionSetName);
				return Default;
			}
			_activePermissionSet = permissionSet;
			return permissionSet;
		}
		set
		{
			if (value != _activePermissionSet)
			{
				SetActivePermissionSet(value, value.Name);
			}
		}
	}

	public static string ActivePermissionSetName
	{
		get
		{
			return _activePermissionSetName;
		}
		set
		{
			if (!(value == _activePermissionSetName))
			{
				SetActivePermissionSet(null, value);
			}
		}
	}

	public bool this[Permission permission]
	{
		get
		{
			return IsGranted(permission);
		}
		set
		{
			_permissionGranted[permission] = value;
		}
	}

	public string Name { get; init; }

	public static IReadOnlyCollection<PermissionSet> PermissionSets => KnownPermissionSets.Values;

	public static PermissionSet Default
	{
		get
		{
			if (KnownPermissionSets.TryGetValue("default", out var value))
			{
				return value;
			}
			value = new PermissionSet("default", (PermissionSet?)null);
			InsertPermissionSet(value);
			return value;
		}
	}

	public static PermissionSet Admin
	{
		get
		{
			if (KnownPermissionSets.TryGetValue("admin", out var value))
			{
				return value;
			}
			value = new PermissionSet("admin")
			{
				[Permission.EngineVersion] = true,
				[Permission.WindowAdmin] = true
			};
			InsertPermissionSet(value);
			return value;
		}
	}

	public static event ActivePermissionSetChangedHandler? ActivePermissionSetChanged;

	public static event PermissionSetChangedHandler? PermissionSetUpdated;

	private static void SetActivePermissionSet(PermissionSet? permissionSet, string permissionSetName)
	{
		ApplicationContext.Context.Value?.Logger.LogInformation("Updating active permission set to '{PermissionSetName}'", permissionSetName);
		_activePermissionSetName = permissionSetName;
		_activePermissionSet = permissionSet;
		PermissionSet.ActivePermissionSetChanged?.Invoke(permissionSetName);
	}

	private PermissionSet(string name, PermissionSet? inheritsFrom, string? inheritsFromName)
	{
		Name = name;
		_inheritsFrom = inheritsFrom;
		_inheritsFromName = inheritsFromName;
	}

	public PermissionSet(string name, PermissionSet? inheritsFrom)
		: this(name, inheritsFrom, inheritsFrom?.Name)
	{
	}

	public PermissionSet(string name, string inheritsFromName = "default")
		: this(name, string.IsNullOrWhiteSpace(inheritsFromName) ? null : KnownPermissionSets.GetValueOrDefault(inheritsFromName), inheritsFromName)
	{
	}

	public void ClearOverride(Permission permission)
	{
		_permissionGranted.Remove(permission);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool AreAllGranted(params Permission[] permissions)
	{
		return permissions.All(IsGranted);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsGranted(Permission permission)
	{
		if (_permissionGranted.TryGetValue(permission, out var value))
		{
			return value;
		}
		return _inheritsFrom?.IsGranted(permission) ?? false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsAnyGranted(params Permission[] permissions)
	{
		return permissions.Any(IsGranted);
	}

	public static bool HasPermissionSetUpdatedHandler(Delegate @delegate)
	{
		return PermissionSet.PermissionSetUpdated?.GetInvocationList().Contains(@delegate) ?? false;
	}

	private static void UpdatePermissionSet(PermissionSet permissionSet)
	{
		foreach (PermissionSet value in KnownPermissionSets.Values)
		{
			if (permissionSet.Name == value.Name)
			{
				value._inheritsFrom = permissionSet;
			}
		}
		InsertPermissionSet(permissionSet);
	}

	private static void InsertPermissionSet(PermissionSet permissionSet)
	{
		KnownPermissionSets[permissionSet.Name] = permissionSet;
		PermissionSet.PermissionSetUpdated?.Invoke(permissionSet);
	}

	public static bool TryGet(string permissionSetName, [NotNullWhen(true)] out PermissionSet? permissionSet)
	{
		return KnownPermissionSets.TryGetValue(permissionSetName, out permissionSet);
	}
}
