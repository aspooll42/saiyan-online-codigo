using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Crafting;

public class CraftingTableDescriptor : DatabaseObject<CraftingTableDescriptor>, IFolderable
{
	[NotMapped]
	public DbList<CraftingRecipeDescriptor> Crafts { get; set; } = new DbList<CraftingRecipeDescriptor>();

	[JsonIgnore]
	[Column("Crafts")]
	public string CraftsJson
	{
		get
		{
			return JsonConvert.SerializeObject(Crafts, Formatting.None);
		}
		protected set
		{
			Crafts = JsonConvert.DeserializeObject<DbList<CraftingRecipeDescriptor>>(value);
		}
	}

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public CraftingTableDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Table";
	}

	public CraftingTableDescriptor()
	{
		base.Name = "New Table";
	}
}
