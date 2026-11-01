using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Admin.Actions;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.EventArguments.InputSubmissionEvent;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Config;
using Intersect.Enums;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Housing;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Rankings;
using Intersect.Framework.Core.Market;
using Intersect.Models;
using Intersect.Network.Packets.Client;

namespace Intersect.Client.Networking;

public static class PacketSender
{
	public static void SendPing()
	{
		Network.SendPacket(new PingPacket
		{
			Responding = true
		});
	}

	public static void SendLogin(string username, string password)
	{
		Network.SendPacket(new LoginPacket(username, password)
		{
			MachineId = MachineIdentity.Id
		});
	}

	public static void SendLogout(bool characterSelect = false)
	{
		Network.SendPacket(new LogoutPacket(characterSelect));
	}

	public static void SendNeedMap(params ObjectCacheKey<MapDescriptor>[] cacheKeys)
	{
		MapInstance instance;
		ObjectCacheKey<MapDescriptor>[] array = (from cacheKey in cacheKeys
			where cacheKey != default(ObjectCacheKey<MapDescriptor>) && MapInstance.MapNotRequested(cacheKey.Id.Guid)
			where string.IsNullOrWhiteSpace(cacheKey.Checksum) || string.IsNullOrWhiteSpace(cacheKey.Version) || !MapInstance.TryGet(cacheKey.Id.Guid, out instance)
			select cacheKey).ToArray();
		if (array.Length >= 1)
		{
			Network.SendPacket(new GetObjectData<MapDescriptor>(array));
			MapInstance.UpdateMapRequestTime(array.Select((ObjectCacheKey<MapDescriptor> cacheKey) => cacheKey.Id.Guid).ToArray());
		}
	}

	public static void SendNeedMap(params Guid[] mapIds)
	{
		MapInstance instance;
		Guid[] array = mapIds.Where((Guid mapId) => mapId != default(Guid) && !MapInstance.TryGet(mapId, out instance) && MapInstance.MapNotRequested(mapId)).ToArray();
		if (array.Length >= 1)
		{
			ChargementDesCartes.DemanderLesVoisines(array);
		}
	}

	public static void SendNeedMapForGrid(MapInstance? mapInstance = null)
	{
		if (mapInstance == null && !MapInstance.TryGet(Globals.Me.MapId, out mapInstance))
		{
			return;
		}
		int gridX = mapInstance.GridX;
		int gridY = mapInstance.GridY;
		int num = Math.Max(0, gridX - 1);
		int num2 = Math.Max(0, gridY - 1);
		long num3 = Math.Min(Globals.MapGridWidth, gridX + 2);
		long num4 = Math.Min(Globals.MapGridHeight, gridY + 2);
		List<Guid> list = new List<Guid>(9);
		for (int i = num; i < num3; i++)
		{
			for (int j = num2; j < num4; j++)
			{
				Guid guid = Globals.MapGrid[i, j];
				if (!(guid == Guid.Empty))
				{
					list.Add(guid);
				}
			}
		}
		SendNeedMap(list.ToArray());
	}

	public static void SendMove()
	{
		Network.SendPacket(new MovePacket(Globals.Me.MapId, Globals.Me.X, Globals.Me.Y, Globals.Me.DirectionFacing));
	}

	public static void SendMove(long ticksDuPas)
	{
		Network.SendPacket(new MovePacket(Globals.Me.MapId, Globals.Me.X, Globals.Me.Y, Globals.Me.DirectionFacing)
		{
			Adjusted = ticksDuPas
		});
	}

	public static void SendChatMsg(string msg, byte channel)
	{
		Network.SendPacket(new ChatMsgPacket(msg, channel));
	}

	public static void SendChatPhrase(byte channel, string phraseId, string phraseNom = "")
	{
		Network.SendPacket(new ChatPhrasePacket(channel, phraseId, phraseNom));
	}

	public static void SendAttack(Guid targetId, bool ruee = false, GesteDeCoup geste = GesteDeCoup.Ordinaire, byte coup = 0)
	{
		Network.SendPacket(new AttackPacket(targetId, ruee, geste, coup));
	}

	public static void SendFrappeDeClash(Guid adversaire, int cercle, int ecartMs)
	{
		Network.SendPacket(new AttackPacket(adversaire, ruee: false, GesteDeCoup.FrappeDeClash, 0)
		{
			Cercle = cercle,
			EcartMs = ecartMs
		});
	}

	public static void SendBlock(bool blocking)
	{
		if (Heritage.Actif && blocking)
		{
			Heritage.AvantUneAutreAction();
		}
		Network.SendPacket(new BlockPacket(blocking));
	}

	public static void SendChargeKi(bool charging)
	{
		Network.SendPacket(new ChargeKiPacket(charging));
	}

	public static void SendBeamRequest(int slot, bool charging, bool cancel, float angle, Guid cibleLiee = default(Guid), float courbe = 0f)
	{
		Network.SendPacket(new BeamRequestPacket(slot, charging, cancel, angle)
		{
			CibleLiee = cibleLiee,
			Courbe = courbe
		});
	}

	public static void SendEauDesCases(Guid mapId, byte[] cases)
	{
		Network.SendPacket(new EauDesCasesPacket(mapId, cases));
	}

	public static void SendVanishRequest(sbyte sensX = 0, sbyte sensY = 0)
	{
		if (Heritage.Actif)
		{
			Heritage.AvantUneAutreAction();
		}
		Network.SendPacket(new VanishRequestPacket(sensX, sensY));
	}

	public static void SendDashRequest(float sensX, float sensY, Guid cibleId)
	{
		if (Heritage.Actif)
		{
			Heritage.AvantUneAutreAction();
		}
		Network.SendPacket(new DashRequestPacket(sensX, sensY, cibleId));
	}

	public static void SendMarketSearch(string query, MarketCategory category, MarketSort sort, int page)
	{
		Network.SendPacket(new MarketSearchPacket(query, category, sort, page));
	}

	public static void SendMarketMyListings()
	{
		Network.SendPacket(new MarketMyListingsPacket());
	}

	public static void SendMarketSell(int slot, int quantity, long unitPrice)
	{
		Network.SendPacket(new MarketSellPacket(slot, quantity, unitPrice));
	}

	public static void SendMarketBuy(Guid listingId, int quantity, long unitPrice)
	{
		Network.SendPacket(new MarketBuyPacket(listingId, quantity, unitPrice));
	}

	public static void SendMarketCancel(Guid listingId)
	{
		Network.SendPacket(new MarketCancelPacket(listingId));
	}

	public static void SendMarketCollect()
	{
		Network.SendPacket(new MarketCollectPacket());
	}

	public static void SendMarketPrices(Guid[] itemIds, int slot)
	{
		Network.SendPacket(new MarketPricesPacket(itemIds, slot));
	}

	public static void SendDailyRewardClaim()
	{
		Network.SendPacket(new DailyRewardClaimPacket());
	}

	public static void SendSetActiveTitle(Guid titleId)
	{
		Network.SendPacket(new SetActiveTitlePacket(titleId));
	}

	public static void SendSetActiveOrnament(Guid ornamentId)
	{
		Network.SendPacket(new SetActiveOrnamentPacket(ornamentId));
	}

	public static void SendLangue()
	{
		Network.SendPacket(new LanguePacket(Langue.Actuelle));
	}

	public static void SendSprint(bool sprinting)
	{
		Network.SendPacket(new SprintPacket(sprinting));
	}

	public static void SendWorldMapTeleport(string destinationId)
	{
		Network.SendPacket(new WorldMapTeleportPacket(destinationId));
	}

	public static void SendDirection(Direction dir)
	{
		Network.SendPacket(new DirectionPacket(dir));
	}

	public static void SendEnterGame()
	{
		Network.SendPacket(new EnterGamePacket());
	}

	public static void SendActivateEvent(Guid eventId)
	{
		Network.SendPacket(new ActivateEventPacket(eventId));
	}

	public static void SendEventResponse(byte response, Dialog ed)
	{
		Globals.EventDialogs.Remove(ed);
		Network.SendPacket(new EventResponsePacket(ed.EventId, response));
	}

	public static void SendEventInputVariable(Base sender, InputSubmissionEventArgs args)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid eventId)
		{
			bool booleanValue = args.Value is BooleanSubmissionValue booleanSubmissionValue && booleanSubmissionValue.Value;
			int value = ((args.Value is NumericalSubmissionValue numericalSubmissionValue) ? ((int)numericalSubmissionValue.Value) : 0);
			string stringValue = ((args.Value is StringSubmissionValue stringSubmissionValue) ? stringSubmissionValue.Value : null);
			Network.SendPacket(new EventInputVariablePacket(eventId, booleanValue, value, stringValue));
		}
	}

	public static void SendEventInputVariableCancel(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid eventId)
		{
			Network.SendPacket(new EventInputVariablePacket(eventId, booleanValue: false, 0, null, canceled: true));
		}
	}

	public static void SendUserRegistration(string username, string password, string email)
	{
		Network.SendPacket(new UserRegistrationRequestPacket(username, password, email)
		{
			MachineId = MachineIdentity.Id
		});
	}

	public static void SendCreateCharacter(string name, Guid classId, int sprite)
	{
		Network.SendPacket(new CreateCharacterPacket(name, classId, sprite));
	}

	public static void SendPickupItem(Guid mapId, int tileIndex, Guid uniqueId)
	{
		Network.SendPacket(new PickupItemPacket(mapId, tileIndex, uniqueId));
	}

	public static void SendSwapInvItems(int item1, int item2)
	{
		Network.SendPacket(new SwapInvItemsPacket(item1, item2));
	}

	public static void SendDropItem(int slot, int amount)
	{
		Globals.Me?.NoterUnLacher();
		Network.SendPacket(new DropItemPacket(slot, amount));
	}

	public static void SendUseItem(int slot, Guid targetId)
	{
		Network.SendPacket(new UseItemPacket(slot, targetId));
	}

	public static void SendSwapSpells(int spell1, int spell2)
	{
		Network.SendPacket(new SwapSpellsPacket(spell1, spell2));
	}

	public static void SendEvolveSpell(int slot)
	{
		Network.SendPacket(new EvolveSpellPacket(slot));
	}

	public static void SendForgetSpell(int slot)
	{
		Network.SendPacket(new ForgetSpellPacket(slot));
	}

	public static void SendUseSpell(int slot, Guid targetId, byte lien = 0)
	{
		if (Heritage.Actif && !HeritageRegles.EstLie(lien))
		{
			Heritage.AvantUneAutreAction();
		}
		Network.SendPacket(new UseSpellPacket(slot, targetId, Globals.ShouldSoftRetargetOnSelfCast)
		{
			Lien = lien
		});
	}

	public static void SendUnequipItem(int slot)
	{
		Network.SendPacket(new UnequipItemPacket(slot));
	}

	public static void SendUpgradeStat(byte stat, int amount = 1)
	{
		Network.SendPacket(new UpgradeStatPacket(stat, amount));
	}

	public static void SendReinitialiserLesPoints()
	{
		Network.SendPacket(new UpgradeStatPacket(byte.MaxValue, 0));
	}

	public static void SendHotbarUpdate(int hotbarSlot, sbyte type, int itemIndex)
	{
		Network.SendPacket(new HotbarUpdatePacket(hotbarSlot, type, itemIndex));
	}

	public static void SendHotbarSwap(int index, int swapIndex)
	{
		Network.SendPacket(new HotbarSwapPacket(index, swapIndex));
	}

	public static void SendOpenAdminWindow()
	{
		Network.SendPacket(new OpenAdminWindowPacket());
	}

	public static void SendSellItem(int slot, int amount)
	{
		Network.SendPacket(new SellItemPacket(slot, amount));
	}

	public static void SendBuyItem(int slot, int amount)
	{
		Network.SendPacket(new BuyItemPacket(slot, amount));
	}

	public static void SendCloseShop()
	{
		Network.SendPacket(new CloseShopPacket());
	}

	public static void SendDepositItem(int slot, int amount, int bankSlot = -1)
	{
		Network.SendPacket(new DepositItemPacket(slot, amount, bankSlot));
	}

	public static void SendWithdrawItem(int slot, int amount, int invSlot = -1)
	{
		Network.SendPacket(new WithdrawItemPacket(slot, amount, invSlot));
	}

	public static void SendCloseBank()
	{
		Network.SendPacket(new CloseBankPacket());
	}

	public static void SendCloseCrafting()
	{
		Network.SendPacket(new CloseCraftingPacket());
	}

	public static void SendMoveBankItems(int slot1, int slot2)
	{
		Network.SendPacket(new SwapBankItemsPacket(slot1, slot2));
	}

	public static void SendCraftItem(Guid id, int count)
	{
		Network.SendPacket(new CraftItemPacket(id, count));
	}

	public static void SendPartyInvite(Guid targetId)
	{
		Network.SendPacket(new PartyInvitePacket(targetId));
	}

	public static void SendPartyInvite(string target)
	{
		Network.SendPacket(new PartyInvitePacket(target));
	}

	public static void SendPartyKick(Guid targetId)
	{
		Network.SendPacket(new PartyKickPacket(targetId));
	}

	public static void SendPartyLeave()
	{
		Network.SendPacket(new PartyLeavePacket());
	}

	public static void SendPartyAccept(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid partyId)
		{
			Network.SendPacket(new PartyInviteResponsePacket(partyId, accepting: true));
		}
	}

	public static void SendPartyDecline(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid partyId)
		{
			Network.SendPacket(new PartyInviteResponsePacket(partyId, accepting: false));
		}
	}

	public static void SendPayHellBail()
	{
		Network.SendPacket(new PayHellBailPacket());
	}

	public static void SendTrackingRadarLocate(Guid targetId)
	{
		Network.SendPacket(new TrackingRadarLocatePacket(targetId));
	}

	public static void SendWantedContract(WantedContractKind kind, Guid targetId, bool take)
	{
		Network.SendPacket(new WantedContractPacket(kind, targetId, take));
	}

	public static void SendWantedTurnInClaim()
	{
		Network.SendPacket(new WantedTurnInClaimPacket());
	}

	public static void SendBlackMarketBuy(BlackMarketArticle article)
	{
		Network.SendPacket(new BlackMarketBuyPacket(article));
	}

	public static void SendRibbonContract(string key, bool take)
	{
		Network.SendPacket(new RibbonContractPacket(key, take));
	}

	public static void SendAcceptQuest(Guid questId)
	{
		Network.SendPacket(new QuestResponsePacket(questId, accepting: true));
	}

	public static void SendDeclineQuest(Guid questId)
	{
		Network.SendPacket(new QuestResponsePacket(questId, accepting: false));
	}

	public static void SendAbandonQuest(Guid questId)
	{
		Network.SendPacket(new AbandonQuestPacket(questId));
	}

	public static void SendTradeRequest(Guid targetId)
	{
		Network.SendPacket(new TradeRequestPacket(targetId));
	}

	public static void SendOfferTradeItem(int slot, int amount)
	{
		Network.SendPacket(new OfferTradeItemPacket(slot, amount));
	}

	public static void SendRevokeTradeItem(int slot, int amount)
	{
		Network.SendPacket(new RevokeTradeItemPacket(slot, amount));
	}

	public static void SendAcceptTrade(int revision)
	{
		Network.SendPacket(new AcceptTradePacket(revision));
	}

	public static void SendOfferTradeZeni(long amount)
	{
		Network.SendPacket(new OfferTradeZeniPacket(amount));
	}

	public static void SendTradeRequestResponse(Guid partnerId, bool accepted)
	{
		Network.SendPacket(new TradeRequestResponsePacket(partnerId, accepted));
	}

	public static void SendPartyInviteResponse(Guid leaderId, bool accepted)
	{
		Network.SendPacket(new PartyInviteResponsePacket(leaderId, accepted));
	}

	public static void SendFriendRequestResponse(Guid requesterId, bool accepted)
	{
		Network.SendPacket(new FriendRequestResponsePacket(requesterId, accepted));
	}

	public static void SendGuildInviteResponse(bool accepted)
	{
		if (accepted)
		{
			Network.SendPacket(new GuildInviteAcceptPacket());
		}
		else
		{
			Network.SendPacket(new GuildInviteDeclinePacket());
		}
	}

	public static void SendDeclineTrade()
	{
		Network.SendPacket(new DeclineTradePacket());
	}

	public static void SendTradeRequestAccept(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid tradeId)
		{
			Network.SendPacket(new TradeRequestResponsePacket(tradeId, accepting: true));
		}
	}

	public static void SendTradeRequestDecline(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid tradeId)
		{
			Network.SendPacket(new TradeRequestResponsePacket(tradeId, accepting: false));
		}
	}

	public static void SendStoreBagItem(int invSlot, int amount, int bagSlot)
	{
		Network.SendPacket(new StoreBagItemPacket(invSlot, amount, bagSlot));
	}

	public static void SendRetrieveBagItem(int bagSlot, int amount, int invSlot)
	{
		Network.SendPacket(new RetrieveBagItemPacket(bagSlot, amount, invSlot));
	}

	public static void SendCloseBag()
	{
		Network.SendPacket(new CloseBagPacket());
	}

	public static void SendMoveBagItems(int slot1, int slot2)
	{
		Network.SendPacket(new SwapBagItemsPacket(slot1, slot2));
	}

	public static void SendRequestFriends()
	{
		Network.SendPacket(new RequestFriendsPacket());
	}

	public static void SendAddFriend(string name)
	{
		Network.SendPacket(new UpdateFriendsPacket(name, adding: true));
	}

	public static void SendRemoveFriend(string name)
	{
		Network.SendPacket(new UpdateFriendsPacket(name, adding: false));
	}

	public static void SendFriendRequestAccept(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid friendId)
		{
			Network.SendPacket(new FriendRequestResponsePacket(friendId, accepting: true));
		}
	}

	public static void SendFriendRequestDecline(object? sender, EventArgs e)
	{
		if (sender is InputBox inputBox && inputBox.UserData is Guid friendId)
		{
			Network.SendPacket(new FriendRequestResponsePacket(friendId, accepting: false));
		}
	}

	public static void SendSelectCharacter(Guid charId)
	{
		Network.SendPacket(new SelectCharacterPacket(charId));
	}

	public static void SendDeleteCharacter(Guid charId)
	{
		Network.SendPacket(new DeleteCharacterPacket(charId));
	}

	public static void SendNewCharacter()
	{
		Network.SendPacket(new NewCharacterPacket());
	}

	public static void SendRequestPasswordReset(string nameEmail)
	{
		Network.SendPacket(new RequestPasswordResetPacket(nameEmail));
	}

	public static void SendPasswordChangeRequest(string identifier, string token, string passwordHash)
	{
		Network.SendPacket(new PasswordChangeRequestPacket(identifier, token, passwordHash));
	}

	public static void SendAdminAction(Intersect.Admin.Actions.AdminAction action)
	{
		Network.SendPacket(new AdminActionPacket(action));
	}

	public static void SendBumpEvent(Guid mapId, Guid eventId)
	{
		Network.SendPacket(new BumpPacket(mapId, eventId));
	}

	public static void SendRequestGuild()
	{
		Network.SendPacket(new RequestGuildPacket());
	}

	public static void SendGuildInviteAccept(object? sender, EventArgs e)
	{
		Network.SendPacket(new GuildInviteAcceptPacket());
	}

	public static void SendGuildInviteDecline(object? sender, EventArgs e)
	{
		Network.SendPacket(new GuildInviteDeclinePacket());
	}

	public static void SendInviteGuild(string name)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(Guid.Empty, name, GuildMemberUpdateAction.Invite));
	}

	public static void SendLeaveGuild()
	{
		Network.SendPacket(new GuildLeavePacket());
	}

	public static void SendKickGuildMember(Guid id)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(id, null, GuildMemberUpdateAction.Remove));
	}

	public static void SendPromoteGuildMember(Guid id, int rank)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(id, null, GuildMemberUpdateAction.Promote, rank));
	}

	public static void SendDemoteGuildMember(Guid id, int rank)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(id, null, GuildMemberUpdateAction.Demote, rank));
	}

	public static void SendTransferGuild(Guid id)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(id, null, GuildMemberUpdateAction.Transfer));
	}

	public static void SendGuildMotd(string motd)
	{
		Network.SendPacket(new GuildMotdPacket(motd));
	}

	public static void SendGuildDisband()
	{
		Network.SendPacket(new GuildDisbandPacket());
	}

	public static void SendGuildCreate(string name)
	{
		Network.SendPacket(new GuildCreatePacket(name));
	}

	public static void SendGuildDirectory(string query, int page)
	{
		Network.SendPacket(new GuildDirectoryRequestPacket(query, page));
	}

	public static void SendGuildApply(string guildName)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(Guid.Empty, guildName, GuildMemberUpdateAction.Apply));
	}

	public static void SendGuildProfile(string guildName)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(Guid.Empty, guildName, GuildMemberUpdateAction.ViewProfile));
	}

	public static void SendGuildIdentity(string tag, int emblem, int color)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(Guid.Empty, $"{tag}|{emblem}|{color}", GuildMemberUpdateAction.SetIdentity));
	}

	public static void SendGuildApplicationResponse(Guid playerId, bool accepted)
	{
		Network.SendPacket(new UpdateGuildMemberPacket(playerId, null, accepted ? GuildMemberUpdateAction.AcceptApplication : GuildMemberUpdateAction.RefuseApplication));
	}

	public static void SendClosePicture(Guid eventId)
	{
		if (eventId != Guid.Empty)
		{
			Network.SendPacket(new PictureClosedPacket(eventId));
		}
	}

	public static void SendFadeCompletePacket()
	{
		Network.SendPacket(new FadeCompletePacket());
	}

	public static void SendTarget(Guid targetId)
	{
		Network.SendPacket(new TargetPacket(targetId));
	}

	public static void SendDungeonEnter(Guid dungeonId, DungeonDifficulty difficulty)
	{
		Network.SendPacket(new DungeonEnterPacket(dungeonId, difficulty));
	}

	public static void SendDungeonLeave()
	{
		Network.SendPacket(new DungeonLeavePacket());
	}

	public static void SendHouseBuy(Guid mapId, Guid terrainId, Guid capsuleId, int offsetX, int offsetY)
	{
		Network.SendPacket(new HouseBuyPacket(mapId, terrainId, capsuleId, offsetX, offsetY));
	}

	public static void SendHouseSell()
	{
		Network.SendPacket(new HouseSellPacket());
	}

	public static void SendHouseUpgrade(Guid capsuleId, int offsetX, int offsetY)
	{
		Network.SendPacket(new HouseUpgradePacket(capsuleId, offsetX, offsetY));
	}

	public static void SendHouseDoor(Guid mapId, Guid terrainId)
	{
		Network.SendPacket(new HouseDoorPacket(mapId, terrainId));
	}

	public static void SendHouseKnockAnswer(Guid visitorId, bool open)
	{
		Network.SendPacket(new HouseKnockAnswerPacket(visitorId, open));
	}

	public static void SendHouseAccess(HouseAccess access)
	{
		Network.SendPacket(new HouseSetAccessPacket(access));
	}

	public static void SendHouseShowOut(Guid visitorId)
	{
		Network.SendPacket(new HouseShowOutPacket(visitorId));
	}

	public static void SendHouseFurniturePlace(Guid furnitureId, Guid itemId, int x, int y, int variant, bool mirror)
	{
		Network.SendPacket(new HouseFurniturePlacePacket(furnitureId, itemId, x, y, variant, mirror));
	}

	public static void SendHouseFurnitureStore(Guid furnitureId)
	{
		Network.SendPacket(new HouseFurnitureStorePacket(furnitureId));
	}

	public static void SendHouseFurnitureUse(Guid furnitureId)
	{
		Network.SendPacket(new HouseFurnitureUsePacket(furnitureId));
	}

	public static void SendRankingRequest(RankingQuery query)
	{
		Network.SendPacket(new RankingRequestPacket(query));
	}

	public static void SendRankingProfileRequest(Guid playerId)
	{
		Network.SendPacket(new RankingProfileRequestPacket(playerId));
	}

	public static void SendSaveTeachings(int main, int secondary, Dictionary<string, int> ranks)
	{
		Network.SendPacket(new SaveTeachingsPacket(main, secondary, ranks));
	}

	public static void SendTeachingTrialLeave()
	{
		Network.SendPacket(new TeachingTrialLeavePacket());
	}
}
