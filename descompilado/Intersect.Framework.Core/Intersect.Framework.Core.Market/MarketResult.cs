namespace Intersect.Framework.Core.Market;

public enum MarketResult : byte
{
	Listed,
	Bought,
	Cancelled,
	Collected,
	ItemsReturned,
	SoldNotice,
	ProceedsWaiting,
	NothingToCollect,
	Disabled,
	NoCurrency,
	NotEnoughCurrency,
	InventoryFull,
	InvalidItem,
	NotTradable,
	Equipped,
	TooManyListings,
	InvalidPrice,
	InvalidQuantity,
	ListingUnavailable,
	OwnListing,
	Error,
	LinkedAccount,
	AccountTooYoung
}
