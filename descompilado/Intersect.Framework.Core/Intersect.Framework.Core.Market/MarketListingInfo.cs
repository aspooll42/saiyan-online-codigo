using System;
using Intersect.Framework.Core.GameObjects.Items;
using MessagePack;

namespace Intersect.Framework.Core.Market;

[MessagePackObject(false)]
public class MarketListingInfo
{
	[Key(0)]
	public Guid Id { get; set; }

	[Key(1)]
	public Guid ItemId { get; set; }

	[Key(2)]
	public int Quantity { get; set; }

	[Key(3)]
	public long UnitPrice { get; set; }

	[Key(4)]
	public string SellerName { get; set; } = string.Empty;

	[Key(5)]
	public ItemProperties? Properties { get; set; }

	[Key(6)]
	public long ExpiresAt { get; set; }

	[Key(7)]
	public MarketListingState State { get; set; }

	[Key(8)]
	public long PendingProceeds { get; set; }

	[Key(9)]
	public bool IsMine { get; set; }
}
