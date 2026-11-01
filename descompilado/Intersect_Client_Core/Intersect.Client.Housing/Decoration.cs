using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Housing;

public static class Decoration
{
	private static bool _active;

	private static readonly Dictionary<Guid, int> PendingFromBag = new Dictionary<Guid, int>();

	public static Guid HeldItemId { get; private set; }

	public static HouseFurnitureInfo? HeldFromRoom { get; private set; }

	public static int Variant { get; private set; }

	public static bool Mirror { get; private set; }

	public static (int X, int Y)? Cursor { get; private set; }

	public static HouseFurnitureInfo? Hovered { get; private set; }

	public static (int X, int Y)? PointerCell { get; private set; }

	public static (string Text, bool Ok, long Until)? Note { get; private set; }

	public static FurnitureCategory? Filter { get; set; }

	public static bool Active
	{
		get
		{
			if (_active)
			{
				return Houses.CanDecorateHere;
			}
			return false;
		}
	}

	public static Guid HeldDescriptorItemId => HeldFromRoom?.ItemId ?? HeldItemId;

	public static FurnitureDescriptor? HeldDescriptor
	{
		get
		{
			if (!(HeldDescriptorItemId == Guid.Empty))
			{
				return DatabaseObject<ItemDescriptor>.Get(HeldDescriptorItemId)?.Furniture;
			}
			return null;
		}
	}

	public static bool IsHolding => HeldDescriptor != null;

	public static void Toggle()
	{
		if (Active)
		{
			Stop();
		}
		else
		{
			Start();
		}
	}

	public static void Start()
	{
		if (Houses.CanDecorateHere)
		{
			_active = true;
			Drop();
			HouseInterface.ShowDecoration(show: true);
		}
	}

	public static void Stop()
	{
		_active = false;
		Drop();
		Hovered = null;
		Note = null;
		HouseInterface.ShowDecoration(show: false);
	}

	public static void OnInterior()
	{
		PendingFromBag.Clear();
		HouseFurnitureInfo held = HeldFromRoom;
		if (held != null)
		{
			HouseInteriorPacket? interior = Houses.Interior;
			if (interior == null || interior.Furniture.All((HouseFurnitureInfo furniture) => furniture.Id != held.Id))
			{
				HeldFromRoom = null;
			}
		}
		if (!Houses.CanDecorateHere && _active)
		{
			Stop();
		}
	}

	public static void Alert(string text, bool ok)
	{
		Note = (text, ok, Timing.Global.MillisecondsUtc + 1800);
	}

	public static int BagCount(Guid itemId)
	{
		Player me = Globals.Me;
		if (me == null || itemId == Guid.Empty)
		{
			return 0;
		}
		int num = 0;
		IItem[] inventory = me.Inventory;
		foreach (IItem item in inventory)
		{
			if (item != null && item.ItemId == itemId)
			{
				num += Math.Max(1, item.Quantity);
			}
		}
		return Math.Max(0, num - PendingFromBag.GetValueOrDefault(itemId));
	}

	public static List<(ItemDescriptor Item, FurnitureDescriptor Furniture, int InBag)> Catalog()
	{
		HashSet<Guid> hashSet = new HashSet<Guid>();
		Player me = Globals.Me;
		if (me != null)
		{
			IItem[] inventory = me.Inventory;
			foreach (IItem item in inventory)
			{
				if (item != null && item.ItemId != Guid.Empty)
				{
					hashSet.Add(item.ItemId);
				}
			}
		}
		HouseFurnitureInfo[] array = Houses.Interior?.Furniture ?? Array.Empty<HouseFurnitureInfo>();
		foreach (HouseFurnitureInfo houseFurnitureInfo in array)
		{
			hashSet.Add(houseFurnitureInfo.ItemId);
		}
		return (from id in hashSet
			select DatabaseObject<ItemDescriptor>.Get(id) into itemDescriptor
			where itemDescriptor?.Furniture != null
			select (Item: itemDescriptor, Furniture: itemDescriptor.Furniture, InBag: BagCount(itemDescriptor.Id)) into entry
			where !Filter.HasValue || entry.Furniture.Category == Filter
			orderby entry.Furniture.Category
			select entry).ThenBy<(ItemDescriptor, FurnitureDescriptor, int), string>(((ItemDescriptor Item, FurnitureDescriptor Furniture, int InBag) entry) => entry.Item.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	public static IEnumerable<PlacedFurniture> Placed()
	{
		HouseFurnitureInfo[] array = Houses.Interior?.Furniture ?? Array.Empty<HouseFurnitureInfo>();
		foreach (HouseFurnitureInfo houseFurnitureInfo in array)
		{
			Guid id = houseFurnitureInfo.Id;
			Guid? obj = HeldFromRoom?.Id;
			if (!(id == obj))
			{
				FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo.ItemId)?.Furniture;
				if (furnitureDescriptor != null)
				{
					yield return new PlacedFurniture(houseFurnitureInfo.Id, furnitureDescriptor, houseFurnitureInfo.Variant, houseFurnitureInfo.Mirror, houseFurnitureInfo.X, houseFurnitureInfo.Y);
				}
			}
		}
	}

	private static HouseFurnitureInfo? FurnitureUnder(int cellX, int cellY)
	{
		HouseFurnitureInfo[] array = Houses.Interior?.Furniture ?? Array.Empty<HouseFurnitureInfo>();
		HouseFurnitureInfo houseFurnitureInfo = null;
		for (int num = array.Length - 1; num >= 0; num--)
		{
			HouseFurnitureInfo houseFurnitureInfo2 = array[num];
			Guid id = houseFurnitureInfo2.Id;
			Guid? obj = HeldFromRoom?.Id;
			if (!(id == obj))
			{
				FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo2.ItemId)?.Furniture;
				if (furnitureDescriptor != null && HousingRules.FurnitureImageContains(furnitureDescriptor, houseFurnitureInfo2.Variant, houseFurnitureInfo2.X, houseFurnitureInfo2.Y, cellX, cellY))
				{
					if (furnitureDescriptor.Placement != FurniturePlacement.Rug)
					{
						return houseFurnitureInfo2;
					}
					if (houseFurnitureInfo == null)
					{
						houseFurnitureInfo = houseFurnitureInfo2;
					}
				}
			}
		}
		return houseFurnitureInfo;
	}

	public static void UpdatePointer(bool overInterface)
	{
		PointerCell = null;
		Cursor = null;
		Hovered = null;
		if (!Active || overInterface)
		{
			return;
		}
		HouseInteriorPacket interior = Houses.Interior;
		if (interior != null && MapInstance.TryGet(interior.MapId, out MapInstance instance))
		{
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.GetMousePosition());
			int tileWidth = Options.Instance.Map.TileWidth;
			int tileHeight = Options.Instance.Map.TileHeight;
			int num = (int)Math.Floor((vector.X - (float)instance.X) / (float)tileWidth);
			int num2 = (int)Math.Floor((vector.Y - (float)instance.Y) / (float)tileHeight);
			PointerCell = (num, num2);
			FurnitureDescriptor heldDescriptor = HeldDescriptor;
			if (heldDescriptor != null)
			{
				FurnitureVariant furnitureVariant = heldDescriptor.VariantAt(Variant);
				int item = num - (Math.Max(1, furnitureVariant.Width) - 1) / 2;
				int item2 = ((heldDescriptor.Placement == FurniturePlacement.Wall) ? num2 : (num2 - (Math.Clamp(furnitureVariant.FootHeight, 1, Math.Max(1, furnitureVariant.Height)) - 1)));
				Cursor = (item, item2);
			}
			else
			{
				Hovered = FurnitureUnder(num, num2);
			}
		}
	}

	public static FurnitureVerdict CurrentVerdict()
	{
		FurnitureDescriptor heldDescriptor = HeldDescriptor;
		if (heldDescriptor != null)
		{
			(int, int)? cursor = Cursor;
			if (cursor.HasValue)
			{
				(int, int) valueOrDefault = cursor.GetValueOrDefault();
				return VerdictAt(heldDescriptor, Variant, valueOrDefault.Item1, valueOrDefault.Item2, HeldFromRoom?.Id ?? Guid.Empty);
			}
		}
		return FurnitureVerdict.OutsideRoom;
	}

	private static FurnitureVerdict VerdictAt(FurnitureDescriptor furniture, int variant, int x, int y, Guid ignoreId)
	{
		CapsuleDescriptor interiorCapsule = Houses.InteriorCapsule;
		if (interiorCapsule != null)
		{
			HouseInteriorPacket interior = Houses.Interior;
			if (interior != null && MapInstance.TryGet(interior.MapId, out MapInstance map))
			{
				HashSet<(int, int)> occupied = new HashSet<(int, int)>();
				if (HousingRules.BlocksMovement(furniture))
				{
					foreach (Entity value in Globals.Entities.Values)
					{
						if (value is Player player && player.MapId == map.Id)
						{
							occupied.Add((player.X, player.Y));
						}
					}
				}
				return HousingRules.Verdict(interiorCapsule, furniture, variant, x, y, Placed(), ignoreId, (int cx, int cy) => MapBlocked(map, cx, cy) || occupied.Contains((cx, cy)), (int cx, int cy) => InMap(map, cx, cy) && map.Attributes[cx, cy] is MapWarpAttribute);
			}
		}
		return FurnitureVerdict.OutsideRoom;
	}

	private static bool InMap(MapInstance map, int x, int y)
	{
		if (map.Attributes != null && x >= 0 && y >= 0 && x < map.Attributes.GetLength(0))
		{
			return y < map.Attributes.GetLength(1);
		}
		return false;
	}

	private static bool MapBlocked(MapInstance map, int x, int y)
	{
		bool flag = !InMap(map, x, y);
		bool flag2 = flag;
		if (!flag2)
		{
			MapAttribute mapAttribute = map.Attributes[x, y];
			bool flag3 = ((mapAttribute is MapBlockedAttribute || mapAttribute is MapAnimationAttribute { IsBlock: not false }) ? true : false);
			flag2 = flag3;
		}
		return flag2;
	}

	public static void PickFromBag(Guid itemId)
	{
		if (Active && BagCount(itemId) > 0)
		{
			bool flag = HeldItemId == itemId && HeldFromRoom == null;
			Drop();
			if (!flag)
			{
				HeldItemId = itemId;
			}
		}
	}

	public static void Click()
	{
		if (!Active)
		{
			return;
		}
		FurnitureDescriptor heldDescriptor = HeldDescriptor;
		if (heldDescriptor != null)
		{
			(int, int)? cursor = Cursor;
			if (cursor.HasValue)
			{
				(int, int) valueOrDefault = cursor.GetValueOrDefault();
				if (CurrentVerdict() != FurnitureVerdict.Ok)
				{
					return;
				}
				HouseFurnitureInfo heldFromRoom = HeldFromRoom;
				if (heldFromRoom != null)
				{
					PacketSender.SendHouseFurniturePlace(heldFromRoom.Id, Guid.Empty, valueOrDefault.Item1, valueOrDefault.Item2, Variant, Mirror);
					heldFromRoom.X = valueOrDefault.Item1;
					heldFromRoom.Y = valueOrDefault.Item2;
					heldFromRoom.Variant = Variant;
					heldFromRoom.Mirror = Mirror;
					HeldFromRoom = null;
				}
				else
				{
					Guid heldItemId = HeldItemId;
					PacketSender.SendHouseFurniturePlace(Guid.Empty, heldItemId, valueOrDefault.Item1, valueOrDefault.Item2, Variant, Mirror);
					PendingFromBag[heldItemId] = PendingFromBag.GetValueOrDefault(heldItemId) + 1;
					if (BagCount(heldItemId) <= 0)
					{
						HeldItemId = Guid.Empty;
					}
				}
				return;
			}
		}
		HouseFurnitureInfo hovered = Hovered;
		if (hovered != null)
		{
			HeldItemId = Guid.Empty;
			HeldFromRoom = hovered;
			Variant = hovered.Variant;
			Mirror = hovered.Mirror;
			Hovered = null;
		}
	}

	public static bool Drop()
	{
		bool isHolding = IsHolding;
		HeldItemId = Guid.Empty;
		HeldFromRoom = null;
		Variant = 0;
		Mirror = false;
		return isHolding;
	}

	public static void Rotate()
	{
		if (!Active)
		{
			return;
		}
		FurnitureDescriptor heldDescriptor = HeldDescriptor;
		if (heldDescriptor != null)
		{
			int newVariant;
			bool newMirror;
			RotateOutcome rotateOutcome = HousingRules.Rotate(heldDescriptor, Variant, Mirror, out newVariant, out newMirror);
			if (rotateOutcome == RotateOutcome.Refused)
			{
				Alert(Strings.Housing.NoTurn.ToString(Reason(heldDescriptor)), ok: false);
				return;
			}
			Variant = newVariant;
			Mirror = newMirror;
			Alert(Strings.Housing.Rotated.ToString(RotateLabel(heldDescriptor, rotateOutcome, newVariant)), ok: true);
			return;
		}
		HouseFurnitureInfo hovered = Hovered;
		if (hovered == null)
		{
			return;
		}
		FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(hovered.ItemId)?.Furniture;
		if (furnitureDescriptor == null)
		{
			return;
		}
		int newVariant2;
		bool newMirror2;
		RotateOutcome rotateOutcome2 = HousingRules.Rotate(furnitureDescriptor, hovered.Variant, hovered.Mirror, out newVariant2, out newMirror2);
		if (rotateOutcome2 == RotateOutcome.Refused)
		{
			Alert(Strings.Housing.NoTurn.ToString(Reason(furnitureDescriptor)), ok: false);
			return;
		}
		FurnitureVerdict furnitureVerdict = VerdictAt(furnitureDescriptor, newVariant2, hovered.X, hovered.Y, hovered.Id);
		if (furnitureVerdict != FurnitureVerdict.Ok)
		{
			Alert(Strings.Housing.NoRoomToTurn.ToString(Houses.VerdictText(furnitureVerdict).ToLowerInvariant()), ok: false);
			return;
		}
		PacketSender.SendHouseFurniturePlace(hovered.Id, Guid.Empty, hovered.X, hovered.Y, newVariant2, newMirror2);
		hovered.Variant = newVariant2;
		hovered.Mirror = newMirror2;
		Alert(Strings.Housing.Rotated.ToString(RotateLabel(furnitureDescriptor, rotateOutcome2, newVariant2)), ok: true);
	}

	public static void StoreHovered()
	{
		if (Active)
		{
			HouseFurnitureInfo houseFurnitureInfo = HeldFromRoom ?? Hovered;
			if (houseFurnitureInfo != null)
			{
				PacketSender.SendHouseFurnitureStore(houseFurnitureInfo.Id);
				HeldFromRoom = null;
				HeldItemId = Guid.Empty;
				Hovered = null;
			}
		}
	}

	public static void StoreAll()
	{
		if (Active)
		{
			Drop();
			PacketSender.SendHouseFurnitureStore(Guid.Empty);
		}
	}

	private static string Reason(FurnitureDescriptor furniture)
	{
		if (!string.IsNullOrWhiteSpace(furniture.NoMirrorReason))
		{
			return furniture.NoMirrorReason.Trim();
		}
		return Strings.Housing.NoTurnDefault;
	}

	private static string RotateLabel(FurnitureDescriptor furniture, RotateOutcome outcome, int variant)
	{
		return outcome switch
		{
			RotateOutcome.NextVariant => furniture.VariantAt(variant).Label, 
			RotateOutcome.Mirrored => Strings.Housing.Mirrored, 
			_ => Strings.Housing.Unmirrored, 
		};
	}

	public static string HeldSenseSuffix()
	{
		FurnitureDescriptor heldDescriptor = HeldDescriptor;
		if (heldDescriptor == null)
		{
			return string.Empty;
		}
		if (heldDescriptor.Variants.Count > 1 && !string.IsNullOrWhiteSpace(heldDescriptor.VariantAt(Variant).Label))
		{
			return " · " + heldDescriptor.VariantAt(Variant).Label;
		}
		if (!Mirror)
		{
			return string.Empty;
		}
		return " · " + Strings.Housing.Mirrored;
	}
}
