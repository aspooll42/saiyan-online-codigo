using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Networking;
using Intersect.Framework.Core.Market;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game;

public static class MarketPrices
{
	private const int MaxItemsPerRequest = 64;

	private static readonly Dictionary<Guid, MarketPriceInfo> _known = new Dictionary<Guid, MarketPriceInfo>();

	public static int SaleTaxPercent { get; private set; }

	public static event Action<MarketPricesResultPacket>? Received;

	public static bool TryGet(Guid itemId, out MarketPriceInfo info)
	{
		if (_known.TryGetValue(itemId, out MarketPriceInfo value))
		{
			info = value;
			return true;
		}
		info = null;
		return false;
	}

	public static void Request(IEnumerable<Guid> itemIds, int slot = -1)
	{
		Guid[] array = itemIds?.Where((Guid itemId) => itemId != Guid.Empty).Distinct().ToArray() ?? Array.Empty<Guid>();
		if (array.Length != 0 || slot >= 0)
		{
			int num = 0;
			do
			{
				int num2 = ((num == 0 && slot >= 0) ? 63 : 64);
				PacketSender.SendMarketPrices(array.Skip(num).Take(num2).ToArray(), (num == 0) ? slot : (-1));
				num += num2;
			}
			while (num < array.Length);
		}
	}

	internal static void OnResult(MarketPricesResultPacket packet)
	{
		SaleTaxPercent = packet.SaleTaxPercent;
		MarketPriceInfo[] array = packet.Prices ?? Array.Empty<MarketPriceInfo>();
		foreach (MarketPriceInfo marketPriceInfo in array)
		{
			if (marketPriceInfo != null && marketPriceInfo.ItemId != Guid.Empty)
			{
				_known[marketPriceInfo.ItemId] = marketPriceInfo;
			}
		}
		MarketPrices.Received?.Invoke(packet);
	}
}
