using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Models;
using Newtonsoft.Json;

namespace Intersect.GameObjects;

public class ShopDescriptor : DatabaseObject<ShopDescriptor>, IFolderable
{
	[NotMapped]
	public List<ShopItemDescriptor> BuyingItems { get; set; } = new List<ShopItemDescriptor>();

	[NotMapped]
	public List<ShopItemDescriptor> SellingItems { get; set; } = new List<ShopItemDescriptor>();

	public bool BuyingWhitelist { get; set; } = true;

	[Column("DefaultCurrency")]
	[JsonProperty]
	public Guid DefaultCurrencyId { get; set; }

	[NotMapped]
	[JsonIgnore]
	public ItemDescriptor DefaultCurrency
	{
		get
		{
			return DatabaseObject<ItemDescriptor>.Get(DefaultCurrencyId);
		}
		set
		{
			DefaultCurrencyId = value?.Id ?? Guid.Empty;
		}
	}

	[Column("BuyingItems")]
	[JsonIgnore]
	public string JsonBuyingItems
	{
		get
		{
			return JsonConvert.SerializeObject(BuyingItems);
		}
		set
		{
			BuyingItems = JsonConvert.DeserializeObject<List<ShopItemDescriptor>>(value);
		}
	}

	[Column("SellingItems")]
	[JsonIgnore]
	public string JsonSellingItems
	{
		get
		{
			return JsonConvert.SerializeObject(SellingItems);
		}
		set
		{
			SellingItems = JsonConvert.DeserializeObject<List<ShopItemDescriptor>>(value);
		}
	}

	public string BuySound { get; set; }

	public string SellSound { get; set; }

	public string Folder { get; set; } = string.Empty;

	[JsonConstructor]
	public ShopDescriptor(Guid id)
		: base(id)
	{
		base.Name = "New Shop";
	}

	public ShopDescriptor()
	{
		base.Name = "New Shop";
	}
}
