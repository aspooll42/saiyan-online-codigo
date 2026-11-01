using System;
using Intersect.Enums;
using MessagePack;

namespace Intersect.Admin.Actions;

[MessagePackObject(false)]
public class GiveItemAction : AdminAction
{
	[Key(1)]
	public override Intersect.Enums.AdminAction Action { get; } = Intersect.Enums.AdminAction.GiveItem;

	[Key(2)]
	public string Name { get; set; }

	[Key(3)]
	public Guid ItemId { get; set; }

	[Key(4)]
	public int Quantity { get; set; }

	public GiveItemAction()
	{
	}

	public GiveItemAction(string name, Guid itemId, int quantity)
	{
		Name = name;
		ItemId = itemId;
		Quantity = quantity;
	}
}
