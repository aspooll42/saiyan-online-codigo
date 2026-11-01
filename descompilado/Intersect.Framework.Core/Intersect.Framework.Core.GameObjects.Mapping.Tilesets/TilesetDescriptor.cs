using System;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Mapping.Tilesets;

public class TilesetDescriptor : DatabaseObject<TilesetDescriptor>
{
	public new string Name
	{
		get
		{
			return base.Name;
		}
		set
		{
			base.Name = value?.Trim().ToLower();
		}
	}

	[JsonConstructor]
	public TilesetDescriptor(Guid id)
		: base(id)
	{
		Name = string.Empty;
	}

	public TilesetDescriptor()
	{
		Name = string.Empty;
	}
}
