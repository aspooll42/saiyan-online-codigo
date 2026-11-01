using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Core.Sounds;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Items;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.Boss;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.Enseignements;
using Intersect.Client.Interface.Game.Rankings;
using Intersect.Client.Interface.Game.Trades;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Items;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.Spells;
using Intersect.Collections;
using Intersect.Config;
using Intersect.Configuration;
using Intersect.Core;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.Entities;
using Intersect.Framework.Core.GameObjects.Achievements;
using Intersect.Framework.Core.GameObjects.Animations;
using Intersect.Framework.Core.GameObjects.Crafting;
using Intersect.Framework.Core.GameObjects.DailyRewards;
using Intersect.Framework.Core.GameObjects.Events;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Core.GameObjects.Maps.Attributes;
using Intersect.Framework.Core.GameObjects.Maps.MapList;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Framework.Core.GameObjects.WorldMap;
using Intersect.Framework.Core.Security;
using Intersect.GameObjects;
using Intersect.Localization;
using Intersect.Models;
using Intersect.Network;
using Intersect.Network.Packets.Server;
using Microsoft.Extensions.Logging;
using Saiyan.Paquets;

namespace Intersect.Client.Networking;

internal sealed class PacketHandler
{
	private sealed class VirtualPacketSender : IPacketSender
	{
		public IApplicationContext ApplicationContext { get; }

		public INetwork Network => Intersect.Client.Networking.Network.Socket.Network;

		public VirtualPacketSender(IApplicationContext applicationContext)
		{
			ApplicationContext = applicationContext ?? throw new ArgumentNullException("applicationContext");
		}

		public bool Send(IPacket packet)
		{
			if (packet is IntersectPacket packet2)
			{
				Intersect.Client.Networking.Network.SendPacket(packet2);
				return true;
			}
			return false;
		}
	}

	public long Ping { get; set; }

	public long PingTime { get; set; }

	public IClientContext Context { get; }

	public ILogger Logger => Context.Logger;

	public PacketHandlerRegistry Registry { get; }

	public IPacketSender VirtualSender { get; }

	public void HandlePacket(IPacketSender packetSender, BossMondialPacket packet)
	{
		BossMondial.OnEtat(packet);
	}

	public void HandlePacket(IPacketSender packetSender, BossMondialResultPacket packet)
	{
		BossMondial.OnResultat(packet);
	}

	public void HandlePacket(IPacketSender packetSender, CleDesRessourcesPacket packet)
	{
		Coffre.PoserLaCleDuJeu(packet.Cle);
	}

	public PacketHandler(IClientContext context, PacketHandlerRegistry packetHandlerRegistry)
	{
		Context = context ?? throw new ArgumentNullException("context");
		Registry = packetHandlerRegistry ?? throw new ArgumentNullException("packetHandlerRegistry");
		if (!Registry.TryRegisterAvailableMethodHandlers(GetType(), this, requireAttribute: false) || Registry.IsEmpty)
		{
			throw new InvalidOperationException("Failed to register method handlers, see logs for more details.");
		}
		if (!Registry.TryRegisterAvailableTypeHandlers(GetType().Assembly))
		{
			throw new InvalidOperationException("Failed to register type handlers, see logs for more details.");
		}
		VirtualSender = new VirtualPacketSender(context);
	}

	public bool HandlePacket(IPacket packet)
	{
		if (!(packet is IntersectPacket))
		{
			return false;
		}
		if (!packet.IsValid)
		{
			return false;
		}
		if (!Registry.TryGetHandler(packet, out var handlerInstance))
		{
			Logger.LogError("No registered handler for " + packet.GetType().FullName + "!");
			return false;
		}
		if (Registry.TryGetPreprocessors(packet, out var preprocessors) && !preprocessors.All((IPacketHandler preprocessor) => preprocessor.Handle(VirtualSender, packet)))
		{
			return false;
		}
		if (Registry.TryGetPreHooks(packet, out var preHooks) && !preHooks.All((IPacketHandler hook) => hook.Handle(VirtualSender, packet)))
		{
			Logger.LogError("PreHook handler failed for " + packet.GetType().FullName + ".");
			return false;
		}
		if (!handlerInstance(VirtualSender, packet))
		{
			return false;
		}
		if (Registry.TryGetPostHooks(packet, out var postHooks) && !postHooks.All((IPacketHandler hook) => hook.Handle(VirtualSender, packet)))
		{
			Logger.LogError("PostHook handler failed for " + packet.GetType().FullName + ".");
			return false;
		}
		return true;
	}

	public void HandlePacket(IPacketSender packetSender, PingPacket packet)
	{
		if (packet.RequestingReply)
		{
			PacketSender.SendPing();
			PingTime = Timing.Global.Milliseconds;
		}
	}

	public void HandlePacket(IPacketSender packetSender, ConfigPacket packet)
	{
		ApplicationContext.Context.Value?.Logger.LogDebug("Received configuration from server.");
		Options.LoadFromServer(packet.Config);
		Globals.WaitingOnServer = false;
		PacketSender.SendLangue();
		MainMenu.HandleReceivedConfiguration();
		try
		{
			Strings.Load();
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "Error loading strings");
			throw;
		}
		Graphics.InitInGame();
	}

	public void HandlePacket(IPacketSender packetSender, JoinGamePacket packet)
	{
		Main.JoinGame();
		Globals.JoiningGame = true;
	}

	public void HandlePacket(IPacketSender packetSender, MapAreaPacket packet)
	{
		MapPacket[] maps = packet.Maps;
		foreach (MapPacket paquet in maps)
		{
			ChargementDesCartes.LireDuServeur(paquet, chercherLesVoisines: false);
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapAreaIdsPacket packet)
	{
		ChargementDesCartes.LireDuCache(packet.MapIds);
	}

	public void HandlePacket(IPacketSender packetSender, MapPacket packet)
	{
		ChargementDesCartes.LireDuServeur(packet, chercherLesVoisines: true);
	}

	public void HandlePacket(IPacketSender packetSender, PlayerEntityPacket packet)
	{
		Entity entity = Globals.GetEntity(packet.EntityId, EntityType.Player);
		if (entity != null)
		{
			entity.Load(packet);
			if (packet.IsSelf)
			{
				Globals.Me = (Player)Globals.Entities[packet.EntityId];
			}
		}
		else
		{
			Globals.Entities.Add(packet.EntityId, new Player(packet.EntityId, packet));
			if (packet.IsSelf)
			{
				Globals.Me = (Player)Globals.Entities[packet.EntityId];
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, NpcEntityPacket packet)
	{
		Entity entity = Globals.GetEntity(packet.EntityId, EntityType.GlobalEntity);
		if (entity != null)
		{
			entity.Load(packet);
			entity.Aggression = packet.Aggression;
			entity.Bleeds = packet.Bleeds;
			entity.BossEnragePercent = packet.BossEnragePercent;
			entity.BossVisual = (BossVisual)packet.BossVisual;
			entity.RayonDuCorps = packet.RayonDuCorps;
		}
		else
		{
			Entity entity2 = new Entity(packet.EntityId, packet, EntityType.GlobalEntity)
			{
				Aggression = packet.Aggression,
				Bleeds = packet.Bleeds,
				BossEnragePercent = packet.BossEnragePercent,
				BossVisual = (BossVisual)packet.BossVisual,
				RayonDuCorps = packet.RayonDuCorps
			};
			Globals.Entities.Add(entity2.Id, entity2);
		}
	}

	public void HandlePacket(IPacketSender packetSender, ResourceEntityPacket packet)
	{
		if (Globals.TryGetEntity(EntityType.Resource, packet.EntityId, out Entity entity))
		{
			entity.Load(packet);
			return;
		}
		entity = new Resource(packet.EntityId, packet);
		if (!Globals.Entities.TryAdd(entity.Id, entity))
		{
			ApplicationContext.CurrentContext.Logger.LogError("Failed to register new {EntityType} {EntityId} ({EntityName})", EntityType.Resource, packet.EntityId, packet.Name);
		}
	}

	public void HandlePacket(IPacketSender packetSender, ProjectileEntityPacket packet)
	{
		Entity entity = Globals.GetEntity(packet.EntityId, EntityType.Projectile);
		if (entity != null)
		{
			entity.Load(packet);
			return;
		}
		Projectile projectile = new Projectile(packet.EntityId, packet);
		Globals.Entities.Add(projectile.Id, projectile);
	}

	public void HandlePacket(IPacketSender packetSender, EventEntityPacket packet)
	{
		MapInstance mapInstance = MapInstance.Get(packet.MapId);
		if (mapInstance != null)
		{
			mapInstance?.AddEvent(packet.EntityId, packet);
			return;
		}
		Dictionary<Guid, EventEntityPacket> dictionary = (Globals.PendingEvents.ContainsKey(packet.MapId) ? Globals.PendingEvents[packet.MapId] : new Dictionary<Guid, EventEntityPacket>());
		if (dictionary.ContainsKey(packet.EntityId))
		{
			dictionary[packet.EntityId] = packet;
		}
		else
		{
			dictionary.Add(packet.EntityId, packet);
		}
		if (!Globals.PendingEvents.ContainsKey(packet.MapId))
		{
			Globals.PendingEvents.Add(packet.MapId, dictionary);
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapEntitiesPacket entitiesPacket)
	{
		Dictionary<Guid, HashSet<Guid>> dictionary = new Dictionary<Guid, HashSet<Guid>>();
		EntityPacket[] mapEntities = entitiesPacket.MapEntities;
		foreach (EntityPacket entityPacket in mapEntities)
		{
			HandlePacket(entityPacket);
			if (!dictionary.TryGetValue(entityPacket.MapId, out var value))
			{
				value = new HashSet<Guid>();
				dictionary.Add(entityPacket.MapId, value);
			}
			value.Add(entityPacket.EntityId);
		}
		foreach (KeyValuePair<Guid, HashSet<Guid>> item in dictionary)
		{
			item.Deconstruct(out var key, out var value2);
			Guid guid = key;
			HashSet<Guid> hashSet = value2;
			foreach (KeyValuePair<Guid, Entity> entity2 in Globals.Entities)
			{
				entity2.Deconstruct(out key, out var value3);
				Guid guid2 = key;
				Entity entity = value3;
				if (!(entity.MapId != guid) && !hashSet.Contains(guid2) && !Globals.EntitiesToDispose.Contains(guid2) && entity != Globals.Me && !(entity is Projectile))
				{
					EntitesInconnues.NoterPerte(guid2, 3);
					Globals.EntitiesToDispose.Add(guid2);
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapInstanceChangedPacket packet)
	{
		List<Guid> list = new List<Guid>();
		EntityPacket[] entitiesToDispose = packet.EntitiesToDispose;
		foreach (EntityPacket entityPacket in entitiesToDispose)
		{
			HandlePacket(entityPacket);
			list.Add(entityPacket.EntityId);
		}
		foreach (Guid item in list)
		{
			Globals.EntitiesToDispose.Add(item);
		}
		foreach (Guid item2 in packet.MapIdsToRefresh)
		{
			MapInstance mapInstance = MapInstance.Get(item2);
			if (mapInstance != null)
			{
				Globals.EntitiesToDispose.AddRange(from en in mapInstance.LocalEntities.Values.ToList()
					select en.Id);
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityPositionPacket packet)
	{
		Guid id = packet.Id;
		ChuteDansLeVide.Finir(id);
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity;
		if (type != EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				if ((uint)type <= 1u)
				{
					EntitesInconnues.Noter(id, mapId);
				}
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			if (MapInstance.Get(mapId) == null || !MapInstance.Get(mapId).LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = MapInstance.Get(mapId).LocalEntities[id];
		}
		if (entity == Globals.Me)
		{
			ApplicationContext.Context.Value?.Logger.LogDebug($"received epp: {Timing.Global.Milliseconds}");
		}
		if (entity == Globals.Me && (Globals.Me.DashQueue.Count > 0 || Globals.Me.DashTimer > Timing.Global.Milliseconds))
		{
			if (Globals.Me.MapId == mapId)
			{
				return;
			}
			Globals.Me.DashQueue.Clear();
			Globals.Me.Dashing = null;
			Globals.Me.PendingDashMove = null;
			Globals.Me.DashTimer = 0L;
			Globals.Me.OffsetX = 0f;
			Globals.Me.OffsetY = 0f;
		}
		if (entity != Globals.Me && (entity.Dashing != null || entity.DashQueue.Count > 0 || entity.PendingDashMove != null))
		{
			entity.DashQueue.Clear();
			entity.Dashing = null;
			entity.PendingDashMove = null;
			entity.OffsetX = 0f;
			entity.OffsetY = 0f;
		}
		float mondeX;
		float mondeY;
		(float, float)? tuple = ((entity != Globals.Me && entity.EstPiloteLibrement && entity.PositionMonde(out mondeX, out mondeY)) ? new(float, float)?((mondeX, mondeY)) : (((float, float)?)null));
		if (entity == Globals.Me && Globals.Me.MapId != mapId)
		{
			Globals.Me.MapId = mapId;
			Globals.NeedsMaps = true;
			Player.FetchNewMaps();
		}
		else
		{
			entity.MapId = mapId;
		}
		if (entity.EstPiloteLibrement && entity == Globals.Me)
		{
			entity.OffsetX = 0f;
			entity.OffsetY = 0f;
			entity.IsMoving = false;
		}
		entity.X = packet.X;
		entity.Y = packet.Y;
		entity.DirectionFacing = (Direction)packet.Direction;
		entity.Passable = packet.Passable;
		entity.HideName = packet.HideName;
		if (tuple.HasValue)
		{
			(float, float) valueOrDefault = tuple.GetValueOrDefault();
			entity.RepasserEnCasesDepuis(valueOrDefault.Item1, valueOrDefault.Item2);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityLeftPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Guid guid = id;
		Guid? guid2 = Globals.Me?.Id;
		if (guid2.HasValue && guid == guid2.GetValueOrDefault() && type < EntityType.Event)
		{
			return;
		}
		if (type != EntityType.Event)
		{
			Dictionary<Guid, Entity> entities = Globals.Entities;
			if (entities != null && entities.ContainsKey(id))
			{
				EntitesInconnues.NoterPerte(id, 4);
				Globals.EntitiesToDispose?.Add(id);
			}
			return;
		}
		MapInstance mapInstance = MapInstance.Get(mapId);
		if (mapInstance?.LocalEntities?.ContainsKey(id) == true)
		{
			mapInstance.LocalEntities[id]?.Dispose();
			mapInstance.LocalEntities[id] = null;
			mapInstance.LocalEntities.Remove(id);
		}
	}

	public void HandlePacket(IPacketSender packetSender, ChatMsgPacket packet)
	{
		Color clr = new Color(packet.Color.A, packet.Color.R, packet.Color.G, packet.Color.B);
		if (!string.IsNullOrEmpty(packet.Langue))
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(packet.Message ?? "", clr, packet.Type, packet.Target ?? "", packet.MessageId, packet.Langue, packet.Original, packet.Etat, packet.PhraseId, packet.PhraseNom, packet.CorpsDebut));
		}
		else
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(packet.Message ?? "", clr, packet.Type, packet.Target));
		}
	}

	public void HandlePacket(IPacketSender packetSender, ChatReprisePacket packet)
	{
		ChatboxMsg.Reprendre(packet.MessageId, packet.Texte, packet.Etat);
	}

	public void HandlePacket(IPacketSender packetSender, AnnouncementPacket packet)
	{
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
		{
			gameInterface.Annoncer(packet.Message, packet.Duration);
		});
	}

	public void HandlePacket(IPacketSender packetSender, ActionMsgPackets packet)
	{
		ActionMsgPacket[] packets = packet.Packets;
		foreach (ActionMsgPacket packet2 in packets)
		{
			HandlePacket(packet2);
		}
	}

	public void HandlePacket(IPacketSender packetSender, ActionMsgPacket packet)
	{
		if (packet.EntityId != Guid.Empty && Globals.Entities.TryGetValue(packet.EntityId, out Entity value) && value is Resource ressource && Recolte.SurMessage(ressource, packet.Message))
		{
			return;
		}
		Globals.Entities.TryGetValue(packet.AttackerId, out Entity value2);
		if (value2 != null && value2 == Globals.Me && Globals.Entities.TryGetValue(packet.EntityId, out Entity value3))
		{
			Globals.Me.EchangeAvec(value3);
		}
		if (value2 != null)
		{
			Guid entityId = packet.EntityId;
			Guid? obj = Globals.Me?.Id;
			if (entityId == obj)
			{
				Globals.Me?.EchangeAvec(value2);
				CoupsDifferes.ViseMoi(value2.Id);
				goto IL_0123;
			}
		}
		if (CoupsDifferes.Differer(value2, Montrer))
		{
			return;
		}
		goto IL_0123;
		IL_0123:
		Montrer();
		void Montrer()
		{
			MapInstance mapInstance = MapInstance.Get(packet.MapId);
			if (mapInstance != null && (!Heritage.Actif || !Heritage.Chiffres.Prendre(packet)))
			{
				mapInstance.ActionMessages.Add(new ActionMessage(mapInstance, packet.X, packet.Y, packet.Message, new Color(packet.Color.A, packet.Color.R, packet.Color.G, packet.Color.B), packet.EntityId));
			}
			if (packet.EntityId != Guid.Empty)
			{
				string message = packet.Message;
				if (message != null && message.Length > 1 && message[0] == '-' && char.IsDigit(message[1]))
				{
					string s = new string(message.Skip(1).TakeWhile((char c) => char.IsDigit(c) || char.IsWhiteSpace(c)).Where(char.IsDigit)
						.ToArray());
					long.TryParse(s, out var result);
					CombatFeedback.OnHit(packet.EntityId, result, packet.AttackerId);
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, GameDataPacket packet)
	{
		GameObjectPacket[] gameObjects = packet.GameObjects;
		foreach (GameObjectPacket packet2 in gameObjects)
		{
			HandlePacket(packet2);
		}
		CustomColors.Load(packet.ColorsJson);
		Globals.HasGameData = true;
	}

	public void HandlePacket(IPacketSender packetSender, MapListPacket packet)
	{
		MapList.List.JsonData = packet.MapListData;
		MapList.List.PostLoad(DatabaseObject<MapDescriptor>.Lookup, isServer: false, isTopLevel: true);
		Intersect.Client.Interface.Interface.GameUi.UpdateAdminWindowMapList();
	}

	public void HandlePacket(IPacketSender packetSender, EntityMovementPackets packet)
	{
		if (packet.GlobalMovements != null)
		{
			EntityMovePacket[] globalMovements = packet.GlobalMovements;
			foreach (EntityMovePacket packet2 in globalMovements)
			{
				HandlePacket(packet2);
			}
		}
		if (packet.LocalMovements != null)
		{
			EntityMovePacket[] localMovements = packet.LocalMovements;
			foreach (EntityMovePacket packet3 in localMovements)
			{
				HandlePacket(packet3);
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityMovePacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				if ((uint)type <= 1u)
				{
					EntitesInconnues.Noter(id, mapId);
				}
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity == null)
		{
			return;
		}
		MapInstance mapInstance2 = MapInstance.Get(entity.MapId);
		if (mapInstance2 == null)
		{
			return;
		}
		if (entity is Player && Options.Instance.Combat.MovementCancelsCast)
		{
			entity.CastTime = 0L;
		}
		if (entity != Globals.Me && entity.EstPiloteLibrement)
		{
			entity.OublierLeDeplacementLibre();
		}
		entity.InterrompreLeRecentrage();
		if (entity.Dashing != null || entity.DashQueue.Count > 0)
		{
			entity.PendingDashMove = packet;
			return;
		}
		Guid guid = mapId;
		byte x = packet.X;
		byte y = packet.Y;
		Direction direction = (Direction)packet.Direction;
		bool correction = packet.Correction;
		if ((entity.MapId != guid || entity.X != x || entity.Y != y) && (entity != Globals.Me || (entity == Globals.Me && correction)) && entity.Dashing == null)
		{
			float offsetX = entity.OffsetX;
			float offsetY = entity.OffsetY;
			Guid mapId2 = entity.MapId;
			byte x2 = entity.X;
			byte y2 = entity.Y;
			entity.MapId = guid;
			entity.X = x;
			entity.Y = y;
			entity.DirectionFacing = direction;
			if (entity is Player player)
			{
				player.DirectionMoving = direction;
			}
			entity.IsMoving = true;
			switch (entity.DirectionFacing)
			{
			case Direction.Up:
				entity.OffsetY = Options.Instance.Map.TileWidth;
				entity.OffsetX = 0f;
				break;
			case Direction.Down:
				entity.OffsetY = -Options.Instance.Map.TileWidth;
				entity.OffsetX = 0f;
				break;
			case Direction.Left:
				entity.OffsetY = 0f;
				entity.OffsetX = Options.Instance.Map.TileWidth;
				break;
			case Direction.Right:
				entity.OffsetY = 0f;
				entity.OffsetX = -Options.Instance.Map.TileWidth;
				break;
			case Direction.UpLeft:
				entity.OffsetY = Options.Instance.Map.TileHeight;
				entity.OffsetX = Options.Instance.Map.TileWidth;
				break;
			case Direction.UpRight:
				entity.OffsetY = Options.Instance.Map.TileHeight;
				entity.OffsetX = -Options.Instance.Map.TileWidth;
				break;
			case Direction.DownLeft:
				entity.OffsetY = -Options.Instance.Map.TileHeight;
				entity.OffsetX = Options.Instance.Map.TileWidth;
				break;
			case Direction.DownRight:
				entity.OffsetY = -Options.Instance.Map.TileHeight;
				entity.OffsetX = -Options.Instance.Map.TileWidth;
				break;
			}
			if (entity != Globals.Me)
			{
				if (TryExactStepOffset(mapId2, x2, y2, offsetX, offsetY, guid, x, y, out var offsetX2, out var offsetY2))
				{
					entity.OffsetX = offsetX2;
					entity.OffsetY = offsetY2;
				}
				else
				{
					entity.OffsetX = CombineStepOffsets(entity.OffsetX, offsetX);
					entity.OffsetY = CombineStepOffsets(entity.OffsetY, offsetY);
				}
				entity.NoteRemoteStep();
			}
		}
		if (mapInstance2.Attributes[entity.X, entity.Y] != null && mapInstance2.Attributes[entity.X, entity.Y].Type == MapAttributeType.ZDimension && ((MapZDimensionAttribute)mapInstance2.Attributes[entity.X, entity.Y]).GatewayTo > 0)
		{
			entity.Z = (byte)(((MapZDimensionAttribute)mapInstance2.Attributes[entity.X, entity.Y]).GatewayTo - 1);
		}
	}

	private static bool TryExactStepOffset(Guid previousMapId, int previousX, int previousY, float carriedX, float carriedY, Guid mapId, int x, int y, out float offsetX, out float offsetY)
	{
		offsetX = 0f;
		offsetY = 0f;
		if (!MapInstance.TryGet(previousMapId, out MapInstance instance) || !MapInstance.TryGet(mapId, out MapInstance instance2))
		{
			return false;
		}
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		offsetX = (float)(instance.X + previousX * tileWidth) + carriedX - (float)(instance2.X + x * tileWidth);
		offsetY = (float)(instance.Y + previousY * tileHeight) + carriedY - (float)(instance2.Y + y * tileHeight);
		if (Math.Abs(offsetX) <= (float)(2 * tileWidth))
		{
			return Math.Abs(offsetY) <= (float)(2 * tileHeight);
		}
		return false;
	}

	private static float CombineStepOffsets(float stepOffset, float carried)
	{
		if (carried == 0f)
		{
			return stepOffset;
		}
		if (stepOffset != 0f && Math.Sign(stepOffset) != Math.Sign(carried))
		{
			return stepOffset;
		}
		float val = 2f * (float)Math.Max(Options.Instance.Map.TileWidth, Options.Instance.Map.TileHeight);
		float value = stepOffset + carried;
		return (float)Math.Sign(value) * Math.Min(Math.Abs(value), val);
	}

	public void HandlePacket(IPacketSender packetSender, MapEntityVitalsPacket packet)
	{
		MapInstance mapInstance = MapInstance.Get(packet.MapId);
		if (mapInstance == null)
		{
			return;
		}
		EntityVitalData[] entityUpdates = packet.EntityUpdates;
		foreach (EntityVitalData entityVitalData in entityUpdates)
		{
			Entity entity = null;
			if (entityVitalData.Type < EntityType.Event)
			{
				if (!Globals.Entities.ContainsKey(entityVitalData.Id))
				{
					EntityType type = entityVitalData.Type;
					if ((uint)type <= 1u)
					{
						EntitesInconnues.Noter(entityVitalData.Id, packet.MapId);
					}
					continue;
				}
				entity = Globals.Entities[entityVitalData.Id];
			}
			else
			{
				if (!mapInstance.LocalEntities.ContainsKey(entityVitalData.Id))
				{
					continue;
				}
				entity = mapInstance.LocalEntities[entityVitalData.Id];
			}
			if (entity != null)
			{
				entity.Vital = entityVitalData.Vitals;
				entity.MaxVital = entityVitalData.MaxVitals;
				if (entity == Globals.Me && entityVitalData.CombatTimeRemaining > 0)
				{
					Globals.Me.CombatTimer = Timing.Global.Milliseconds + entityVitalData.CombatTimeRemaining;
				}
				if (entity is Resource resource)
				{
					resource.UpdateCurrentState();
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapEntityStatusPacket packet)
	{
		MapInstance mapInstance = MapInstance.Get(packet.MapId);
		if (mapInstance == null)
		{
			return;
		}
		EntityStatusData[] entityUpdates = packet.EntityUpdates;
		foreach (EntityStatusData entityStatusData in entityUpdates)
		{
			Entity entity = null;
			if (entityStatusData.Type < EntityType.Event)
			{
				if (!Globals.Entities.ContainsKey(entityStatusData.Id))
				{
					continue;
				}
				entity = Globals.Entities[entityStatusData.Id];
			}
			else
			{
				if (!mapInstance.LocalEntities.ContainsKey(entityStatusData.Id))
				{
					continue;
				}
				entity = mapInstance.LocalEntities[entityStatusData.Id];
			}
			if (entity == null)
			{
				continue;
			}
			entity.Status.Clear();
			StatusPacket[] statuses = entityStatusData.Statuses;
			foreach (StatusPacket statusPacket in statuses)
			{
				Status status = new Status(statusPacket.SpellId, statusPacket.Type, statusPacket.TransformSprite, statusPacket.TimeRemaining, statusPacket.TotalDuration)
				{
					Sequence = statusPacket.Sequence
				};
				entity.Status.Add(status);
				if (status.Type == SpellEffect.Stun || status.Type == SpellEffect.Silence)
				{
					entity.CastTime = 0L;
				}
				else if (status.Type == SpellEffect.Shield)
				{
					status.Shield = statusPacket.VitalShields;
				}
			}
			entity.SortStatuses();
			if (Intersect.Client.Interface.Interface.HasInGameUI)
			{
				if (entityStatusData.Id == Globals.Me.Id && Intersect.Client.Interface.Interface.GameUi.PlayerStatusWindow != null)
				{
					Intersect.Client.Interface.Interface.GameUi.PlayerStatusWindow.ShouldUpdateStatuses = true;
				}
				else if (entityStatusData.Id == Globals.Me.TargetId && Globals.Me.TargetBox != null)
				{
					Globals.Me.TargetBox.ShouldUpdateStatuses = true;
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityVitalsPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				if ((uint)type <= 1u)
				{
					EntitesInconnues.Noter(id, mapId);
				}
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity == null)
		{
			return;
		}
		entity.Vital = packet.Vitals;
		entity.MaxVital = packet.MaxVitals;
		if (entity == Globals.Me && packet.CombatTimeRemaining > 0)
		{
			Globals.Me.CombatTimer = Timing.Global.Milliseconds + packet.CombatTimeRemaining;
		}
		entity.Status.Clear();
		StatusPacket[] statusEffects = packet.StatusEffects;
		foreach (StatusPacket statusPacket in statusEffects)
		{
			Status status = new Status(statusPacket.SpellId, statusPacket.Type, statusPacket.TransformSprite, statusPacket.TimeRemaining, statusPacket.TotalDuration)
			{
				Sequence = statusPacket.Sequence
			};
			entity.Status.Add(status);
			if (status.Type == SpellEffect.Stun || status.Type == SpellEffect.Silence)
			{
				entity.CastTime = 0L;
			}
			else if (status.Type == SpellEffect.Shield)
			{
				status.Shield = statusPacket.VitalShields;
			}
		}
		entity.SortStatuses();
		if (Intersect.Client.Interface.Interface.HasInGameUI)
		{
			if (id == Globals.Me.Id && Intersect.Client.Interface.Interface.GameUi.PlayerStatusWindow != null)
			{
				Intersect.Client.Interface.Interface.GameUi.PlayerStatusWindow.ShouldUpdateStatuses = true;
			}
			else if (id == Globals.Me.TargetId && Globals.Me.TargetBox != null)
			{
				Globals.Me.TargetBox.ShouldUpdateStatuses = true;
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityStatsPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity != null)
		{
			entity.Stat = packet.Stats;
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityDirectionPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				if ((uint)type <= 1u)
				{
					EntitesInconnues.Noter(id, mapId);
				}
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity != null && (!entity.EstPiloteLibrement || entity == Globals.Me))
		{
			entity.DirectionFacing = (Direction)packet.Direction;
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityAttackPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		int attackTimer = packet.AttackTimer;
		Entity en = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				if ((uint)type <= 1u)
				{
					EntitesInconnues.Noter(id, mapId);
				}
				return;
			}
			en = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			en = mapInstance.LocalEntities[id];
		}
		bool isSelf;
		if (en != null)
		{
			isSelf = en == Globals.Me;
			if (isSelf || attackTimer <= -1 || !CoupsDifferes.Differer(en, Appliquer))
			{
				Appliquer();
			}
		}
		void Appliquer()
		{
			if (!Options.Instance.Combat.EnableGuard)
			{
				en.IsBlocking = packet.IsBlocking;
			}
			if (!isSelf && attackTimer > -1)
			{
				en.DirectionFacing = (Direction)packet.Direction;
			}
			if (attackTimer > -1 && (!Heritage.Actif || !Heritage.EchoSansRelance(en, packet.Pas, packet.Coup, attackTimer)))
			{
				if (!isSelf || !en.IsAttacking)
				{
					en.PasDuCoup = packet.Pas;
				}
				if (isSelf && en is Player player)
				{
					player.RecalerLePas(packet.Pas, attackTimer);
				}
				en.AttackTimer = Timing.Global.Milliseconds + attackTimer;
				if (Heritage.Actif)
				{
					Heritage.NoterLeGesteDeLEcho(en, packet.Coup);
				}
				if (!isSelf)
				{
					en.AttackTime = attackTimer;
					if (!packet.IsBlocking && !(en is Event) && !Recolte.FaitFaceARessource(en))
					{
						if (Heritage.Actif)
						{
							Heritage.SouffleDUnAutre(en, packet.Coup);
						}
						else
						{
							CombatAudio.Swing(en);
						}
					}
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityFlightPacket packet)
	{
		if (Globals.Entities.TryGetValue(packet.Id, out Entity value) && value != null)
		{
			Flight.OnPacket(value, packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntitySprintPacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity != null && entity != Globals.Me)
		{
			entity.IsSprinting = packet.Sprinting;
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityChargePacket packet)
	{
		if (packet.Type >= EntityType.Event || !Globals.Entities.TryGetValue(packet.Id, out Entity value) || value == null)
		{
			return;
		}
		if (value == Globals.Me)
		{
			if (!packet.Charging && value.IsCharging && value is Player player)
			{
				player.ChargeCoupeeParLeServeur();
			}
		}
		else
		{
			value.IsCharging = packet.Charging;
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityBeamPacket packet)
	{
		if (packet.Type < EntityType.Event && Globals.Entities.TryGetValue(packet.Id, out Entity value) && value != null)
		{
			KiBeam.OnServerState(value, packet.Phase, packet.Angle, packet.Level, packet.TargetId, packet.ReachTiles, packet.Courbe);
		}
	}

	public void HandlePacket(IPacketSender packetSender, NpcNoticePacket packet)
	{
		if (packet.Genre != NpcNoticeGenre.Remarque)
		{
			Globals.Entities.TryGetValue(packet.Id, out Entity value);
			if (packet.Cible != Guid.Empty)
			{
				Guid cible = packet.Cible;
				Guid? obj = Globals.Me?.Id;
				if (cible == obj)
				{
					CoupsDifferes.ViseMoi(packet.Id);
					goto IL_00ba;
				}
			}
			if (CoupsDifferes.Differer(value, delegate
			{
				Telegraphe.Recevoir(packet);
			}))
			{
				return;
			}
			goto IL_00ba;
		}
		if (Globals.Entities.ContainsKey(packet.Id))
		{
			CombatFeedback.Callout(packet.Id, "!", new Color(255, 255, 90, 80), 14);
		}
		return;
		IL_00ba:
		Telegraphe.Recevoir(packet);
	}

	public void HandlePacket(IPacketSender packetSender, WorldMapPacket packet)
	{
		Globals.WorldMapDestinations = packet.Destinations ?? Array.Empty<WorldMapDestination>();
		WorldMapDestination[] worldMapDestinations = Globals.WorldMapDestinations;
		foreach (WorldMapDestination worldMapDestination in worldMapDestinations)
		{
			worldMapDestination.Name = Traduction.Contenu(worldMapDestination.Name);
		}
		HashSet<Guid> hashSet = new HashSet<Guid>();
		Guid[] array = packet.VisitedMaps ?? Array.Empty<Guid>();
		foreach (Guid item in array)
		{
			hashSet.Add(item);
		}
		Globals.VisitedMaps = hashSet;
		Intersect.Client.Interface.Interface.GameUi?.RefreshWorldMapDestinations();
		if (packet.RetourDEnma)
		{
			Intersect.Client.Interface.Interface.GameUi?.OuvrirCarteDuRetour();
		}
	}

	public void HandlePacket(IPacketSender packetSender, DragonBallsPacket packet)
	{
		Globals.DragonBalls = packet.Owned ?? Array.Empty<bool>();
	}

	public void HandlePacket(IPacketSender packetSender, AchievementsPacket packet)
	{
		Globals.Achievements = packet.Achievements ?? Array.Empty<AchievementProgress>();
		AchievementProgress[] achievements = Globals.Achievements;
		foreach (AchievementProgress objet in achievements)
		{
			Traduction.Objet(objet);
		}
		Globals.AchievementPointsEarned = packet.EarnedPoints;
		Globals.AchievementPointsTotal = packet.TotalPoints;
		Annonces.ObserverSucces();
		Intersect.Client.Interface.Interface.GameUi?.RefreshAchievementsWindow();
	}

	public void HandlePacket(IPacketSender packetSender, AchievementUnlockedPacket packet)
	{
		string nom = Traduction.Contenu(packet.Name);
		string msg = Strings.Achievements.Unlocked.ToString(nom, packet.Points);
		ChatboxMsg.AddMessage(new ChatboxMsg(msg, CustomColors.Alerts.Accepted, ChatMessageType.Notice));
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
		{
			gameInterface.ShowAchievementBanner(nom, packet.Points);
		});
		Intersect.Client.Interface.Interface.GameUi?.GameMenu?.Notify(MenuBadge.Progression);
	}

	public void HandlePacket(IPacketSender packetSender, DailyRewardsPacket packet)
	{
		Globals.DailyRewards = packet.Rewards ?? Array.Empty<DailyRewardEntry>();
		Globals.DailyRewardDay = packet.Day;
		Globals.DailyRewardCanClaim = packet.CanClaim;
		Globals.DailyRewardTotal = packet.TotalClaimed;
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
		{
			gameInterface.OnDailyRewards(packet.CanClaim);
		});
	}

	public void HandlePacket(IPacketSender packetSender, DailyRewardGrantedPacket packet)
	{
		string msg = Strings.DailyRewards.Granted.ToString(packet.Day, packet.Label);
		ChatboxMsg.AddMessage(new ChatboxMsg(msg, CustomColors.Alerts.Accepted, ChatMessageType.Notice));
	}

	public void HandlePacket(IPacketSender packetSender, MarketListingsPacket packet)
	{
		Intersect.Client.Interface.Interface.GameUi?.OnMarketListings(packet);
	}

	public void HandlePacket(IPacketSender packetSender, MarketMyListingsResultPacket packet)
	{
		Intersect.Client.Interface.Interface.GameUi?.OnMarketMyListings(packet);
	}

	public void HandlePacket(IPacketSender packetSender, MarketPricesResultPacket packet)
	{
		MarketPrices.OnResult(packet);
	}

	public void HandlePacket(IPacketSender packetSender, WalletPacket packet)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			me.Zeni = packet.Zeni;
			me.CurrencyItemId = packet.CurrencyItemId;
		}
	}

	public void HandlePacket(IPacketSender packetSender, KarmaPacket packet)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			KarmaOptions karma = Options.Instance.Karma;
			KarmaTier karmaTier = karma.TierOf(me.Karma);
			KarmaTier karmaTier2 = karma.TierOf(packet.Karma);
			bool karmaKnown = me.KarmaKnown;
			me.Karma = packet.Karma;
			me.KarmaKnown = true;
			if (karmaKnown && karmaTier2 != karmaTier)
			{
				Intersect.Client.Interface.Interface.GameUi?.ShowKarmaTierBanner(karmaTier2, packet.Karma);
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, KarmaStatusPacket packet)
	{
		if (Globals.Entities.TryGetValue(packet.PlayerId, out Entity value) && value is Player player)
		{
			player.KarmaTier = (KarmaTier)packet.Tier;
			player.AggressorUntil = ((packet.AggressorRemainingMs > 0) ? (Timing.Global.Milliseconds + packet.AggressorRemainingMs) : 0);
		}
	}

	public void HandlePacket(IPacketSender packetSender, HellStatusPacket packet)
	{
		Player me = Globals.Me;
		if (me != null)
		{
			me.HellSentenceUntil = ((packet.SentenceMs > 0) ? (Timing.Global.Milliseconds + packet.SentenceMs) : 0);
			me.HellBail = packet.Bail;
			me.DetentionPlace = packet.Place;
		}
	}

	public void HandlePacket(IPacketSender packetSender, HellBailOfferPacket packet)
	{
		HellBailWindow.Open(packet.Bail, packet.Place);
	}

	public void HandlePacket(IPacketSender packetSender, TrackingRadarPacket packet)
	{
		TrackingRadarWindow.Open(packet.Entries ?? Array.Empty<TrackingRadarEntryPacket>(), packet.Radars);
	}

	public void HandlePacket(IPacketSender packetSender, TrackingRadarResultPacket packet)
	{
		TrackingRadarWindow.Current?.ShowResult(packet.TargetId, packet.Result, packet.MapName, packet.Radars);
	}

	public void HandlePacket(IPacketSender packetSender, WantedBoardPacket packet)
	{
		WantedBoardWindow.Open(packet);
	}

	public void HandlePacket(IPacketSender packetSender, WantedContractsPacket packet)
	{
		Globals.WantedContracts = packet.Entries ?? Array.Empty<WantedContractEntryPacket>();
		Globals.WantedContractsAt = Timing.Global.MillisecondsUtc;
	}

	public void HandlePacket(IPacketSender packetSender, WantedTurnInPacket packet)
	{
		WantedTurnInWindow.Open(packet);
	}

	public void HandlePacket(IPacketSender packetSender, WantedTurnInResultPacket packet)
	{
		WantedTurnInWindow.Current?.ShowResult(packet);
	}

	public void HandlePacket(IPacketSender packetSender, RubanRougePacket packet)
	{
		RubanRougeWindow.Open(packet);
	}

	public void HandlePacket(IPacketSender packetSender, CasseEtatsPacket packet)
	{
		Casse.Recevoir(packet);
	}

	public void HandlePacket(IPacketSender packetSender, CasseCoupPacket packet)
	{
		Casse.Recevoir(packet);
	}

	public void HandlePacket(IPacketSender packetSender, CasseSillonPacket packet)
	{
		if (!Heritage.Actif || !Heritage.Marques.Recevoir(packet))
		{
			Casse.Recevoir(packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, RecherchePacket packet)
	{
		RecherchePanel.Recevoir(packet);
	}

	public void HandlePacket(IPacketSender packetSender, RibbonContractHeldPacket packet)
	{
		Globals.RibbonContract = (packet.Has ? packet.Entry : null);
		Globals.RibbonContractAt = Timing.Global.MillisecondsUtc;
	}

	public void HandlePacket(IPacketSender packetSender, MarketResultPacket packet)
	{
		bool windowOpen = Intersect.Client.Interface.Interface.GameUi?.IsMarketOpen ?? false;
		(string Text, bool Success, bool ToChat) tuple = MarketWindow.Describe(packet, windowOpen);
		var (text, flag, _) = tuple;
		if (tuple.ToChat && !string.IsNullOrEmpty(text))
		{
			Color clr = (flag ? CustomColors.Alerts.Accepted : new Color(255, 255, 172, 82));
			ChatboxMsg.AddMessage(new ChatboxMsg(text, clr, ChatMessageType.Notice));
		}
		Intersect.Client.Interface.Interface.GameUi?.OnMarketResult(packet, text, flag);
	}

	public void HandlePacket(IPacketSender packetSender, PlayerTitlesPacket packet)
	{
		Globals.Me?.SetTitles(packet.TitleIds, packet.ActiveTitleId);
	}

	public void HandlePacket(IPacketSender packetSender, PlayerOrnamentsPacket packet)
	{
		Globals.Me?.SetOrnaments(packet.OrnamentIds, packet.ActiveOrnamentId);
	}

	public void HandlePacket(IPacketSender packetSender, PlayerProfessionsPacket packet)
	{
		Globals.Me?.SetProfessions(packet);
	}

	public void HandlePacket(IPacketSender packetSender, ItemPickedUpPacket packet)
	{
		if (packet.Properties != null || packet.Precise)
		{
			LootFeedback.OnCrafted(packet.ItemId, packet.Quantity, packet.Properties, packet.Precise);
		}
		else
		{
			LootFeedback.OnPickup(packet.ItemId, packet.Quantity);
		}
	}

	public void HandlePacket(IPacketSender packetSender, CraftingActivityPacket packet)
	{
		Artisanat.SurActivite(packet);
	}

	public void HandlePacket(IPacketSender packetSender, GuardPacket packet)
	{
		CombatFeedback.OnGuard(packet);
		if (Heritage.Actif)
		{
			Heritage.Rang.SurGarde(packet);
			Heritage.Garde.SurGarde(packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, VanishPacket packet)
	{
		Vanish.OnPacket(packet);
		if (Heritage.Actif)
		{
			Heritage.Rang.SurEsquive(packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, MetierNiveauPacket packet)
	{
		if (Globals.Entities.TryGetValue(packet.EntityId, out Entity value) && value != Globals.Me && Professions.IsValid(packet.Metier))
		{
			ProfessionLevelUp.Begin(value, packet.Metier, packet.Niveau);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EchangePacket packet)
	{
		if (!Maitres.OnEchange(packet))
		{
			Echanges.OnEchange(packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, KiClashPacket packet)
	{
		KiBlast.OnClash(packet);
	}

	public void HandlePacket(IPacketSender packetSender, CombatImpactPacket packet)
	{
		Globals.Entities.TryGetValue(packet.AttackerId, out Entity value);
		Guid targetId = packet.TargetId;
		Guid? obj = Globals.Me?.Id;
		if (!(targetId != obj) || !CoupsDifferes.Differer(value, delegate
		{
			CombatFeedback.OnImpact(packet);
		}))
		{
			CombatFeedback.OnImpact(packet);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityDiePacket packet)
	{
		Guid id = packet.Id;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		Entity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(id))
			{
				return;
			}
			entity = Globals.Entities[id];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(id))
			{
				return;
			}
			entity = mapInstance.LocalEntities[id];
		}
		if (entity != null)
		{
			entity.ClearAnimations();
			CombatFeedback.OnDeath(entity);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EventDialogPacket packet)
	{
		Dialog dialog = new Dialog()
		{
			Prompt = Touches.Remplacer(Traduction.Contenu(packet.Prompt)),
			Face = packet.Face,
		};
		if (packet.Type != 0)
		{
			dialog.Options = packet.Responses?.Select((string reponse) => Touches.Remplacer(Traduction.Contenu(reponse))).ToArray();
		}
		dialog.EventId = packet.EventId;
		Globals.EventDialogs.Add(dialog);
	}

	public void HandlePacket(IPacketSender packetSender, InputVariablePacket packet)
	{
		InputType inputType = packet.Type switch
		{
			VariableDataType.String => InputType.TextInput, 
			VariableDataType.Boolean => InputType.YesNoCancel, 
			_ => InputType.NumericInput, 
		};
		new InputBox(Traduction.Contenu(packet.Title), Traduction.Contenu(packet.Prompt), inputType, userData: packet.EventId, onSubmit: PacketSender.SendEventInputVariable, onCancel: PacketSender.SendEventInputVariableCancel);
	}

	public void HandlePacket(IPacketSender packetSender, ErrorMessagePacket packet)
	{
		Fade.Cancel();
		Globals.WaitingOnServer = false;
		MenuGuiBase? menuUiIfReady = Intersect.Client.Interface.Interface.MenuUiIfReady;
		if (menuUiIfReady == null || !menuUiIfReady.MainMenu.TryShowErrorInline(packet.Error))
		{
			Intersect.Client.Interface.Interface.ShowAlert(packet.Error, packet.Header);
		}
		Intersect.Client.Interface.Interface.MenuUiIfReady?.Reset();
	}

	public void HandlePacket(IPacketSender packetSender, MapItemsPacket packet)
	{
		MapInstance mapInstance = MapInstance.Get(packet.MapId);
		if (mapInstance == null)
		{
			return;
		}
		mapInstance.MapItems.Clear();
		MapItemUpdatePacket[] items = packet.Items;
		foreach (MapItemUpdatePacket mapItemUpdatePacket in items)
		{
			MapItemInstance mapItemInstance = new MapItemInstance(mapItemUpdatePacket.TileIndex, mapItemUpdatePacket.Id, mapItemUpdatePacket.ItemId, mapItemUpdatePacket.BagId, mapItemUpdatePacket.Quantity, mapItemUpdatePacket.Properties);
			if (!mapInstance.MapItems.ContainsKey(mapItemInstance.TileIndex))
			{
				mapInstance.MapItems.Add(mapItemInstance.TileIndex, new List<IMapItemInstance>());
			}
			mapInstance.MapItems[mapItemInstance.TileIndex].Add(mapItemInstance);
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapItemUpdatePacket packet)
	{
		MapInstance mapInstance = MapInstance.Get(packet.MapId);
		if (mapInstance == null)
		{
			return;
		}
		if (packet.ItemId == Guid.Empty)
		{
			foreach (int key in mapInstance.MapItems.Keys)
			{
				IMapItemInstance mapItemInstance = mapInstance.MapItems[key].Where((IMapItemInstance item) => item.Id == packet.Id).SingleOrDefault();
				if (mapItemInstance != null)
				{
					mapInstance.MapItems[key].Remove(mapItemInstance);
				}
			}
			return;
		}
		if (!mapInstance.MapItems.ContainsKey(packet.TileIndex))
		{
			mapInstance.MapItems.Add(packet.TileIndex, new List<IMapItemInstance>());
		}
		MapItemInstance mapItem = new MapItemInstance(packet.TileIndex, packet.Id, packet.ItemId, packet.BagId, packet.Quantity, packet.Properties);
		if (mapInstance.MapItems[packet.TileIndex].Any((IMapItemInstance item) => item.Id == mapItem.Id))
		{
			for (int num = 0; num < mapInstance.MapItems[packet.TileIndex].Count; num++)
			{
				if (mapInstance.MapItems[packet.TileIndex][num].Id == mapItem.Id)
				{
					mapInstance.MapItems[packet.TileIndex][num] = mapItem;
				}
			}
		}
		else
		{
			mapInstance.MapItems[packet.TileIndex].Add(mapItem);
		}
	}

	public void HandlePacket(IPacketSender packetSender, InventoryPacket packet)
	{
		InventoryUpdatePacket[] slots = packet.Slots;
		foreach (InventoryUpdatePacket packet2 in slots)
		{
			HandlePacket(packet2);
		}
		PanoplieSuivi.ApresSacComplet();
	}

	public void HandlePacket(IPacketSender packetSender, InventoryUpdatePacket packet)
	{
		Globals.Me?.UpdateInventory(packet.Slot, packet.ItemId, packet.Quantity, packet.BagId, packet.Properties);
	}

	public void HandlePacket(IPacketSender packetSender, SpellsPacket packet)
	{
		SpellUpdatePacket[] slots = packet.Slots;
		foreach (SpellUpdatePacket packet2 in slots)
		{
			HandlePacket(packet2);
		}
	}

	public void HandlePacket(IPacketSender packetSender, SpellUpdatePacket packet)
	{
		if (Globals.Me != null)
		{
			Spell spell = Globals.Me.Spells[packet.Slot];
			Guid id = spell.Id;
			int rank = spell.Rank;
			spell.Load(packet.SpellId, packet.Rank, packet.Experience);
			TechniqueProgress.Observe(packet.Slot, id, rank, spell);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EquipmentPacket packet)
	{
		Guid entityId = packet.EntityId;
		if (!Globals.Entities.ContainsKey(entityId))
		{
			return;
		}
		Entity entity = Globals.Entities[entityId];
		if (entity != null)
		{
			if (entity == Globals.Me && packet.InventorySlots != null)
			{
				entity.MyEquipment = packet.InventorySlots;
				PanoplieSuivi.ApresEquipement();
			}
			else if (packet.ItemIds != null)
			{
				entity.Equipment = packet.ItemIds;
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, StatPointsPacket packet)
	{
		if (Globals.Me != null)
		{
			Globals.Me.StatPoints = packet.Points;
		}
	}

	public void HandlePacket(IPacketSender packetSender, HotbarPacket packet)
	{
		for (int i = 0; i < Options.Instance.Player.HotbarSlotCount; i++)
		{
			if (Globals.Me == null)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Can't set hotbar, Globals.Me is null!");
				break;
			}
			if (Globals.Me.Hotbar == null)
			{
				ApplicationContext.Context.Value?.Logger.LogDebug("Can't set hotbar, hotbar is null!");
				break;
			}
			HotbarInstance hotbarInstance = Globals.Me.Hotbar[i];
			hotbarInstance.Load(packet.SlotData[i]);
		}
	}

	public void HandlePacket(IPacketSender packetSender, CharacterCreationPacket packet)
	{
		Globals.WaitingOnServer = false;
		Intersect.Client.Interface.Interface.MenuUi.MainMenu.NotifyOpenCharacterCreation(packet.Force);
	}

	public void HandlePacket(IPacketSender packetSender, SpellCastPacket packet)
	{
		Guid entityId = packet.EntityId;
		Guid spellId = packet.SpellId;
		if (DatabaseObject<SpellDescriptor>.Get(spellId) != null && Globals.Entities.ContainsKey(entityId))
		{
			int num = ((packet.Duration >= 0) ? packet.Duration : DatabaseObject<SpellDescriptor>.Get(spellId).CastDuration);
			Globals.Entities[entityId].CastTime = Timing.Global.Milliseconds + num;
			Globals.Entities[entityId].CastDuration = num;
			Globals.Entities[entityId].SpellCast = spellId;
		}
	}

	public void HandlePacket(IPacketSender packetSender, SpellCooldownPacket packet)
	{
		foreach (KeyValuePair<Guid, long> spellCd in packet.SpellCds)
		{
			long value = Timing.Global.Milliseconds + spellCd.Value;
			if (!Globals.Me.SpellCooldowns.ContainsKey(spellCd.Key))
			{
				Globals.Me.SpellCooldowns.Add(spellCd.Key, value);
			}
			else
			{
				Globals.Me.SpellCooldowns[spellCd.Key] = value;
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, ItemCooldownPacket packet)
	{
		foreach (KeyValuePair<Guid, long> itemCd in packet.ItemCds)
		{
			long value = Timing.Global.Milliseconds + itemCd.Value;
			if (!Globals.Me.ItemCooldowns.ContainsKey(itemCd.Key))
			{
				Globals.Me.ItemCooldowns.Add(itemCd.Key, value);
			}
			else
			{
				Globals.Me.ItemCooldowns[itemCd.Key] = value;
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, GlobalCooldownPacket packet)
	{
		Globals.Me.GlobalCooldown = Timing.Global.Milliseconds + packet.GlobalCooldown;
	}

	public void HandlePacket(IPacketSender packetSender, ExperiencePacket packet)
	{
		if (Globals.Me != null)
		{
			long num = ((!Globals.Me.ExperienceReceived) ? 0 : ((packet.Experience >= Globals.Me.Experience) ? (packet.Experience - Globals.Me.Experience) : (Math.Max(0L, Globals.Me.ExperienceToNextLevel - Globals.Me.Experience) + packet.Experience)));
			Globals.Me.ExperienceReceived = true;
			if (num > 0)
			{
				CombatFeedback.OnExperience(num);
				ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Banners.ExperienceGainedChat.ToString(MarketWindow.FormatAmount(num)), new Color(255, 245, 190, 90), ChatMessageType.Experience));
			}
			Globals.Me.Experience = packet.Experience;
			Globals.Me.ExperienceToNextLevel = packet.ExperienceToNextLevel;
		}
	}

	public void HandlePacket(IPacketSender packetSender, ProjectileDeadPacket packet)
	{
		if (packet.ProjectileDeaths != null)
		{
			Guid[] projectileDeaths = packet.ProjectileDeaths;
			foreach (Guid guid in projectileDeaths)
			{
				if (Globals.Entities.ContainsKey(guid) && Globals.Entities[guid] is Projectile)
				{
					Globals.EntitiesToDispose?.Add(guid);
				}
			}
		}
		if (packet.SpawnDeaths == null)
		{
			return;
		}
		Guid[] spawnHits = packet.SpawnHits;
		Guid[] array = ((spawnHits != null && spawnHits.Length == packet.SpawnDeaths.Length) ? spawnHits : null);
		for (int j = 0; j < packet.SpawnDeaths.Length; j++)
		{
			KeyValuePair<Guid, int> keyValuePair = packet.SpawnDeaths[j];
			if (Globals.Entities.ContainsKey(keyValuePair.Key) && Globals.Entities[keyValuePair.Key] is Projectile projectile)
			{
				projectile.SpawnDead(keyValuePair.Value, (array != null) ? new Guid?(array[j]) : ((Guid?)null));
			}
		}
	}

	public void HandlePacket(IPacketSender sender, PlayAnimationPackets packet)
	{
		PlayAnimationPacket[] packets = packet.Packets;
		foreach (PlayAnimationPacket packet2 in packets)
		{
			HandlePacket(packet2);
		}
	}

	public void HandlePacket(IPacketSender packetSender, PlayAnimationPacket packet)
	{
		Guid mapId = packet.MapId;
		Guid animationId = packet.AnimationId;
		int targetType = packet.TargetType;
		Guid entityId = packet.EntityId;
		AnimationSource animationSource = new AnimationSource(packet.SourceType, packet.SourceId);
		switch (targetType)
		{
		case -1:
		{
			if (MapInstance.TryGet(mapId, out MapInstance instance2) && instance2.AddTileAnimation(animationId, packet.X, packet.Y, packet.Direction, null, animationSource) is Animation animation3)
			{
				animation3.SousLesCimes = true;
			}
			break;
		}
		case 1:
		{
			if (Globals.EntitiesToDispose.Contains(entityId) || !Globals.Entities.TryGetValue(entityId, out Entity value2) || !DatabaseObject<AnimationDescriptor>.TryGet(animationId, out var databaseObject2))
			{
				break;
			}
			if (animationSource == default(AnimationSource) || value2.RemoveAnimationIfExists(animationSource, dispose: true))
			{
				Animation animation2 = new Animation(databaseObject2, loopForever: false, packet.Direction != Direction.None, -1, value2, animationSource)
				{
					SousLesCimes = true
				};
				if (packet.Direction > Direction.None)
				{
					animation2.SetDir(packet.Direction);
				}
				value2.TryAddAnimation(animation2, animationSource);
			}
			else
			{
				ApplicationContext.CurrentContext.Logger.LogDebug("Unable to add new instance of animation {AnimationId} to entity {EntityId} ({EntityName})  because one already exists for the animation source {AnimationSource} and it could not be removed", animationId, value2.Id, value2.Name, animationSource);
			}
			break;
		}
		case 2:
		{
			if (!MapInstance.TryGet(mapId, out MapInstance instance) || !instance.LocalEntities.TryGetValue(entityId, out Entity value) || !DatabaseObject<AnimationDescriptor>.TryGet(animationId, out var databaseObject))
			{
				break;
			}
			if (databaseObject.Name == "Coup de scène")
			{
				if (value != null)
				{
					CombatFeedback.CoupDeScene(value);
				}
			}
			else if (animationSource == default(AnimationSource) || value.RemoveAnimationIfExists(animationSource, dispose: true))
			{
				Animation animation = new Animation(databaseObject, loopForever: false, packet.Direction == Direction.None, -1, value, animationSource)
				{
					SousLesCimes = true
				};
				if (packet.Direction > Direction.None)
				{
					animation.SetDir(packet.Direction);
				}
				value.TryAddAnimation(animation, animationSource);
			}
			else
			{
				ApplicationContext.CurrentContext.Logger.LogDebug("Unable to add new instance of animation {AnimationId} to entity {EntityId} ({EntityName}) because one already exists for the animation source {AnimationSource} and it could not be removed", animationId, value.Id, value.Name, animationSource);
			}
			break;
		}
		case 0:
			break;
		}
	}

	public void HandlePacket(IPacketSender packetSender, HoldPlayerPacket packet)
	{
		Guid eventId = packet.EventId;
		Guid mapId = packet.MapId;
		if (!packet.Releasing)
		{
			if (!Globals.EventHolds.ContainsKey(eventId))
			{
				Globals.EventHolds.Add(eventId, mapId);
			}
		}
		else if (Globals.EventHolds.ContainsKey(eventId))
		{
			Globals.EventHolds.Remove(eventId);
		}
	}

	public void HandlePacket(IPacketSender packetSender, PlayMusicPacket packet)
	{
		Audio.PlayMusic(packet.BGM, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
	}

	public void HandlePacket(IPacketSender packetSender, StopMusicPacket packet)
	{
		Audio.StopMusic(ClientConfiguration.Instance.MusicFadeTimer);
	}

	public void HandlePacket(IPacketSender packetSender, PlaySoundPacket packet)
	{
		Audio.AddGameSound(packet.Sound, loop: false);
	}

	public void HandlePacket(IPacketSender packetSender, StopSoundsPacket packet)
	{
		Audio.StopAllSounds();
	}

	public void HandlePacket(IPacketSender packetSender, ShowPicturePacket packet)
	{
		PacketSender.SendClosePicture(Globals.Picture?.EventId ?? Guid.Empty);
		packet.ReceiveTime = Timing.Global.Milliseconds;
		Globals.Picture = packet;
	}

	public void HandlePacket(IPacketSender packetSender, HidePicturePacket packet)
	{
		PacketSender.SendClosePicture(Globals.Picture?.EventId ?? Guid.Empty);
		Globals.Picture = null;
	}

	public void HandlePacket(IPacketSender packetSender, ShopPacket packet)
	{
		if (!Intersect.Client.Interface.Interface.HasInGameUI)
		{
			throw new ArgumentNullException("GameUi");
		}
		if (packet == null)
		{
			throw new ArgumentNullException("packet");
		}
		if (packet.ShopData != null)
		{
			Globals.GameShop = new ShopDescriptor();
			Globals.GameShop.Load(packet.ShopData);
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyOpenShop();
			});
		}
		else
		{
			Globals.GameShop = null;
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyCloseShop();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, CraftingTablePacket packet)
	{
		if (!packet.Close)
		{
			Globals.ActiveCraftingTable = new CraftingTableDescriptor();
			Globals.ActiveCraftingTable.Load(packet.TableData);
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyOpenCraftingTable(packet.JournalMode);
			});
		}
		else
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyCloseCraftingTable();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, BankPacket packet)
	{
		if (!packet.Close)
		{
			Globals.IsGuildBank = packet.Guild;
			Globals.BankSlots = new Item[packet.Slots];
			BankUpdatePacket[] items = packet.Items;
			foreach (BankUpdatePacket packet2 in items)
			{
				HandlePacket(packet2);
			}
			Globals.BankSlotCount = packet.Slots;
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyOpenBank();
			});
		}
		else
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyCloseBank();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, BankUpdatePacket packet)
	{
		int slot = packet.Slot;
		if (packet.ItemId != Guid.Empty)
		{
			Globals.BankSlots[slot] = new Item();
			Globals.BankSlots[slot].Load(packet.ItemId, packet.Quantity, packet.BagId, packet.Properties);
		}
		else
		{
			Globals.BankSlots[slot] = null;
		}
	}

	public void HandlePacket(IPacketSender packetSender, GameObjectPacket packet)
	{
		GameObjectType type = packet.Type;
		Guid id = packet.Id;
		bool anotherFollowing = packet.AnotherFollowing;
		bool deleted = packet.Deleted;
		string json = string.Empty;
		if (!deleted)
		{
			json = packet.Data;
		}
		switch (type)
		{
		case GameObjectType.Tileset:
		{
			TilesetDescriptor tilesetDescriptor = new TilesetDescriptor(id);
			tilesetDescriptor.Load(json);
			DatabaseObject<TilesetDescriptor>.Lookup.Set(id, tilesetDescriptor);
			if (Globals.HasGameData && !anotherFollowing)
			{
				Globals.ContentManager.LoadTilesets(DatabaseObject<TilesetDescriptor>.GetNameList());
			}
			return;
		}
		case GameObjectType.Map:
		case GameObjectType.Event:
			return;
		}
		DatabaseObjectLookup lookup = type.GetLookup();
		lookup.DeleteAt(id);
		if (!deleted)
		{
			Type objectType = type.GetObjectType();
			IDatabaseObject databaseObject = lookup.AddNew(objectType, id);
			databaseObject.Load(json);
			Traduction.Fiche(type, databaseObject);
		}
		_ = 6;
	}

	public void HandlePacket(IPacketSender packetSender, EntityDashPacket packet)
	{
		if (!Globals.Entities.TryGetValue(packet.EntityId, out Entity value) && (!MapInstance.TryGet(packet.EndMapId, out MapInstance instance) || !instance.LocalEntities.TryGetValue(packet.EntityId, out value) || value == null))
		{
			return;
		}
		if (value is Event && packet.Direction == Direction.None)
		{
			CombatFeedback.HitStop(70L);
			CombatFeedback.StartShake(3f, 280L);
		}
		if (!value.IsDying || !(value.KiKillHeadingX * value.KiKillHeadingX + value.KiKillHeadingY * value.KiKillHeadingY > 0.1f))
		{
			value.PendingDashMove = null;
			value.DashQueue.Enqueue(new Dash(packet.EndMapId, packet.EndX, packet.EndY, packet.DashEndMilliseconds, packet.DashLengthMilliseconds, packet.Direction, packet.EndOffsetX, packet.EndOffsetY, packet.Nature, packet.AuteurId)
			{
				ContreLeDecor = packet.ContreLeDecor,
				PartHeritage = packet.Part,
				ArretHeritage = (MatiereDArret)packet.Arret,
				DegatsDArretHeritage = packet.DegatsDArret,
				FormeHeritage = (FormeDeLancer)packet.Forme
			});
			if (Heritage.Actif)
			{
				Heritage.Rang.SurGlissade(value, packet.Nature);
				Heritage.SurGlissadeDeRuee(value, packet.Nature, packet.AuteurId, packet.DashLengthMilliseconds);
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, MapGridPacket packet)
	{
		Globals.MapGridWidth = packet.Grid.GetLength(0);
		Globals.MapGridHeight = packet.Grid.GetLength(1);
		bool clearKnownMaps = packet.ClearKnownMaps;
		Globals.MapGrid = new Guid[(int)checked((nint)Globals.MapGridWidth), (int)checked((nint)Globals.MapGridHeight)];
		if (clearKnownMaps)
		{
			IDatabaseObject[] array = MapInstance.Lookup.Values.ToArray();
			foreach (IDatabaseObject databaseObject in array)
			{
				((MapInstance)databaseObject).Dispose();
			}
		}
		Globals.NeedsMaps = true;
		Globals.GridMaps.Clear();
		for (int j = 0; j < Globals.MapGridWidth; j++)
		{
			for (int k = 0; k < Globals.MapGridHeight; k++)
			{
				Globals.MapGrid[j, k] = packet.Grid[j, k];
				if (Globals.MapGrid[j, k] != Guid.Empty)
				{
					Globals.GridMaps[Globals.MapGrid[j, k]] = new Point(j, k);
				}
			}
		}
		if (Globals.Me != null)
		{
			Player.FetchNewMaps();
		}
		Graphics.GridSwitched = true;
		if (Heritage.Actif)
		{
			Heritage.Effets.Oublier();
		}
	}

	public void HandlePacket(IPacketSender packetSender, TimePacket packet)
	{
		Time.LoadTime(packet.Time, Color.FromArgb(packet.Color.A, packet.Color.R, packet.Color.G, packet.Color.B), packet.Rate);
	}

	public void HandlePacket(IPacketSender packetSender, PartyPacket packet)
	{
		if (Globals.Me != null && Globals.Me.Party != null)
		{
			Globals.Me.Party.Clear();
			for (int i = 0; i < packet.MemberData.Length; i++)
			{
				PartyMemberPacket partyMemberPacket = packet.MemberData[i];
				Globals.Me.Party.Add(new PartyMember(partyMemberPacket.Id, partyMemberPacket.Name, partyMemberPacket.Vital, partyMemberPacket.MaxVital, partyMemberPacket.Level, partyMemberPacket.Sprite));
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, PartyUpdatePacket packet)
	{
		int memberIndex = packet.MemberIndex;
		if (memberIndex < Globals.Me.Party.Count)
		{
			PartyMemberPacket memberData = packet.MemberData;
			Globals.Me.Party[memberIndex] = new PartyMember(memberData.Id, memberData.Name, memberData.Vital, memberData.MaxVital, memberData.Level, memberData.Sprite);
		}
	}

	public void HandlePacket(IPacketSender packetSender, PartyInvitePacket packet)
	{
		Intersect.Client.Interface.Interface.GameUi?.GameMenu?.Notify(MenuBadge.Party);
		RequestCardWindow.Open(RequestCardWindow.RequestKind.Party, packet.LeaderId, packet.LeaderName);
	}

	public void HandlePacket(IPacketSender packetSender, ChatBubblePacket packet)
	{
		Guid entityId = packet.EntityId;
		EntityType type = packet.Type;
		Guid mapId = packet.MapId;
		IEntity entity = null;
		if (type < EntityType.Event)
		{
			if (!Globals.Entities.ContainsKey(entityId))
			{
				return;
			}
			entity = Globals.Entities[entityId];
		}
		else
		{
			MapInstance mapInstance = MapInstance.Get(mapId);
			if (mapInstance == null || !mapInstance.LocalEntities.ContainsKey(entityId))
			{
				return;
			}
			entity = mapInstance.LocalEntities[entityId];
		}
		entity?.AddChatBubble((type == EntityType.Event) ? Traduction.Contenu(packet.Text) : packet.Text);
	}

	public void HandlePacket(IPacketSender packetSender, QuestOfferPacket packet)
	{
		if (!Globals.QuestOffers.Contains(packet.QuestId))
		{
			Globals.QuestOffers.Add(packet.QuestId);
		}
	}

	public void HandlePacket(IPacketSender packetSender, QuestProgressPacket packet)
	{
		if (Globals.Me == null)
		{
			return;
		}
		foreach (KeyValuePair<Guid, string> quest in packet.Quests)
		{
			if (quest.Value == null)
			{
				if (Globals.Me.QuestProgress.ContainsKey(quest.Key))
				{
					Globals.Me.QuestProgress.Remove(quest.Key);
				}
			}
			else if (Globals.Me.QuestProgress.ContainsKey(quest.Key))
			{
				Globals.Me.QuestProgress[quest.Key] = new QuestProgress(quest.Value);
			}
			else
			{
				Globals.Me.QuestProgress.Add(quest.Key, new QuestProgress(quest.Value));
			}
		}
		Globals.Me.HiddenQuests = packet.HiddenQuests;
		List<(Guid, Guid, int, int)> list = new List<(Guid, Guid, int, int)>();
		Guid[] targetQuests = packet.TargetQuests;
		int val = ((targetQuests != null) ? targetQuests.Length : 0);
		Guid[] targetMaps = packet.TargetMaps;
		int val2 = ((targetMaps != null) ? targetMaps.Length : 0);
		int[] targetXs = packet.TargetXs;
		int val3 = ((targetXs != null) ? targetXs.Length : 0);
		int[] targetYs = packet.TargetYs;
		int num = Math.Min(val, Math.Min(val2, Math.Min(val3, (targetYs != null) ? targetYs.Length : 0)));
		for (int i = 0; i < num; i++)
		{
			list.Add((packet.TargetQuests[i], packet.TargetMaps[i], packet.TargetXs[i], packet.TargetYs[i]));
		}
		Globals.QuestTargets = list;
		Guid previousStory = Globals.StoryQuestId;
		Guid story = packet.StoryQuestId;
		string giver = Traduction.Contenu(packet.StoryGiver ?? string.Empty);
		string place = Traduction.Contenu(packet.StoryPlace ?? string.Empty);
		Globals.StoryQuestId = story;
		Globals.StoryGiver = giver;
		Globals.StoryPlace = place;
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface uiInGame)
		{
			StoryThread.Observe(previousStory, story, giver, place);
			Annonces.ObserverQuetes();
			TrackedQuests.Observe();
			uiInGame.NotifyQuestsUpdated();
		});
	}

	public void HandlePacket(IPacketSender packetSender, TradePacket packet)
	{
		if (!string.IsNullOrEmpty(packet.TradePartner))
		{
			TradeState.Reset();
			Globals.Trade = new Item[2, Options.Instance.Player.MaxInventory];
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < Options.Instance.Player.MaxInventory; j++)
				{
					Globals.Trade[i, j] = new Item();
				}
			}
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyOpenTrading(packet.TradePartner);
			});
		}
		else
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyCloseTrading();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, TradeUpdatePacket packet)
	{
		int num = 0;
		if (packet.TraderId != Globals.Me.Id)
		{
			num = 1;
		}
		int slot = packet.Slot;
		if (packet.ItemId == Guid.Empty)
		{
			Globals.Trade[num, slot] = null;
			return;
		}
		Globals.Trade[num, slot] = new Item();
		Globals.Trade[num, slot].Load(packet.ItemId, packet.Quantity, packet.BagId, packet.Properties);
	}

	public void HandlePacket(IPacketSender packetSender, TradeRequestPacket packet)
	{
		RequestCardWindow.Open(RequestCardWindow.RequestKind.Trade, packet.PartnerId, packet.PartnerName);
	}

	public void HandlePacket(IPacketSender packetSender, TradeStatusPacket packet)
	{
		TradeState.Apply(packet.MyZeni, packet.TheirZeni, packet.MyReady, packet.TheirReady, packet.Revision);
	}

	public void HandlePacket(IPacketSender packetSender, NpcAggressionPacket packet)
	{
		if (Globals.Entities.ContainsKey(packet.EntityId))
		{
			Globals.Entities[packet.EntityId].Aggression = packet.Aggression;
		}
	}

	public void HandlePacket(IPacketSender packetSender, PlayerDeathPacket packet)
	{
		if (Globals.Entities.ContainsKey(packet.PlayerId))
		{
			Globals.Entities[packet.PlayerId].DashQueue.Clear();
			Globals.Entities[packet.PlayerId].Dashing = null;
			Globals.Entities[packet.PlayerId].DashTimer = 0L;
			Mort.Commencer(Globals.Entities[packet.PlayerId], packet.Etape);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EntityZDimensionPacket packet)
	{
		if (Globals.Entities.ContainsKey(packet.EntityId))
		{
			Globals.Entities[packet.EntityId].Z = packet.Level;
		}
	}

	public void HandlePacket(IPacketSender packetSender, BagPacket packet)
	{
		if (!packet.Close)
		{
			Globals.BagSlots = new Item[packet.Slots];
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyOpenBag();
			});
		}
		else
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyCloseBag();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, BagUpdatePacket packet)
	{
		if (packet.ItemId == Guid.Empty)
		{
			Globals.BagSlots[packet.Slot] = null;
			return;
		}
		Globals.BagSlots[packet.Slot] = new Item();
		Globals.BagSlots[packet.Slot].Load(packet.ItemId, packet.Quantity, packet.BagId, packet.Properties);
	}

	public void HandlePacket(IPacketSender packetSender, MoveRoutePacket packet)
	{
		Globals.MoveRouteActive = packet.Active;
	}

	public void HandlePacket(IPacketSender packetSender, FriendsPacket packet)
	{
		Globals.Me?.Friends.Clear();
		FriendEntryPacket[] array = packet.Friends ?? Array.Empty<FriendEntryPacket>();
		foreach (FriendEntryPacket friendEntryPacket in array)
		{
			Globals.Me?.Friends.Add(new FriendInstance
			{
				Name = friendEntryPacket.Name,
				Online = friendEntryPacket.Online,
				Map = (friendEntryPacket.Online ? friendEntryPacket.Map : null),
				Sprite = friendEntryPacket.Sprite,
				Face = friendEntryPacket.Face,
				Level = friendEntryPacket.Level,
				LastSeenMinutes = friendEntryPacket.LastSeenMinutes
			});
		}
		if (Intersect.Client.Interface.Interface.HasInGameUI)
		{
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyUpdateFriendsList();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, FriendRequestPacket packet)
	{
		Intersect.Client.Interface.Interface.GameUi?.GameMenu?.Notify(MenuBadge.Friends);
		RequestCardWindow.Open(RequestCardWindow.RequestKind.Friend, packet.FriendId, packet.FriendName);
	}

	public void HandlePacket(IPacketSender packetSender, CharactersPacket packet)
	{
		List<CharacterSelectionPreviewMetadata> list = new List<CharacterSelectionPreviewMetadata>();
		list.AddRange(packet.Characters.Select((CharacterPacket characterPacket) => new CharacterSelectionPreviewMetadata(characterPacket.Id, characterPacket.Name, characterPacket.Sprite, characterPacket.Face, characterPacket.Level, characterPacket.ClassName, characterPacket.Equipment)));
		List<CharacterSelectionPreviewMetadata> list2 = list;
		if (packet.FreeSlot)
		{
			list2.Add(default(CharacterSelectionPreviewMetadata));
		}
		Globals.WaitingOnServer = false;
		Intersect.Client.Interface.Interface.MenuUi.MainMenu.NotifyOpenCharacterSelection(list2, packet.Username);
	}

	public void HandlePacket(IPacketSender packetSender, PasswordChangeResultPacket packet)
	{
		PasswordResetResultType resultType = packet.ResultType;
		LocalizedString localizedString = Strings.PasswordChange.ResponseMessages[resultType];
		bool flag = resultType == PasswordResetResultType.Success;
		AlertType alertType = (flag ? AlertType.Information : AlertType.Error);
		LocalizedString localizedString2 = (flag ? Strings.PasswordChange.AlertTitleSuccess : Strings.PasswordChange.AlertTitleError);
		Intersect.Client.Interface.Interface.ShowAlert(localizedString, localizedString2, alertType);
		if (flag)
		{
			Intersect.Client.Interface.Interface.MenuUi.MainMenu.NotifyOpenLogin();
		}
		Globals.WaitingOnServer = false;
	}

	public void HandlePacket(IPacketSender packetSender, TargetOverridePacket packet)
	{
		if (Globals.Entities.ContainsKey(packet.TargetId))
		{
			Globals.Me.TryTarget(Globals.Entities[packet.TargetId], force: true);
		}
	}

	public void HandlePacket(IPacketSender packetSender, EnteringGamePacket packet)
	{
		Fade.Cancel();
	}

	public void HandlePacket(IPacketSender packetSender, CancelCastPacket packet)
	{
		if (Globals.Entities.TryGetValue(packet.EntityId, out Entity value))
		{
			AnimationSource animationSource = new AnimationSource(AnimationSourceType.SpellCast, value.SpellCast);
			if (value.TryRemoveAnimation(animationSource, dispose: true, out Animation animation))
			{
				ApplicationContext.CurrentContext.Logger.LogDebug("Removing cancelled spell cast animation {AnimationId} ({AnimationName})", animation.Descriptor?.Id, animation.Descriptor?.Name);
			}
			value.CastTime = 0L;
			value.SpellCast = default(Guid);
		}
	}

	public void HandlePacket(IPacketSender packetSender, GuildPacket packet)
	{
		if (Globals.Me == null)
		{
			return;
		}
		GuildMember[] array = (from m in packet.Members
			orderby m.Online descending, m.Rank, m.Name
			select m).ToArray();
		GuildMember[] guildMembers = Globals.Me.GuildMembers;
		bool flag = guildMembers?.Length != array.Length;
		if (!flag)
		{
			for (int num = 0; num < guildMembers.Length; num++)
			{
				GuildMember guildMember = guildMembers[num];
				GuildMember guildMember2 = array[num];
				if (guildMember.Id != guildMember2.Id)
				{
					flag = true;
					break;
				}
				if (guildMember.Online != guildMember2.Online)
				{
					flag = true;
					break;
				}
				if (guildMember.Rank != guildMember2.Rank)
				{
					flag = true;
					break;
				}
				if (!string.Equals(guildMember.Name, guildMember2.Name))
				{
					flag = true;
					break;
				}
			}
		}
		string text = packet.Motd ?? string.Empty;
		GuildActivityEntry[] array2 = packet.Activity ?? Array.Empty<GuildActivityEntry>();
		if (!string.Equals(Globals.Me.GuildMotd, text, StringComparison.Ordinal) || Globals.Me.GuildActivity.Length != array2.Length)
		{
			flag = true;
		}
		GuildApplication[] array3 = packet.Applications ?? Array.Empty<GuildApplication>();
		if (Globals.Me.GuildApplications.Length != array3.Length)
		{
			flag = true;
		}
		Globals.Me.GuildMotd = text;
		Globals.Me.GuildActivity = array2;
		Globals.Me.GuildApplications = array3;
		Globals.Me.GuildLevel = packet.Level;
		Globals.Me.GuildExperience = packet.Experience;
		Globals.Me.GuildExperienceToNext = packet.ExperienceToNext;
		Globals.Me.GuildWeekExperience = packet.WeekExperience;
		Globals.Me.GuildObjectives = packet.Objectives ?? Array.Empty<GuildObjective>();
		Globals.Me.GuildTag = packet.Tag ?? string.Empty;
		Globals.Me.GuildEmblem = packet.Emblem;
		Globals.Me.GuildColor = packet.Color;
		Globals.Me.GuildObjectivesEndAt = Timing.Global.Milliseconds + packet.ObjectivesEndInSeconds * 1000;
		if (flag)
		{
			Globals.Me.GuildMembers = array;
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyUpdateGuildList();
			});
		}
	}

	public void HandlePacket(IPacketSender packetSender, GuildDirectoryPacket packet)
	{
		if (Globals.Me == null)
		{
			return;
		}
		if (packet.Profile != null)
		{
			Globals.Me.GuildProfile = packet.Profile;
			Globals.Me.GuildAppliedTo = packet.AppliedTo ?? string.Empty;
			Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
			{
				gameInterface.NotifyUpdateGuildList();
			});
			return;
		}
		Globals.Me.GuildDirectory = packet.Entries ?? Array.Empty<GuildDirectoryEntry>();
		Globals.Me.GuildDirectoryQuery = packet.Query ?? string.Empty;
		Globals.Me.GuildDirectoryPage = packet.Page;
		Globals.Me.GuildDirectoryTotal = packet.Total;
		Globals.Me.GuildAppliedTo = packet.AppliedTo ?? string.Empty;
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
		{
			gameInterface.NotifyUpdateGuildList();
		});
	}

	public void HandlePacket(IPacketSender packetSender, GuildInvitePacket packet)
	{
		Intersect.Client.Interface.Interface.GameUi?.GameMenu?.Notify(MenuBadge.Guild);
		Intersect.Client.Interface.Interface.EnqueueInGame((Action)delegate
		{
			RequestCardWindow.Open(RequestCardWindow.RequestKind.Guild, Guid.Empty, packet.Inviter, packet.GuildName);
		});
	}

	public void HandlePacket(IPacketSender packetSender, FadePacket packet)
	{
		switch (packet.FadeType)
		{
		case FadeType.None:
			Fade.Cancel();
			break;
		case FadeType.FadeIn:
			Fade.FadeIn(packet.DurationMs, packet.WaitForCompletion);
			break;
		case FadeType.FadeOut:
			Fade.FadeOut(packet.DurationMs, packet.WaitForCompletion);
			break;
		}
	}

	public void HandlePacket(IPacketSender packetSender, CartesLibresPacket packet)
	{
		DeplacementLibre.RecevoirCartes(packet.Cartes, packet.CartesMonstres);
	}

	public void HandlePacket(IPacketSender packetSender, VoisinsPacket packet)
	{
		EntitesInconnues.Recenser(packet.Carte, packet.Ids);
	}

	public void HandlePacket(IPacketSender packetSender, CorrectionLibrePacket packet)
	{
		Globals.Me?.AppliquerCorrectionLibre(packet);
	}

	public void HandlePacket(IPacketSender packetSender, EntitesLibresPacket packet)
	{
		DeplacementLibre.NoterTempsServeur(packet.Temps);
		EntiteLibre[] array = packet.Entites ?? Array.Empty<EntiteLibre>();
		foreach (EntiteLibre entiteLibre in array)
		{
			if (entiteLibre != null)
			{
				if (!Globals.Entities.TryGetValue(entiteLibre.Id, out Entity value) || value == null)
				{
					EntitesInconnues.Noter(entiteLibre.Id, entiteLibre.MapId);
				}
				else if (value != Globals.Me)
				{
					value.RecevoirInstantaneLibre(packet.Temps, entiteLibre);
				}
			}
		}
	}

	public void HandlePacket(IPacketSender packetSender, DungeonStatusesPacket packet)
	{
		Dungeon.OnStatuses(packet);
	}

	public void HandlePacket(IPacketSender packetSender, DungeonRunPacket packet)
	{
		Dungeon.OnRun(packet);
	}

	public void HandlePacket(IPacketSender packetSender, DungeonTelegraphPacket packet)
	{
		Dungeon.OnTelegraph(packet);
	}

	public void HandlePacket(IPacketSender packetSender, DungeonCompletePacket packet)
	{
		Dungeon.OnComplete(packet);
	}

	public void HandlePacket(IPacketSender packetSender, DungeonResultPacket packet)
	{
		Dungeon.OnResult(packet);
	}

	public void HandlePacket(IPacketSender packetSender, HouseStatesPacket packet)
	{
		Houses.OnStates(packet);
	}

	public void HandlePacket(IPacketSender packetSender, HouseInteriorPacket packet)
	{
		Houses.OnInterior(packet);
	}

	public void HandlePacket(IPacketSender packetSender, HouseKnockPacket packet)
	{
		Houses.OnKnock(packet);
	}

	public void HandlePacket(IPacketSender packetSender, HouseResultPacket packet)
	{
		Houses.OnResult(packet);
	}

	public void HandlePacket(IPacketSender packetSender, JoueursEnLignePacket packet)
	{
		MiniMapPanel.JoueursEnLigne = Math.Max(0, packet.Nombre);
	}

	public void HandlePacket(IPacketSender packetSender, MarketOpenPacket packet)
	{
		Intersect.Client.Interface.Interface.EnqueueInGame(delegate(GameInterface gameInterface)
		{
			if (packet.Open)
			{
				gameInterface.OpenMarketWindow();
			}
			else
			{
				gameInterface.CloseMarketWindow(packet.TooFar);
			}
		});
	}

	public void HandlePacket(IPacketSender packetSender, RankingPacket packet)
	{
		RankingState.OnRanking(packet);
	}

	public void HandlePacket(IPacketSender packetSender, RankingProfilePacket packet)
	{
		RankingState.OnProfile(packet);
	}

	public void HandlePacket(IPacketSender packetSender, TeachingsPacket packet)
	{
		Globals.Me?.SetTeachings(packet);
	}

	public void HandlePacket(IPacketSender packetSender, TeachingTrialPacket packet)
	{
		MasterTrial.OnState(packet);
	}

	public void HandlePacket(IPacketSender packetSender, TeachingTrialResultPacket packet)
	{
		MasterTrial.OnResult(packet);
	}
}
