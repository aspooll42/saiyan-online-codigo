using System;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.Framework.Core.GameObjects.Titles;

public class TitleDescriptor : DatabaseObject<TitleDescriptor>, IFolderable
{
	public string Description { get; set; } = string.Empty;

	[Column("Color")]
	[JsonIgnore]
	public string JsonColor
	{
		get
		{
			return JsonConvert.SerializeObject(Color);
		}
		set
		{
			Color = ((!string.IsNullOrWhiteSpace(value)) ? JsonConvert.DeserializeObject<Color>(value) : DefaultColor);
		}
	}

	[NotMapped]
	public Color Color { get; set; } = DefaultColor;

	public string Folder { get; set; } = string.Empty;

	public static Color DefaultColor => new Color(255, 176, 186, 206);

	[JsonConstructor]
	public TitleDescriptor(Guid id)
		: base(id)
	{
		base.Name = "Nouveau titre";
	}

	public TitleDescriptor()
	{
		base.Name = "Nouveau titre";
	}
}
