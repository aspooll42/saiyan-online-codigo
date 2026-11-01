using Intersect.Framework.Core.GameObjects.Items;

namespace Intersect.Client.Interface.Shared;

public sealed record QuantityPricing(ItemDescriptor? Currency, long UnitPrice, bool Earning);
