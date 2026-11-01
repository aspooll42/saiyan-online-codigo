using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Client.Networking;
using Intersect.Client.Security;
using Intersect.Collections;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Mapping.Tilesets;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Framework.Utilities;
using Intersect.Models;

namespace Intersect.Client.Core;

internal static class Main
{
	private static long _animationTimer;

	private static long _lastEntityFrameAt;

	private static float _slowMotionCarry;

	private const string VariableConnexionPerdue = "SAIYAN_CONNEXION_PERDUE";

	private static bool _loadedTilesets;

	private static long _prochaineVerifAntiOutils;

	private static Guid _carteDeLaDerniereImage;

	internal static void Start(IClientContext context)
	{
		Graphics.InitGraphics();
		CarteGraphique.Noter();
		Audio.Init();
		Audio.PlayMusic(ClientConfiguration.Instance.MenuMusic, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
		Intersect.Client.Networking.Network.InitNetwork(context);
		if (Environment.GetEnvironmentVariable("SAIYAN_CONNEXION_PERDUE") == "1")
		{
			Environment.SetEnvironmentVariable("SAIYAN_CONNEXION_PERDUE", null);
			Intersect.Client.Interface.Interface.ShowAlert(Strings.Errors.LostConnection.ToString(), null, AlertType.Information);
		}
		Guid id = Guid.NewGuid();
		foreach (object value in Enum.GetValues(typeof(GameObjectType)))
		{
			GameObjectType gameObjectType = (GameObjectType)value;
			if (gameObjectType != GameObjectType.Event && gameObjectType != GameObjectType.Time)
			{
				DatabaseObjectLookup lookup = gameObjectType.GetLookup();
				IDatabaseObject databaseObject = lookup.AddNew(gameObjectType.GetObjectType(), id);
				databaseObject.Load(databaseObject.JsonData);
				lookup.Delete(databaseObject);
			}
		}
	}

	public static void DestroyGame()
	{
		Intersect.Client.Interface.Interface.DestroyGwen(exiting: true);
		Graphics.Renderer?.Close();
	}

	public static void Update(TimeSpan deltaTime)
	{
		lock (Globals.GameLock)
		{
			if (Environment.TickCount64 >= _prochaineVerifAntiOutils)
			{
				_prochaineVerifAntiOutils = Environment.TickCount64 + 3000;
				AntiOutils.Verifier();
			}
			using (ChronoImage.Etape("reseau (paquets recus)"))
			{
				Intersect.Client.Networking.Network.Update();
			}
			Fade.Update();
			(Graphics.Renderer as MonoRenderer)?.ServirLesLecturesDAvance();
			Intersect.Client.Interface.Interface.SetHandleInput(Globals.GameState != GameStates.Intro);
			switch (Globals.GameState)
			{
			case GameStates.Intro:
				ProcessIntro();
				break;
			case GameStates.Menu:
				ProcessMenu();
				break;
			case GameStates.Loading:
				ProcessLoading();
				break;
			case GameStates.InGame:
			{
				using (ChronoImage.Etape("jeu (mise a jour)"))
				{
					ProcessGame();
				}
				Player me = Globals.Me;
				if (me != null && me.MapId != _carteDeLaDerniereImage)
				{
					if (_carteDeLaDerniereImage != Guid.Empty)
					{
						ChronoImage.Noter("changement de carte vers " + (MapInstance.Get(me.MapId)?.Name ?? "?"));
					}
					_carteDeLaDerniereImage = me.MapId;
				}
				break;
			}
			default:
				throw new ArgumentOutOfRangeException("GameState", $"Value {Globals.GameState} out of range.");
			case GameStates.Error:
				break;
			}
			Globals.InputManager.Update(deltaTime);
			if (Heritage.Actif)
			{
				Heritage.Appuis.Relever();
			}
			using (ChronoImage.Etape("audio"))
			{
				Audio.Update();
			}
			Meteo.HorsDuJeu();
			PresenceDiscord.Suivre();
			Globals.OnGameUpdate(deltaTime);
		}
	}

	private static void ProcessIntro()
	{
		if (ClientConfiguration.Instance.IntroImages.Count > 0)
		{
			IGameTexture texture = Globals.ContentManager.GetTexture(TextureType.Image, ClientConfiguration.Instance.IntroImages[Globals.IntroIndex]);
			if (texture != null)
			{
				if (Globals.IntroStartTime == -1)
				{
					if (Fade.DoneFading())
					{
						if (Globals.IntroComing)
						{
							Globals.IntroStartTime = Timing.Global.MillisecondsUtc;
						}
						else
						{
							Globals.IntroIndex++;
							Fade.FadeIn(ClientConfiguration.Instance.FadeDurationMs);
							Globals.IntroComing = true;
						}
					}
				}
				else if (Timing.Global.MillisecondsUtc > Globals.IntroStartTime + Globals.IntroDelay)
				{
					Fade.FadeOut(ClientConfiguration.Instance.FadeDurationMs);
					Globals.IntroStartTime = -1L;
					Globals.IntroComing = false;
				}
			}
			else
			{
				Globals.IntroIndex++;
			}
			if (Globals.IntroIndex >= ClientConfiguration.Instance.IntroImages.Count)
			{
				Globals.GameState = GameStates.Menu;
			}
		}
		else
		{
			Globals.GameState = GameStates.Menu;
		}
	}

	private static void ProcessMenu()
	{
		if (Globals.JoiningGame)
		{
			Globals.GameState = GameStates.Loading;
			Intersect.Client.Interface.Interface.DestroyGwen();
		}
	}

	private static void ProcessLoading()
	{
		if (Globals.Me != null && Globals.Me.MapInstance != null)
		{
			if (!_loadedTilesets && Globals.HasGameData)
			{
				Globals.ContentManager.LoadTilesets(DatabaseObject<TilesetDescriptor>.GetNameList());
				_loadedTilesets = true;
			}
			if (MenuLoadingScreen.ReadyToEnter(Globals.Me.MapInstance.Name))
			{
				Audio.PlayMusic(MapInstance.Get(Globals.Me.MapId).Music, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
				Globals.GameState = GameStates.InGame;
				Fade.FadeIn(ClientConfiguration.Instance.FadeDurationMs);
			}
		}
	}

	private static void ProcessGame()
	{
		if (Globals.ConnectionLost)
		{
			if (!ClientContext.IsSinglePlayer)
			{
				Environment.SetEnvironmentVariable("SAIYAN_CONNEXION_PERDUE", "1");
				if (ProcessHelper.TryRelaunch())
				{
					return;
				}
				Environment.SetEnvironmentVariable("SAIYAN_CONNEXION_PERDUE", null);
			}
			Logout(characterSelect: false);
			Globals.ConnectionLost = false;
			return;
		}
		MenuLastPlaces.Track();
		if (Globals.NeedsMaps)
		{
			Globals.NeedsMaps = false;
			PacketSender.SendPing();
		}
		else
		{
			PacketSender.SendNeedMapForGrid();
		}
		Guid key;
		if (!Globals.NeedsMaps)
		{
			CoupsDifferes.Maj();
			bool isFrozen = CombatFeedback.IsFrozen;
			long milliseconds = Timing.Global.Milliseconds;
			long num = ((_lastEntityFrameAt == 0L) ? 0 : Math.Max(0L, milliseconds - _lastEntityFrameAt));
			_lastEntityFrameAt = milliseconds;
			float slowMotionFactor = CombatFeedback.SlowMotionFactor;
			long num2 = 0L;
			if (!isFrozen && slowMotionFactor < 1f)
			{
				_slowMotionCarry += (float)num * (1f - slowMotionFactor);
				num2 = (long)_slowMotionCarry;
				_slowMotionCarry -= num2;
			}
			ChronoImage.DebutDesTours();
			foreach (KeyValuePair<Guid, Entity> entity in Globals.Entities)
			{
				if (entity.Value == null)
				{
					continue;
				}
				if (isFrozen)
				{
					entity.Value.HoldForHitStop(num);
					continue;
				}
				if (num2 > 0)
				{
					entity.Value.HoldForHitStop(num2);
				}
				double maintenant = ChronoImage.Maintenant;
				entity.Value.Update();
				ChronoImage.EntiteLente(entity.Value, maintenant, "mise a jour");
			}
			ChronoImage.Tour("entites (mise a jour)");
			List<Guid> list = new List<Guid>();
			for (int i = 0; i < Globals.EntitiesToDispose.Count; i++)
			{
				if (!Globals.Entities.TryGetValue(Globals.EntitiesToDispose[i], out Entity value))
				{
					continue;
				}
				key = Globals.EntitiesToDispose[i];
				Guid? obj = Globals.Me?.Id;
				if (!(key == obj))
				{
					if (value.IsDying && Timing.Global.Milliseconds - value.DyingAt < CombatFeedback.DeathDuration(value))
					{
						list.Add(Globals.EntitiesToDispose[i]);
						continue;
					}
					value.Dispose();
					Globals.Entities.Remove(Globals.EntitiesToDispose[i]);
				}
			}
			Globals.EntitiesToDispose.Clear();
			Globals.EntitiesToDispose.AddRange(list);
			EntitesInconnues.Tour();
			ChronoImage.Tour("entites (suppression)");
			IDatabaseObject[] source = MapInstance.Lookup.Values.ToArray();
			foreach (MapInstance item2 in source.Cast<MapInstance>())
			{
				item2?.Update(item2.InView());
			}
			ChronoImage.Tour("cartes (mise a jour)");
		}
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		if (_animationTimer < millisecondsUtc)
		{
			Globals.AnimationFrame = (Globals.AnimationFrame + 1) % 3;
			_animationTimer = millisecondsUtc + 500;
		}
		List<Guid> list2 = new List<Guid>();
		foreach (KeyValuePair<Guid, Guid> eventHold in Globals.EventHolds)
		{
			eventHold.Deconstruct(out key, out var value2);
			Guid item = key;
			Guid guid = value2;
			value2 = default(Guid);
			if (guid == value2)
			{
				continue;
			}
			if (MapInstance.TryGet(guid, out MapInstance _))
			{
				Player? me = Globals.Me;
				if (me != null && me.MapId == guid)
				{
					continue;
				}
			}
			list2.Add(item);
		}
		foreach (Guid item3 in list2)
		{
			Globals.EventHolds.Remove(item3);
		}
		Graphics.UpdatePlayerLight();
		Time.Update();
	}

	public static void JoinGame()
	{
		Globals.LoggedIn = true;
		RecherchePanel.Oublier();
		Audio.StopMusic(ClientConfiguration.Instance.MusicFadeTimer);
	}

	public static void Logout(bool characterSelect, bool skipFade = false)
	{
		Audio.PlayMusic(ClientConfiguration.Instance.MenuMusic, ClientConfiguration.Instance.MusicFadeTimer, ClientConfiguration.Instance.MusicFadeTimer, loop: true);
		if (skipFade)
		{
			Fade.Cancel();
		}
		else
		{
			Fade.FadeOut(ClientConfiguration.Instance.FadeDurationMs);
		}
		if (!ClientContext.IsSinglePlayer)
		{
			Globals.SoftLogout = !characterSelect;
			PacketSender.SendLogout(characterSelect);
		}
		Globals.LoggedIn = false;
		Globals.WaitingOnServer = false;
		Globals.GameState = GameStates.Menu;
		Globals.JoiningGame = false;
		Globals.NeedsMaps = true;
		Globals.Picture = null;
		Globals.InBag = false;
		Globals.InBank = false;
		Globals.GameShop = null;
		Globals.InTrade = false;
		Globals.EventDialogs?.Clear();
		Globals.InCraft = false;
		Houses.Reset();
		Dungeon.Reset();
		Intersect.Client.Interface.Interface.HideUi = false;
		Globals.Me = null;
		Globals.HasGameData = false;
		ChargementDesCartes.Vider();
		Casse.Oublier();
		if (Heritage.Actif)
		{
			Heritage.Effets.Oublier();
		}
		foreach (KeyValuePair<Guid, IDatabaseObject> item in MapInstance.Lookup)
		{
			MapInstance mapInstance = (MapInstance)item.Value;
			mapInstance.Dispose(prep: false);
		}
		KeyValuePair<Guid, Entity>[] array = Globals.Entities.ToArray();
		foreach (KeyValuePair<Guid, Entity> keyValuePair in array)
		{
			keyValuePair.Value.Dispose();
		}
		DatabaseObject<MapDescriptor>.Lookup.Clear();
		MapInstance.Lookup.Clear();
		MapInstance.MapRequests.Clear();
		Globals.Entities.Clear();
		Globals.MapGrid = null;
		Globals.GridMaps.Clear();
		Globals.EventDialogs?.Clear();
		Globals.EventHolds.Clear();
		Globals.PendingEvents.Clear();
		Intersect.Client.Interface.Interface.InitGwen();
		if (ClientContext.IsSinglePlayer)
		{
			PacketSender.SendLogout(characterSelect);
		}
		if (skipFade)
		{
			Fade.Cancel();
		}
		else
		{
			Fade.FadeIn(ClientConfiguration.Instance.FadeDurationMs);
		}
	}
}
