using Microsoft.EntityFrameworkCore;

namespace Intersect.Framework.Core.GameObjects.Items;

[Owned]
public class EffectData
{
	public ItemEffect Type { get; set; }

	public int Percentage { get; set; }

	public EffectData()
	{
		Type = ItemEffect.None;
		Percentage = 0;
	}

	public EffectData(ItemEffect type, int percentage)
	{
		Type = type;
		Percentage = percentage;
	}
}
