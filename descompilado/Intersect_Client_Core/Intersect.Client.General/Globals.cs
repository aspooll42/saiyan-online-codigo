using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Input;
using Intersect.Client.Framework.Sys;
using Intersect.Client.Items;
using Intersect.Client.Maps;
using Intersect.Client.Plugins.Interfaces;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.DailyRewards;
using Intersect.Framework.Core.GameObjects.WorldMap;
using Intersect.GameObjects;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.General;

public static class Globals
{
	public static readonly Dictionary<Guid, Entity> Entities = new Dictionary<Guid, Entity>();

	public static readonly List<Guid> EntitiesToDispose = new List<Guid>();

	public static readonly List<Dialog> EventDialogs = new List<Dialog>();

	public static readonly Dictionary<Guid, Guid> EventHolds = new Dictionary<Guid, Guid>();

	public static readonly object GameLock = new object();

	internal static readonly List<IClientLifecycleHelper> ClientLifecycleHelpers = new List<IClientLifecycleHelper>();

	private static GameStates mGameState = GameStates.Intro;

	public static readonly Dictionary<Guid, Point> GridMaps = new Dictionary<Guid, Point>();

	public static bool HasGameData = false;

	public static bool InBag = false;

	public static bool InBank = false;

	public static bool InCraft = false;

	public static bool InTrade = false;

	public static GameInput InputManager;

	public static bool IntroComing = true;

	public static readonly long IntroDelay = 2000L;

	public static int IntroIndex = 0;

	public static long IntroStartTime = -1L;

	public static bool IsRunning = true;

	public static bool JoiningGame = false;

	public static bool LoggedIn = false;

	public static Guid[,]? MapGrid;

	public static long MapGridHeight;

	public static long MapGridWidth;

	public static Player? Me;

	public static bool MoveRouteActive = false;

	public static bool NeedsMaps = true;

	public static readonly Dictionary<Guid, Dictionary<Guid, EventEntityPacket>> PendingEvents = new Dictionary<Guid, Dictionary<Guid, EventEntityPacket>>();

	public static ShowPicturePacket? Picture;

	public static WorldMapDestination[] WorldMapDestinations = Array.Empty<WorldMapDestination>();

	public static HashSet<Guid> VisitedMaps = new HashSet<Guid>();

	public static bool[] DragonBalls = Array.Empty<bool>();

	public static AchievementProgress[] Achievements = Array.Empty<AchievementProgress>();

	public static int AchievementPointsEarned;

	public static int AchievementPointsTotal;

	public static DailyRewardEntry[] DailyRewards = Array.Empty<DailyRewardEntry>();

	public static int DailyRewardDay;

	public static bool DailyRewardCanClaim;

	public static long DailyRewardTotal;

	public static List<(Guid QuestId, Guid MapId, int X, int Y)> QuestTargets = new List<(Guid, Guid, int, int)>();

	public static Guid StoryQuestId;

	public static string StoryGiver = string.Empty;

	public static string StoryPlace = string.Empty;

	public static WantedContractEntryPacket[] WantedContracts = Array.Empty<WantedContractEntryPacket>();

	public static long WantedContractsAt;

	public static RibbonContractEntryPacket? RibbonContract;

	public static long RibbonContractAt;

	public static readonly List<Guid> QuestOffers = new List<Guid>();

	public static readonly Random Random = new Random();

	public static GameSystem System;

	public static Item[,] Trade;

	public static CraftingTableDescriptor? ActiveCraftingTable { get; set; }

	public static int AnimationFrame { get; set; }

	public static Item[]? BagSlots { get; set; }

	public static Item[]? BankSlots { get; set; }

	public static bool IsGuildBank { get; set; }

	public static int BankSlotCount { get; set; }

	public static bool ConnectionLost { get; set; }

	public static bool SoftLogout { get; set; }

	public static GameContentManager? ContentManager { get; set; }

	public static GameDatabase? Database { get; set; }

	public static ShopDescriptor? GameShop { get; set; }

	public static GameStates GameState
	{
		get
		{
			return mGameState;
		}
		set
		{
			mGameState = value;
			EmitLifecycleChangingState();
		}
	}

	public static bool InShop => GameShop != null;

	public static bool CanCloseInventory
	{
		get
		{
			if (!InBag && !InBank && !InCraft && !InShop)
			{
				return !InTrade;
			}
			return false;
		}
	}

	public static bool HoldToSoftRetargetOnSelfCast { get; set; }

	public static bool ShouldSoftRetargetOnSelfCast
	{
		get
		{
			if (!HoldToSoftRetargetOnSelfCast)
			{
				return Database.AutoSoftRetargetOnSelfCast;
			}
			return true;
		}
	}

	public static bool WaitingOnServer { get; set; }

	internal static void EmitLifecycleChangingState()
	{
		foreach (IClientLifecycleHelper clientLifecycleHelper in ClientLifecycleHelpers)
		{
			clientLifecycleHelper.EmitLifecycleChangingState(GameState);
		}
	}

	internal static void EmitLifecycleChangedState()
	{
		foreach (IClientLifecycleHelper clientLifecycleHelper in ClientLifecycleHelpers)
		{
			clientLifecycleHelper.EmitLifecycleChangedState(GameState);
		}
	}

	internal static void OnGameUpdate(TimeSpan deltaTime)
	{
		Dictionary<Guid, IEntity> knownEntities = new Dictionary<Guid, IEntity>();
		if (Entities != null)
		{
			foreach (KeyValuePair<Guid, Entity> entity in Entities)
			{
				if (!knownEntities.ContainsKey(entity.Key))
				{
					knownEntities.Add(entity.Key, entity.Value);
				}
			}
		}
		if (MapGrid != null)
		{
			for (int i = 0; i < MapGridWidth; i++)
			{
				for (int j = 0; j < MapGridHeight; j++)
				{
					MapInstance mapInstance = MapInstance.Get(MapGrid[i, j]);
					if (mapInstance == null)
					{
						continue;
					}
					foreach (KeyValuePair<Guid, Entity> localEntity in mapInstance.LocalEntities)
					{
						if (!knownEntities.ContainsKey(localEntity.Key))
						{
							knownEntities.Add(localEntity.Key, localEntity.Value);
						}
					}
				}
			}
		}
		ClientLifecycleHelpers.ForEach(delegate(IClientLifecycleHelper clientLifecycleHelper)
		{
			clientLifecycleHelper?.EmitGameUpdate(GameState, Me, knownEntities, deltaTime);
		});
	}

	internal static void OnGameDraw(DrawStates state, TimeSpan deltaTime)
	{
		ClientLifecycleHelpers.ForEach(delegate(IClientLifecycleHelper clientLifecycleHelper)
		{
			clientLifecycleHelper?.EmitGameDraw(state, deltaTime);
		});
	}

	internal static void OnGameDraw(DrawStates state, IEntity entity, TimeSpan deltaTime)
	{
		ClientLifecycleHelpers.ForEach(delegate(IClientLifecycleHelper clientLifecycleHelper)
		{
			clientLifecycleHelper?.EmitGameDraw(state, entity, deltaTime);
		});
	}

	public static bool TryGetEntity(EntityType entityType, Guid entityId, [NotNullWhen(true)] out Entity? entity)
	{
		if (!Entities.TryGetValue(entityId, out entity))
		{
			return false;
		}
		if (entity.IsDisposed)
		{
			Entities.Remove(entityId);
			entity = null;
			return false;
		}
		if (entity.Type != entityType)
		{
			ApplicationContext.CurrentContext.Logger.LogError("Found instance of {ActualEntityType} registered to {EntityId} but it was expected to be {ExpectedEntityType}", entity.Type, entityId, entityType);
			Entities.Remove(entityId);
			entity.Dispose();
			entity = null;
			return false;
		}
		EntitiesToDispose.Remove(entityId);
		return true;
	}

	public static Entity? GetEntity(Guid id, EntityType type)
	{
		if (!TryGetEntity(type, id, out Entity entity))
		{
			return null;
		}
		return entity;
	}
}
