using System;
using System.Collections.Generic;
using Intersect.Client.Entities;
using Intersect.Client.General;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Models;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Housing;

public static class Houses
{
	public readonly record struct Plot(MapInstance Map, TerrainArea Terrain, HouseStateInfo? State, CapsuleDescriptor? Capsule, HouseFootprint Footprint)
	{
		public bool HasHouse => Capsule != null;

		public bool ForSale => State == null;
	}

	public enum TargetKind
	{
		None,
		BuyTerrain,
		BuyHouse,
		Door,
		Furniture
	}

	public readonly record struct Target(TargetKind Kind, Plot Plot, HouseFurnitureInfo? Furniture);

	public readonly record struct Prompt(string Text, string? Detail, bool Refused);

	private sealed record MapPlots(object? Attributes, int Version, List<Plot> Plots, HashSet<int> Blocked, int Width);

	private readonly record struct DoorFeedback(HouseResult Result, string Name, long Until);

	private static HouseStateInfo[] _states = Array.Empty<HouseStateInfo>();

	private static int _version;

	private static readonly Dictionary<Guid, MapPlots> PlotCache = new Dictionary<Guid, MapPlots>();

	private static HashSet<int> _furnitureCells = new HashSet<int>();

	private static DoorFeedback? _feedback;

	private static long _nextInteract;

	public static HouseInteriorPacket? Interior { get; private set; }

	public static long InteriorReceivedAt { get; private set; }

	public static long BoughtAt { get; private set; }

	public static string BoughtName { get; private set; } = string.Empty;

	public static bool InHouse
	{
		get
		{
			HouseInteriorPacket interior = Interior;
			if (interior != null)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					return me.MapId == interior.MapId;
				}
			}
			return false;
		}
	}

	public static CapsuleDescriptor? InteriorCapsule
	{
		get
		{
			HouseInteriorPacket interior = Interior;
			if (interior == null)
			{
				return null;
			}
			return DatabaseObject<CapsuleDescriptor>.Get(interior.CapsuleId);
		}
	}

	public static CapsuleDescriptor? FoldedCapsule { get; private set; }

	public static bool CanDecorateHere
	{
		get
		{
			if (InHouse && Interior.IsOwner)
			{
				return InteriorCapsule?.OwnerCanDecorate ?? false;
			}
			return false;
		}
	}

	public static void OnStates(HouseStatesPacket packet)
	{
		HouseStateInfo[] states = _states;
		_states = packet.States ?? Array.Empty<HouseStateInfo>();
		_version++;
		FoldedCapsule = ((packet.FoldedCapsuleId == Guid.Empty) ? null : DatabaseObject<CapsuleDescriptor>.Get(packet.FoldedCapsuleId));
		if (_states.Length != states.Length)
		{
			HouseInterface.RefreshBuy();
		}
	}

	public static void OnInterior(HouseInteriorPacket packet)
	{
		Interior = ((packet.HouseId == Guid.Empty) ? null : packet);
		InteriorReceivedAt = Timing.Global.MillisecondsUtc;
		RebuildFurnitureCells();
		Decoration.OnInterior();
		if (Interior == null)
		{
			HouseInterface.CloseInside();
		}
	}

	public static void OnKnock(HouseKnockPacket packet)
	{
		RequestCardWindow.Open(RequestCardWindow.RequestKind.Knock, packet.VisitorId, packet.VisitorName);
	}

	public static void OnResult(HouseResultPacket packet)
	{
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		switch (packet.Result)
		{
		case HouseResult.Bought:
			BoughtAt = millisecondsUtc;
			BoughtName = packet.Name;
			HouseInterface.CloseBuy();
			break;
		case HouseResult.Knocked:
		case HouseResult.OwnerAway:
		case HouseResult.NobodyAnswers:
			_feedback = new DoorFeedback(packet.Result, packet.Name, millisecondsUtc + 4500);
			break;
		case HouseResult.TooFar:
			if (!Decoration.Active)
			{
				_feedback = new DoorFeedback(packet.Result, packet.Name, millisecondsUtc + 2500);
			}
			else
			{
				Chat(Strings.Housing.ResultTooFar, success: false);
			}
			break;
		case HouseResult.FurnitureRefused:
			Decoration.Alert(VerdictText((FurnitureVerdict)packet.Amount), ok: false);
			break;
		case HouseResult.FurnitureLimit:
			Decoration.Alert(Strings.Housing.ResultFurnitureLimit.ToString(packet.Amount), ok: false);
			break;
		case HouseResult.InventoryFull:
			Decoration.Alert(Strings.Housing.ResultInventoryFull, ok: false);
			break;
		case HouseResult.ChestNotEmpty:
			if (Decoration.Active)
			{
				Decoration.Alert(Strings.Housing.ResultChestNotEmpty, ok: false);
			}
			else
			{
				Chat(Strings.Housing.ResultChestNotEmptySell, success: false);
			}
			break;
		case HouseResult.Sold:
			Chat(Strings.Housing.ResultSold.ToString(MarketWindow.FormatAmount(packet.Amount)), success: true);
			break;
		case HouseResult.SellNeedsSpace:
			Chat(Strings.Housing.ResultSellNeedsSpace.ToString(packet.Amount), success: false);
			break;
		case HouseResult.Upgraded:
			HouseInterface.CloseBuy();
			Chat((packet.Amount > 0) ? Strings.Housing.ResultUpgradedReturned.ToString(packet.Name, packet.Amount) : Strings.Housing.ResultUpgraded.ToString(packet.Name), success: true);
			break;
		case HouseResult.UpgradeNeedsSpace:
			Chat(Strings.Housing.ResultUpgradeNeedsSpace.ToString(packet.Amount), success: false);
			break;
		case HouseResult.UpgradeChestBlocked:
			Chat(Strings.Housing.ResultUpgradeChestBlocked, success: false);
			break;
		case HouseResult.CannotUpgrade:
			Chat(Strings.Housing.ResultCannotUpgrade, success: false);
			HouseInterface.CloseBuy();
			break;
		case HouseResult.Folded:
			Chat(Strings.Housing.ResultFolded.ToString(packet.Name, packet.Amount), success: false);
			break;
		case HouseResult.FoldedDoesNotFit:
			Chat(Strings.Housing.ResultFoldedDoesNotFit.ToString(packet.Name), success: false);
			break;
		case HouseResult.StoredAll:
			Decoration.Alert((packet.Amount > 0) ? Strings.Housing.ResultStoredAllLeft.ToString(packet.Amount) : Strings.Housing.ResultStoredAll.ToString(), packet.Amount == 0);
			break;
		case HouseResult.NotEnoughZeni:
			Chat(Strings.Housing.ResultNotEnoughZeni.ToString(MarketWindow.FormatAmount(packet.Amount)), success: false);
			break;
		case HouseResult.TerrainTaken:
			Chat(Strings.Housing.ResultTerrainTaken, success: false);
			HouseInterface.CloseBuy();
			break;
		case HouseResult.AlreadyOwner:
			Chat(Strings.Housing.ResultAlreadyOwner.ToString(packet.Name), success: false);
			break;
		case HouseResult.DoesNotFit:
			Chat(Strings.Housing.ResultDoesNotFit, success: false);
			break;
		case HouseResult.InvalidTerrain:
			Chat(Strings.Housing.ResultInvalidTerrain, success: false);
			HouseInterface.CloseBuy();
			break;
		case HouseResult.DoorOpened:
			Chat(Strings.Housing.ResultDoorOpened.ToString(packet.Name), success: true);
			break;
		case HouseResult.ShownOut:
			Chat(Strings.Housing.ResultShownOut.ToString(packet.Name), success: true);
			break;
		case HouseResult.AccessSaved:
			Chat(Strings.Housing.ResultAccessSaved, success: true);
			break;
		case HouseResult.NotYourHouse:
			Chat(Strings.Housing.ResultNotYourHouse, success: false);
			break;
		case HouseResult.CannotDecorate:
			Chat(Strings.Housing.ResultCannotDecorate, success: false);
			break;
		case HouseResult.Slept:
			Chat(string.IsNullOrEmpty(packet.Name) ? Strings.Housing.ResultSlept : Strings.Housing.ResultSleptRespawn, success: true);
			break;
		case HouseResult.Locked:
		case HouseResult.FriendsOnly:
			break;
		}
	}

	private static void Chat(string text, bool success)
	{
		ChatboxMsg.AddMessage(new ChatboxMsg(text, success ? CustomColors.Alerts.Accepted : CustomColors.Alerts.Declined, ChatMessageType.Notice));
	}

	public static List<Plot> PlotsOf(MapInstance map)
	{
		return Cached(map).Plots;
	}

	private static MapPlots Cached(MapInstance map)
	{
		MapAttribute[,] attributes = map.Attributes;
		if (PlotCache.TryGetValue(map.Id, out MapPlots value) && value.Attributes == attributes && value.Version == _version)
		{
			return value;
		}
		List<Plot> list = new List<Plot>();
		HashSet<int> hashSet = new HashSet<int>();
		int num = attributes?.GetLength(0) ?? 0;
		foreach (TerrainArea item in HousingRules.TerrainsOf(attributes))
		{
			HouseStateInfo houseStateInfo = null;
			HouseStateInfo[] states = _states;
			foreach (HouseStateInfo houseStateInfo2 in states)
			{
				if (houseStateInfo2.MapId == map.Id && houseStateInfo2.TerrainId == item.Id)
				{
					houseStateInfo = houseStateInfo2;
					break;
				}
			}
			Guid guid = houseStateInfo?.CapsuleId ?? ((item.Kind == TerrainKind.Prebuilt) ? item.CapsuleId : Guid.Empty);
			CapsuleDescriptor capsuleDescriptor = ((guid == Guid.Empty) ? null : DatabaseObject<CapsuleDescriptor>.Get(guid));
			HouseFootprint footprint = ((capsuleDescriptor != null) ? HousingRules.Footprint(item, capsuleDescriptor, houseStateInfo?.OffsetX ?? (-1), houseStateInfo?.OffsetY ?? (-1)) : default(HouseFootprint));
			list.Add(new Plot(map, item, houseStateInfo, capsuleDescriptor, footprint));
			if (capsuleDescriptor == null)
			{
				continue;
			}
			for (int j = footprint.X; j < footprint.X + footprint.Width; j++)
			{
				for (int k = footprint.Y; k < footprint.Y + footprint.Height; k++)
				{
					hashSet.Add(k * num + j);
				}
			}
		}
		value = new MapPlots(attributes, _version, list, hashSet, num);
		PlotCache[map.Id] = value;
		return value;
	}

	private static void RebuildFurnitureCells()
	{
		HashSet<int> hashSet = new HashSet<int>();
		HouseInteriorPacket interior = Interior;
		if (interior != null && MapInstance.TryGet(interior.MapId, out MapInstance instance) && instance.Attributes != null)
		{
			int length = instance.Attributes.GetLength(0);
			HouseFurnitureInfo[] furniture = interior.Furniture;
			foreach (HouseFurnitureInfo houseFurnitureInfo in furniture)
			{
				FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo.ItemId)?.Furniture;
				if (furnitureDescriptor == null || !HousingRules.BlocksMovement(furnitureDescriptor))
				{
					continue;
				}
				(int, int, int, int) tuple = HousingRules.FurnitureFootprint(furnitureDescriptor, houseFurnitureInfo.Variant, houseFurnitureInfo.X, houseFurnitureInfo.Y);
				var (j, _, _, _) = tuple;
				for (; j < tuple.Item1 + tuple.Item3; j++)
				{
					for (int k = tuple.Item2; k < tuple.Item2 + tuple.Item4; k++)
					{
						hashSet.Add(k * length + j);
					}
				}
			}
		}
		_furnitureCells = hashSet;
	}

	public static bool IsBlocked(Guid mapId, int x, int y)
	{
		if (!MapInstance.TryGet(mapId, out MapInstance instance) || instance.Attributes == null)
		{
			return false;
		}
		MapPlots mapPlots = Cached(instance);
		int item = y * mapPlots.Width + x;
		HouseInteriorPacket interior = Interior;
		if (interior != null && interior.MapId == mapId && _furnitureCells.Contains(item))
		{
			return true;
		}
		return mapPlots.Blocked.Contains(item);
	}

	public static Target TargetFor(Player me)
	{
		var (num, num2) = Front(me);
		if (InHouse)
		{
			HouseFurnitureInfo[] furniture = Interior.Furniture;
			foreach (HouseFurnitureInfo houseFurnitureInfo in furniture)
			{
				FurnitureDescriptor furnitureDescriptor = DatabaseObject<ItemDescriptor>.Get(houseFurnitureInfo.ItemId)?.Furniture;
				if (furnitureDescriptor != null && furnitureDescriptor.Effect != FurnitureEffect.None && (furnitureDescriptor.Effect != FurnitureEffect.Chest || Interior.IsOwner))
				{
					(int, int, int, int) tuple2 = HousingRules.FurnitureFootprint(furnitureDescriptor, houseFurnitureInfo.Variant, houseFurnitureInfo.X, houseFurnitureInfo.Y);
					if (num >= tuple2.Item1 && num < tuple2.Item1 + tuple2.Item3 && num2 >= tuple2.Item2 && num2 < tuple2.Item2 + tuple2.Item4)
					{
						return new Target(TargetKind.Furniture, default(Plot), houseFurnitureInfo);
					}
				}
			}
			return default(Target);
		}
		if (!MapInstance.TryGet(me.MapId, out MapInstance instance))
		{
			return default(Target);
		}
		foreach (Plot item in PlotsOf(instance))
		{
			if (item.HasHouse)
			{
				if (HousingRules.IsAtDoor(item.Footprint, me.X, me.Y))
				{
					return new Target(item.ForSale ? TargetKind.BuyHouse : TargetKind.Door, item, null);
				}
			}
			else if (item.ForSale && item.Terrain.Kind == TerrainKind.Buildable && item.Terrain.DistanceTo(me.X, me.Y) <= 1)
			{
				return new Target(TargetKind.BuyTerrain, item, null);
			}
		}
		return default(Target);
	}

	private static (int X, int Y) Front(Player me)
	{
		return me.DirectionFacing switch
		{
			Direction.Up => (X: me.X, Y: me.Y - 1), 
			Direction.Down => (X: me.X, Y: me.Y + 1), 
			Direction.Left => (X: me.X - 1, Y: me.Y), 
			Direction.Right => (X: me.X + 1, Y: me.Y), 
			Direction.UpLeft => (X: me.X - 1, Y: me.Y - 1), 
			Direction.UpRight => (X: me.X + 1, Y: me.Y - 1), 
			Direction.DownLeft => (X: me.X - 1, Y: me.Y + 1), 
			Direction.DownRight => (X: me.X + 1, Y: me.Y + 1), 
			_ => (X: me.X, Y: me.Y), 
		};
	}

	public static bool TryInteract(Player me)
	{
		if (Decoration.Active)
		{
			return true;
		}
		Target target = TargetFor(me);
		if (target.Kind == TargetKind.None)
		{
			return false;
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (millisecondsUtc < _nextInteract)
		{
			return true;
		}
		_nextInteract = millisecondsUtc + 700;
		switch (target.Kind)
		{
		case TargetKind.BuyTerrain:
		case TargetKind.BuyHouse:
			HouseInterface.OpenBuy(target.Plot);
			break;
		case TargetKind.Door:
			PacketSender.SendHouseDoor(target.Plot.Map.Id, target.Plot.Terrain.Id);
			break;
		case TargetKind.Furniture:
		{
			HouseFurnitureInfo furniture = target.Furniture;
			if (furniture != null)
			{
				ItemDescriptor itemDescriptor = DatabaseObject<ItemDescriptor>.Get(furniture.ItemId);
				if (itemDescriptor != null && itemDescriptor.Furniture?.Effect == FurnitureEffect.Clock)
				{
					Intersect.Client.Interface.Interface.GameUi?.ToggleCalendarWindow();
				}
				else
				{
					PacketSender.SendHouseFurnitureUse(furniture.Id);
				}
			}
			break;
		}
		}
		return true;
	}

	public static Prompt? PromptFor(Player me)
	{
		if (Decoration.Active)
		{
			return null;
		}
		Target target = TargetFor(me);
		switch (target.Kind)
		{
		case TargetKind.BuyTerrain:
			return new Prompt(Strings.Housing.PromptViewTerrain, null, Refused: false);
		case TargetKind.BuyHouse:
			return new Prompt(Strings.Housing.PromptViewHouse, null, Refused: false);
		case TargetKind.Furniture:
		{
			HouseFurnitureInfo furniture = target.Furniture;
			if (furniture != null)
			{
				return DatabaseObject<ItemDescriptor>.Get(furniture.ItemId)?.Furniture?.Effect switch
				{
					FurnitureEffect.Bed => new Prompt(Strings.Housing.PromptBed, null, Refused: false), 
					FurnitureEffect.Chest => new Prompt(Strings.Housing.PromptChest, null, Refused: false), 
					FurnitureEffect.Clock => new Prompt(Strings.Housing.PromptClock, null, Refused: false), 
					_ => null, 
				};
			}
			break;
		}
		case TargetKind.Door:
		{
			HouseStateInfo state = target.Plot.State;
			if (state == null)
			{
				break;
			}
			DoorFeedback? feedback = _feedback;
			if (feedback.HasValue)
			{
				DoorFeedback valueOrDefault = feedback.GetValueOrDefault();
				if (valueOrDefault.Until > Timing.Global.MillisecondsUtc)
				{
					return valueOrDefault.Result switch
					{
						HouseResult.Knocked => new Prompt(Strings.Housing.PromptKnocked, Strings.Housing.PromptKnockedDetail.ToString(valueOrDefault.Name), Refused: true), 
						HouseResult.OwnerAway => new Prompt(Strings.Housing.PromptAway.ToString(valueOrDefault.Name), Strings.Housing.PromptAwayDetail, Refused: true), 
						HouseResult.NobodyAnswers => new Prompt(Strings.Housing.PromptNobody, Strings.Housing.PromptNobodyDetail.ToString(valueOrDefault.Name), Refused: true), 
						_ => new Prompt(Strings.Housing.PromptTooFar, null, Refused: true), 
					};
				}
			}
			if (state.OwnerId == me.Id)
			{
				return new Prompt(Strings.Housing.PromptEnterOwn, null, Refused: false);
			}
			if (state.MayEnter)
			{
				return new Prompt(Strings.Housing.PromptEnter.ToString(state.OwnerName), null, Refused: false);
			}
			return (state.Access == HouseAccess.Locked) ? new Prompt(Strings.Housing.PromptLocked.ToString(state.OwnerName), Strings.Housing.PromptKnock, Refused: true) : new Prompt(Strings.Housing.PromptFriendsOnly.ToString(state.OwnerName), Strings.Housing.PromptKnock, Refused: true);
		}
		}
		return null;
	}

	public static string VerdictText(FurnitureVerdict verdict)
	{
		return verdict switch
		{
			FurnitureVerdict.OutsideRoom => Strings.Housing.VerdictOutside, 
			FurnitureVerdict.WallOnly => Strings.Housing.VerdictWallOnly, 
			FurnitureVerdict.FloorOnly => Strings.Housing.VerdictFloorOnly, 
			FurnitureVerdict.AgainstWallOnly => Strings.Housing.VerdictAgainstWall, 
			FurnitureVerdict.KeepEntranceFree => Strings.Housing.VerdictEntrance, 
			FurnitureVerdict.Occupied => Strings.Housing.VerdictOccupied, 
			_ => string.Empty, 
		};
	}

	public static string AccessText(HouseAccess access)
	{
		return access switch
		{
			HouseAccess.FriendsAndGuild => Strings.Housing.AccessFriends, 
			HouseAccess.Locked => Strings.Housing.AccessLocked, 
			_ => Strings.Housing.AccessOpen, 
		};
	}

	public static string EffectText(FurnitureDescriptor furniture)
	{
		return furniture.Effect switch
		{
			FurnitureEffect.Bed => Strings.Housing.EffectBed, 
			FurnitureEffect.Chest => Strings.Housing.EffectChest.ToString(furniture.ChestSlots), 
			FurnitureEffect.Clock => Strings.Housing.EffectClock, 
			_ => string.Empty, 
		};
	}

	public static void Reset()
	{
		_states = Array.Empty<HouseStateInfo>();
		_version++;
		FoldedCapsule = null;
		PlotCache.Clear();
		Interior = null;
		_furnitureCells = new HashSet<int>();
		_feedback = null;
		BoughtAt = 0L;
		Decoration.Stop();
	}
}
