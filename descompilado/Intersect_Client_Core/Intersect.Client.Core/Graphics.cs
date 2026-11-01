using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Entities.Events;
using Intersect.Client.Entities.Projectiles;
using Intersect.Client.Framework;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Database;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.General;
using Intersect.Client.Housing;
using Intersect.Client.Interface;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Menu;
using Intersect.Client.Maps;
using Intersect.Client.MonoGame.Graphics;
using Intersect.Config;
using Intersect.Configuration;
using Intersect.Enums;
using Intersect.Framework;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Lighting;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Utilities;

namespace Intersect.Client.Core;

public static class Graphics
{
	public static object AnimationLock = new object();

	private static FloatRect _currentView;

	public static GameShader? DefaultShader;

	private static IGameTexture? sMenuBackground;

	public static int DrawCalls;

	public static int EntitiesDrawn;

	public static object GfxLock = new object();

	public static bool GridSwitched;

	public static int LightsDrawn;

	public static List<Animation> LiveAnimations = new List<Animation>();

	public static int MapsDrawn;

	private static int sMenuBackgroundIndex;

	private static long sMenuBackgroundInterval;

	public static Color OverlayColor = Color.Transparent;

	public static ColorF PlayerLightColor = ColorF.White;

	public static HashSet<Entity>[,]? RenderingEntities;

	private static readonly List<Entity> sRangeeTriee = new List<Entity>(16);

	private static readonly Comparison<Entity> sParPieds = delegate(Entity a, Entity b)
	{
		int num = a.Origin.Y.CompareTo(b.Origin.Y);
		if (Heritage.Actif)
		{
			num = Heritage.PiedsPourLeTri(a).CompareTo(Heritage.PiedsPourLeTri(b));
		}
		if (num != 0)
		{
			return num;
		}
		int num2 = (a is Player).CompareTo(b is Player);
		return (num2 == 0) ? a.Id.CompareTo(b.Id) : num2;
	};

	private static GameContentManager sContentManager = null;

	private static IGameRenderTexture? sDarknessTexture;

	private static readonly List<LightDescriptor> sLightQueue = new List<LightDescriptor>();

	private static long sLightUpdate;

	private static int sOldHeight;

	private static int sOldWidth;

	private static long sOverlayUpdate;

	private static float sPlayerLightExpand;

	private static float sPlayerLightIntensity = 255f;

	private static float sPlayerLightSize;

	private static IGameTexture? sKiLightTexture;

	private static bool sKiLightBroken;

	public const int LargeurDeLInterface = 1600;

	public const int LargeurDuGrandEcran = 1920;

	public const int HauteurDuGrandEcran = 1080;

	public const float AgrandissementMinimal = 1.25f;

	private const float EchelleDuMondeLisse = 2f;

	private static bool _mondeLisseOuvert;

	private static FloatRect _vueDeLEcran;

	private static float _echelleDeLEcran;

	private const float ZoomDesReperes = 1.875f;

	public static IFont? ActionMsgFont { get; set; }

	public static int ActionMsgFontSize { get; set; }

	public static float BrightnessLevel { get; set; }

	public static IFont? ChatBubbleFont { get; set; }

	public static int ChatBubbleFontSize { get; set; }

	public static FloatRect CurrentView
	{
		get
		{
			return _currentView;
		}
		set
		{
			_currentView = value;
			Renderer?.SetView(_currentView);
		}
	}

	public static float WorldScale { get; private set; } = 1f;

	public static FloatRect WorldViewport => new FloatRect(CurrentView.Position, CurrentView.Size / WorldScale);

	public static IFont? EntityNameFont { get; set; }

	public static int EntityNameFontSize { get; set; }

	public static IFont? GameFont { get; set; }

	public static int GameFontSize { get; set; }

	public static GameRenderer Renderer { get; set; }

	public static IFont? UIFont { get; set; }

	public static int UIFontSize { get; set; }

	public static float MinimumWorldScale => MathF.Max(Options.Instance?.Map?.MinimumWorldScale ?? 1f, 1.5f);

	public static float MaximumWorldScale => Options.Instance?.Map?.MaximumWorldScale ?? 1f;

	public static float EchelleDeLInterface
	{
		get
		{
			if (Globals.GameState != GameStates.InGame || !(Renderer is MonoRenderer monoRenderer))
			{
				return 1f;
			}
			return EchellePour(monoRenderer.LargeurReelle, monoRenderer.HauteurReelle);
		}
	}

	public static float EchelleDuCurseur
	{
		get
		{
			if (!(Renderer is MonoRenderer monoRenderer))
			{
				return 1f;
			}
			return MathF.Max(1f, EchellePour(monoRenderer.LargeurReelle, monoRenderer.HauteurReelle));
		}
	}

	public static (int Largeur, int Hauteur) TailleReelleDeLEcran
	{
		get
		{
			if (!(Renderer is MonoRenderer monoRenderer))
			{
				return (Largeur: Renderer?.ScreenWidth ?? 0, Hauteur: Renderer?.ScreenHeight ?? 0);
			}
			return (Largeur: monoRenderer.LargeurReelle, Hauteur: monoRenderer.HauteurReelle);
		}
	}

	public static float EchelleDuMonde { get; private set; } = 1f;

	public static float ZoomDeLaVue { get; private set; } = 1.5f;

	public static float ReductionDesReperes => Math.Clamp(ZoomDeLaVue / 1.875f, 0.6f, 1f);

	private static List<Entity> RangeeParPieds(int priorite, int rangee)
	{
		sRangeeTriee.Clear();
		int num = priorite % 3;
		if (num == 2)
		{
			return sRangeeTriee;
		}
		sRangeeTriee.AddRange(RenderingEntities[priorite, rangee]);
		if (num == 1)
		{
			sRangeeTriee.AddRange(RenderingEntities[priorite + 1, rangee]);
		}
		if (sRangeeTriee.Count > 1)
		{
			sRangeeTriee.Sort(sParPieds);
		}
		return sRangeeTriee;
	}

	public static void InitGraphics()
	{
		Chrono.Depart = ChronoImage.DebutDesTours;
		Chrono.Etape = ChronoImage.Tour;
		Chrono.Note = ChronoImage.Noter;
		Renderer.Init();
		sContentManager = Globals.ContentManager;
		sContentManager.LoadAll();
		string mouseCursor = ClientConfiguration.Instance.MouseCursor;
		Renderer.CursorName = mouseCursor;
		(GameFont, GameFontSize) = FindFont(ClientConfiguration.Instance.GameFont);
		(UIFont, UIFontSize) = FindFont(ClientConfiguration.Instance.UIFont);
		(EntityNameFont, EntityNameFontSize) = FindFont(ClientConfiguration.Instance.EntityNameFont);
		(ChatBubbleFont, ChatBubbleFontSize) = FindFont(ClientConfiguration.Instance.ChatBubbleFont);
		(ActionMsgFont, ActionMsgFontSize) = FindFont(ClientConfiguration.Instance.ActionMsgFont);
	}

	private static (IFont?, int) FindFont(string font)
	{
		int result = 8;
		if (font.IndexOf(',') > 0)
		{
			string[] array = font.Split(',');
			font = array[0];
			int.TryParse(array[1], out result);
		}
		return (sContentManager.GetFont(font), result);
	}

	public static void InitInGame()
	{
		RenderingEntities = new HashSet<Entity>[6, Options.Instance.Map.MapHeight * 5];
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < Options.Instance.Map.MapHeight * 5; j++)
			{
				RenderingEntities[i, j] = new HashSet<Entity>();
			}
		}
	}

	public static void DrawIntro()
	{
		List<string> introImages = ClientConfiguration.Instance.IntroImages;
		if (introImages.Count > Globals.IntroIndex && sContentManager.TryGetTexture(TextureType.Image, introImages[Globals.IntroIndex], out IGameTexture texture))
		{
			DrawFullScreenTextureFitMinimum(texture);
		}
	}

	private static void DrawMenu()
	{
		MainMenu mainMenu = Intersect.Client.Interface.Interface.MenuUiIfReady?.MainMenu;
		if (mainMenu != null)
		{
			mainMenu.DrawScene();
		}
		else
		{
			if (ClientConfiguration.Instance.MenuBackground.Count == 0)
			{
				return;
			}
			if (ClientConfiguration.Instance.MenuBackground.Count > 1)
			{
				sMenuBackground = sContentManager.GetTexture(TextureType.Gui, ClientConfiguration.Instance.MenuBackground[sMenuBackgroundIndex]);
				if (sMenuBackground == null)
				{
					return;
				}
				long millisecondsUtc = Timing.Global.MillisecondsUtc;
				if (sMenuBackgroundInterval < millisecondsUtc)
				{
					sMenuBackgroundIndex = (sMenuBackgroundIndex + 1) % ClientConfiguration.Instance.MenuBackground.Count;
					sMenuBackgroundInterval = millisecondsUtc + ClientConfiguration.Instance.MenuBackgroundFrameInterval;
				}
			}
			else
			{
				sMenuBackground = sContentManager.GetTexture(TextureType.Gui, ClientConfiguration.Instance.MenuBackground[0]);
				if (sMenuBackground == null)
				{
					return;
				}
			}
			switch (ClientConfiguration.Instance.MenuBackgroundDisplayMode)
			{
			case DisplayMode.Default:
				DrawFullScreenTexture(sMenuBackground);
				break;
			case DisplayMode.Center:
				DrawFullScreenTextureCentered(sMenuBackground);
				break;
			case DisplayMode.Stretch:
				DrawFullScreenTextureStretched(sMenuBackground);
				break;
			case DisplayMode.FitHeight:
				DrawFullScreenTextureFitHeight(sMenuBackground);
				break;
			case DisplayMode.FitWidth:
				DrawFullScreenTextureFitWidth(sMenuBackground);
				break;
			case DisplayMode.Fit:
				DrawFullScreenTextureFitMaximum(sMenuBackground);
				break;
			case DisplayMode.Cover:
				DrawFullScreenTextureFitMinimum(sMenuBackground);
				break;
			}
		}
	}

	public static void DrawInGame(TimeSpan deltaTime)
	{
		if (!(Globals.Me?.MapInstance is MapInstance mapInstance) || Globals.NeedsMaps || Globals.MapGrid == null || RenderingEntities == null)
		{
			return;
		}
		ChronoImage.DebutDesTours();
		Vent.NouvelleImage(deltaTime);
		Meteo.NouvelleImage(deltaTime);
		Figurants.NouvelleImage(deltaTime);
		ChronoImage.Tour("monde : vent et meteo");
		if (GridSwitched)
		{
			byte b = (byte)((float)mapInstance.Brightness / 100f * 255f);
			BrightnessLevel = (int)b;
			PlayerLightColor.R = (int)mapInstance.PlayerLightColor.R;
			PlayerLightColor.G = (int)mapInstance.PlayerLightColor.G;
			PlayerLightColor.B = (int)mapInstance.PlayerLightColor.B;
			sPlayerLightSize = mapInstance.PlayerLightSize;
			sPlayerLightIntensity = (int)mapInstance.PlayerLightIntensity;
			sPlayerLightExpand = mapInstance.PlayerLightExpand;
			OverlayColor.A = (byte)mapInstance.AHue;
			OverlayColor.R = (byte)mapInstance.RHue;
			OverlayColor.G = (byte)mapInstance.GHue;
			OverlayColor.B = (byte)mapInstance.BHue;
			mapInstance.GridSwitched();
			GridSwitched = false;
		}
		Animation[] array = LiveAnimations.ToArray();
		Animation[] array2 = array;
		foreach (Animation animation in array2)
		{
			if (animation.ParentGone())
			{
				animation.Dispose();
			}
		}
		CommencerVueLissee();
		ClearDarknessTexture();
		SolsPeints.Servir();
		ChronoImage.Tour("monde : preparation");
		int gridX = mapInstance.GridX;
		int gridY = mapInstance.GridY;
		long mapGridWidth = Globals.MapGridWidth;
		long mapGridHeight = Globals.MapGridHeight;
		for (int j = Math.Max(gridX - 1, 0); j <= Math.Min(gridX + 1, mapGridWidth - 1); j++)
		{
			for (int k = Math.Max(gridY - 1, 0); k <= Math.Min(gridY + 1, mapGridHeight - 1); k++)
			{
				Guid guid = Globals.MapGrid[j, k];
				if (!(guid == Guid.Empty))
				{
					SolsPeints.Preparer(guid);
					DrawMapPanorama(Globals.MapGrid[j, k]);
				}
			}
		}
		Meteo.DessinerAuDelaFond();
		for (int l = Math.Max(gridX - 1, 0); l <= Math.Min(gridX + 1, mapGridWidth - 1); l++)
		{
			for (int m = Math.Max(gridY - 1, 0); m <= Math.Min(gridY + 1, mapGridHeight - 1); m++)
			{
				Guid guid2 = Globals.MapGrid[l, m];
				if (!(guid2 == Guid.Empty))
				{
					DrawMap(guid2, 0);
				}
			}
		}
		ChronoImage.Tour("monde : sol des cartes");
		Meteo.DessinerSol();
		Meteo.DessinerAuDelaSol();
		Globals.OnGameDraw(DrawStates.GroundLayers, deltaTime);
		Animation[] array3 = array;
		foreach (Animation animation2 in array3)
		{
			animation2.Draw();
		}
		Globals.OnGameDraw(DrawStates.BelowPlayer, deltaTime);
		ChronoImage.Tour("monde : meteo au sol et animations");
		int mapHeight = Options.Instance.Map.MapHeight;
		GrassReact.DrawGround();
		ChronoImage.Tour("sol : herbe qui reagit (reste)");
		Wildlife.DrawGround();
		CaveLife.DrawGround();
		ChronoImage.Tour("sol : faune et grottes");
		Transformation.DrawGround();
		Blood.DrawGround();
		KiBlast.DrawGround();
		Knockback.DrawGround();
		Echanges.DrawGround();
		KiBeam.DrawGround();
		Maitres.DrawGround();
		Flight.DrawGround();
		SandSteps.DrawGround();
		DesertLife.DrawGround();
		ChronoImage.Tour("sol : ki, sang, vol, sable");
		CityLife.DrawGround();
		ChronoImage.Tour("sol : desert et villes");
		Recolte.DessinerSol();
		HouseWorld.DrawGround();
		DungeonWorld.DrawGround();
		BossWorld.DrawGround();
		NamekFinal.DrawSol();
		Globals.Me?.DrawTargets();
		foreach (Entity value in Globals.Entities.Values)
		{
			value?.DrawCastArea();
		}
		Afterimages.Draw();
		DashAnime.Draw();
		SprintDust.Draw();
		SprintSkid.Draw();
		ChronoImage.Tour("monde : effets au sol");
		KiBlast.PreparerLesRangees(mapHeight * 5 - 1);
		Casse.PreparerLesRangees(mapHeight * 5 - 1, gridX, gridY);
		for (int num = 0; num < mapHeight * 5; num++)
		{
			for (int num2 = 0; num2 < 3; num2++)
			{
				foreach (Entity item in RangeeParPieds(num2, num))
				{
					if (!Flight.IsAirborne(item))
					{
						KiBlast.DessinerDansLaRangee(num, item.Origin.Y, item.Id);
						if (num2 == 1)
						{
							Casse.DessinerDansLaRangee(num, item.Origin.Y);
						}
						Globals.OnGameDraw(DrawStates.BeforeEntity, item, deltaTime);
						CombatFeedback.DrawSparksBehind(item);
						double maintenant = ChronoImage.Maintenant;
						item.Draw();
						ChronoImage.EntiteLente(item, maintenant, "dessin");
						CombatFeedback.DrawSparksInFront(item);
						Globals.OnGameDraw(DrawStates.AfterEntity, item, deltaTime);
						EntitiesDrawn++;
					}
				}
				if (num2 != 0 || num <= 0 || num % mapHeight != 0)
				{
					continue;
				}
				for (int num3 = gridX - 1; num3 <= gridX + 1; num3++)
				{
					int num4 = gridY - 2 + (int)Math.Floor((float)num / (float)mapHeight);
					if (num3 >= 0 && num3 < mapGridWidth && num4 >= 0 && num4 < mapGridHeight && Globals.MapGrid[num3, num4] != Guid.Empty)
					{
						MapInstance.Get(Globals.MapGrid[num3, num4])?.DrawItemsAndLights();
					}
				}
			}
			KiBlast.DessinerDansLaRangee(num, float.MaxValue);
			Casse.DessinerDansLaRangee(num, float.MaxValue);
			KiBlast.DrawClashes(num, mapHeight * 5 - 1);
			HouseWorld.DrawRow(num);
		}
		KiBlast.DrawBehind();
		KiBeam.Draw();
		Maitres.Draw();
		KiCharge.Draw();
		Transformation.Draw();
		Echanges.DrawUnderCanopy();
		Knockback.DrawFront();
		CombatFeedback.DrawUnderCanopy();
		Blood.Draw();
		Animation[] array4 = array;
		foreach (Animation animation3 in array4)
		{
			if (!KiBlast.DessineeDansLesRangees(animation3))
			{
				animation3.Draw(upper: false, alternate: true);
				animation3.Draw(upper: true, alternate: true);
			}
		}
		for (int num6 = Math.Max(gridX - 1, 0); num6 <= Math.Min(gridX + 1, mapGridWidth - 1); num6++)
		{
			for (int num7 = Math.Max(gridY - 1, 0); num7 <= Math.Min(gridY + 1, mapGridHeight - 1); num7++)
			{
				Guid guid3 = Globals.MapGrid[num6, num7];
				if (!(guid3 == Guid.Empty))
				{
					DrawMap(Globals.MapGrid[num6, num7], 1);
				}
			}
		}
		for (int num8 = 0; num8 < mapHeight * 5; num8++)
		{
			for (int num9 = 3; num9 < 6; num9++)
			{
				foreach (Entity item2 in RangeeParPieds(num9, num8))
				{
					if (!Flight.IsAirborne(item2))
					{
						Globals.OnGameDraw(DrawStates.BeforeEntity, item2, deltaTime);
						CombatFeedback.DrawSparksBehind(item2);
						double maintenant2 = ChronoImage.Maintenant;
						item2.Draw();
						ChronoImage.EntiteLente(item2, maintenant2, "dessin");
						CombatFeedback.DrawSparksInFront(item2);
						Globals.OnGameDraw(DrawStates.AfterEntity, item2, deltaTime);
						EntitiesDrawn++;
					}
				}
			}
		}
		ChronoImage.Tour("monde : personnages");
		Globals.OnGameDraw(DrawStates.AbovePlayer, deltaTime);
		bool ouverte = Feuillage.Commencer();
		for (int num10 = Math.Max(gridX - 1, 0); num10 <= Math.Min(gridX + 1, mapGridWidth - 1); num10++)
		{
			for (int num11 = Math.Max(gridY - 1, 0); num11 <= Math.Min(gridY + 1, mapGridHeight - 1); num11++)
			{
				Guid guid4 = Globals.MapGrid[num10, num11];
				if (!(guid4 == Guid.Empty))
				{
					DrawMap(guid4, 2);
				}
			}
		}
		Feuillage.Finir(ouverte);
		Globals.OnGameDraw(DrawStates.FringeLayers, deltaTime);
		ChronoImage.Tour("monde : cimes des cartes");
		Flight.DessinerAltitude();
		Flight.DrawTrails();
		WorldMarks.DrawAir();
		Casse.DessinerAir();
		Wildlife.DrawAir();
		CaveLife.DrawAir();
		DesertLife.DrawAir();
		CityLife.DrawAir();
		BossWorld.DrawAir();
		EntitiesDrawn += Flight.DrawAirborne(deltaTime);
		Vent.DessinerFeuilles();
		KiBeam.DrawAir();
		Maitres.DrawAir();
		Animation[] array5 = array;
		foreach (Animation animation4 in array5)
		{
			animation4.Draw(upper: true);
		}
		ChronoImage.Tour("monde : airs");
		MapInstance.NouvelleImageObjets();
		for (int num13 = Math.Max(gridX - 1, 0); num13 <= Math.Min(gridX + 1, mapGridWidth - 1); num13++)
		{
			for (int num14 = Math.Max(gridY - 1, 0); num14 <= Math.Min(gridY + 1, mapGridHeight - 1); num14++)
			{
				Guid guid5 = Globals.MapGrid[num13, num14];
				if (!(guid5 == Guid.Empty) && MapInstance.TryGet(guid5, out MapInstance instance))
				{
					double maintenant3 = ChronoImage.Maintenant;
					instance.DrawWeather();
					double maintenant4 = ChronoImage.Maintenant;
					instance.DrawFog();
					double maintenant5 = ChronoImage.Maintenant;
					instance.DrawOverlayGraphic();
					double maintenant6 = ChronoImage.Maintenant;
					instance.DrawItemNames();
					double maintenant7 = ChronoImage.Maintenant;
					ChronoImage.Ajouter("meteo : particules", maintenant4 - maintenant3);
					ChronoImage.Ajouter("meteo : brouillard", maintenant5 - maintenant4);
					ChronoImage.Ajouter("meteo : image d'ambiance", maintenant6 - maintenant5);
					ChronoImage.Ajouter("meteo : noms des objets au sol", maintenant7 - maintenant6);
				}
			}
		}
		ChronoImage.Tour("monde : meteo et brouillard des cartes");
		Meteo.DessinerCiel();
		Meteo.DessinerAuDelaCiel();
		DrawOverlay();
		GenerateLightMap();
		DrawDarkness();
		ChronoImage.Tour("monde : ciel, nuit et lumieres");
		Meteo.DessinerApresNuit();
		CaveLife.DrawAfterDark();
		DesertLife.DrawAfterDark();
		CityLife.DrawAfterDark();
		DungeonWorld.DrawAfterDark();
		Transformation.DrawVeil();
		NamekFinal.DrawVoile();
		LowHealth.Draw();
		Mort.DessinerVoile();
		HouseWorld.DrawOverlay();
		ChronoImage.Tour("monde : apres la nuit");
		FinirVueLissee();
		NamePlate.BeginFrame();
		bool interfaceEscamotee = BandesDeCinema.InterfaceEscamotee;
		for (int num15 = 0; num15 < mapHeight * 5; num15++)
		{
			for (int num16 = 0; num16 < 3; num16++)
			{
				foreach (Entity item3 in RangeeParPieds(num16, num15))
				{
					if (Clash.Cache(item3))
					{
						continue;
					}
					if (!interfaceEscamotee)
					{
						item3.DrawName(null);
					}
					if (item3.GetType() != typeof(Event))
					{
						if (!interfaceEscamotee)
						{
							item3.DrawHpBar();
							item3.DrawCastingBar();
						}
						item3.DrawAttackTelegraph();
					}
					item3.DrawChatBubbles();
				}
			}
		}
		for (int num17 = 0; num17 < mapHeight * 5; num17++)
		{
			for (int num18 = 3; num18 < 6; num18++)
			{
				foreach (Entity item4 in RangeeParPieds(num18, num17))
				{
					if (Clash.Cache(item4))
					{
						continue;
					}
					if (!interfaceEscamotee)
					{
						item4.DrawName(null);
					}
					if (item4.GetType() != typeof(Event))
					{
						if (!interfaceEscamotee)
						{
							item4.DrawHpBar();
							item4.DrawCastingBar();
						}
						item4.DrawAttackTelegraph();
					}
					item4.DrawChatBubbles();
				}
			}
		}
		if (!interfaceEscamotee)
		{
			Recolte.SurvolerLesCoinsDePeche();
			ScouterReadout.Draw();
		}
		Echanges.DrawFront();
		CombatFeedback.Draw();
		KiBlast.DrawFront();
		Recolte.Dessiner();
		Artisanat.Dessiner();
		LootFeedback.Draw();
		ProfessionLevelUp.Draw();
		if (!interfaceEscamotee)
		{
			PartyMarkers.Draw();
			QuestMarkers.Draw();
		}
		Transformation.DrawSky();
		KiCharge.DrawSky();
		NamekFinal.DrawMonde();
		for (int num19 = Math.Max(gridX - 1, 0); num19 <= Math.Min(gridX + 1, mapGridWidth - 1); num19++)
		{
			for (int num20 = Math.Max(gridY - 1, 0); num20 <= Math.Min(gridY + 1, mapGridHeight - 1); num20++)
			{
				Guid guid6 = Globals.MapGrid[num19, num20];
				if (!(guid6 == Guid.Empty) && MapInstance.TryGet(guid6, out MapInstance instance2))
				{
					instance2.DrawActionMsgs();
				}
			}
		}
		Animation[] array6 = array;
		foreach (Animation animation5 in array6)
		{
			animation5.EndDraw();
		}
		ChronoImage.Tour("monde : noms, barres et textes");
	}

	public static void Render(TimeSpan deltaTime, TimeSpan totalTime)
	{
		GameRenderer renderer = Renderer;
		if (renderer == null)
		{
			return;
		}
		bool flag = false;
		if (renderer != null && renderer.HasScreenshotRequests)
		{
			flag = renderer.BeginScreenshot();
		}
		GameStates gameState = Globals.GameState;
		int num = Math.Max(1, Options.Instance?.Map?.TileWidth ?? 32);
		if (Globals.Database.WorldZoom < MinimumWorldScale)
		{
			Globals.Database.WorldZoom = MinimumWorldScale;
		}
		(int Largeur, int Hauteur) tuple = TailleDuMonde(renderer.ScreenWidth, renderer.ScreenHeight);
		int item = tuple.Largeur;
		int item2 = tuple.Hauteur;
		ZoomDeLaVue = ((gameState == GameStates.InGame) ? ZoomAdapte(Globals.Database.WorldZoom, item, item2) : Globals.Database.WorldZoom);
		WorldScale = MathF.Max(1f / (float)num, MathF.Round(ZoomDeLaVue * CombatFeedback.ZoomFactor * (float)num) / (float)num);
		renderer.Scale = ((gameState == GameStates.InGame) ? WorldScale : 1f);
		if (!renderer.Begin())
		{
			return;
		}
		if (renderer.ScreenWidth != sOldWidth || renderer.ScreenHeight != sOldHeight || renderer.DisplayModeChanged())
		{
			sDarknessTexture = null;
			Intersect.Client.Interface.Interface.DestroyGwen();
			Intersect.Client.Interface.Interface.InitGwen();
			sOldWidth = renderer.ScreenWidth;
			sOldHeight = renderer.ScreenHeight;
		}
		renderer.Clear(Color.Black);
		DrawCalls = 0;
		MapsDrawn = 0;
		EntitiesDrawn = 0;
		LightsDrawn = 0;
		bool flag2 = CommencerMondeAgrandi(gameState == GameStates.InGame);
		UpdateView();
		switch (gameState)
		{
		case GameStates.Intro:
			DrawIntro();
			break;
		case GameStates.Menu:
			DrawMenu();
			break;
		case GameStates.Loading:
			MenuLoadingScreen.Draw();
			break;
		case GameStates.InGame:
			try
			{
				DrawInGame(deltaTime);
			}
			finally
			{
				FinirVueLissee();
				if (flag2)
				{
					FinirMondeAgrandi();
				}
			}
			break;
		default:
			throw Exceptions.UnreachableInvalidEnum(gameState);
		case GameStates.Error:
			break;
		}
		FloatRect currentView = CurrentView;
		if (flag2)
		{
			(int Largeur, int Hauteur) tailleReelleDeLEcran = TailleReelleDeLEcran;
			int item3 = tailleReelleDeLEcran.Largeur;
			int item4 = tailleReelleDeLEcran.Hauteur;
			CurrentView = new FloatRect(currentView.X, currentView.Y, item3, item4);
		}
		renderer.Scale = Globals.Database.UIScale;
		bool flag3 = gameState == GameStates.InGame && CommencerInterfaceReduite();
		try
		{
			using (ChronoImage.Etape("interface"))
			{
				Intersect.Client.Interface.Interface.DrawGui(deltaTime, totalTime);
			}
		}
		finally
		{
			if (flag3)
			{
				FinirInterfaceReduite();
			}
		}
		if (gameState == GameStates.InGame)
		{
			using (ChronoImage.Etape("carte des environs"))
			{
				LocalMap.Draw();
			}
			NamekFinal.Draw();
		}
		Transformation.DrawFlash();
		Mort.DessinerBlanc();
		DrawGameTexture(renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), CurrentView, new Color((int)Fade.Alpha, 0, 0, 0));
		if (!flag)
		{
			IGameTexture cursor = Renderer.Cursor;
			if (cursor != null)
			{
				Vector2 vector = ConvertToWorldPointNoZoom(Globals.InputManager.GetMousePosition());
				float echelleDuCurseur = EchelleDuCurseur;
				DrawGameTexture(cursor, new FloatRect(0f, 0f, cursor.Width, cursor.Height), new FloatRect(vector.X, vector.Y, (float)cursor.Width * echelleDuCurseur, (float)cursor.Height * echelleDuCurseur), Color.White);
			}
		}
		renderer.End();
		if (flag2)
		{
			CurrentView = currentView;
		}
		if (flag)
		{
			renderer.EndScreenshot();
		}
	}

	private static void DrawMap(Guid mapId, int layer)
	{
		if (MapInstance.TryGet(mapId, out MapInstance instance) && new FloatRect(instance.X, instance.Y, Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth, Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight).IntersectsWith(WorldViewport))
		{
			double maintenant = ChronoImage.Maintenant;
			instance.Draw(layer);
			ChronoImage.CarteLente(instance.Name, layer, maintenant);
			if (layer == 0)
			{
				MapsDrawn++;
			}
		}
	}

	private static void DrawMapPanorama(Guid mapId)
	{
		if (MapInstance.TryGet(mapId, out MapInstance instance) && new FloatRect(instance.X, instance.Y, Options.Instance.Map.TileWidth * Options.Instance.Map.MapWidth, Options.Instance.Map.TileHeight * Options.Instance.Map.MapHeight).IntersectsWith(WorldViewport))
		{
			instance.DrawPanorama();
		}
	}

	public static void DrawOverlay()
	{
		if (Renderer == null)
		{
			return;
		}
		if (MapInstance.TryGet(Globals.Me?.MapId ?? default(Guid), out MapInstance instance))
		{
			float num = Timing.Global.MillisecondsUtc - sOverlayUpdate;
			if (OverlayColor.A != instance.AHue || OverlayColor.R != instance.RHue || OverlayColor.G != instance.GHue || OverlayColor.B != instance.BHue)
			{
				if (OverlayColor.A < instance.AHue)
				{
					if (OverlayColor.A + (int)(255f * num / 2000f) > instance.AHue)
					{
						OverlayColor.A = (byte)instance.AHue;
					}
					else
					{
						OverlayColor.A += (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.A > instance.AHue)
				{
					if (OverlayColor.A - (int)(255f * num / 2000f) < instance.AHue)
					{
						OverlayColor.A = (byte)instance.AHue;
					}
					else
					{
						OverlayColor.A -= (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.R < instance.RHue)
				{
					if (OverlayColor.R + (int)(255f * num / 2000f) > instance.RHue)
					{
						OverlayColor.R = (byte)instance.RHue;
					}
					else
					{
						OverlayColor.R += (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.R > instance.RHue)
				{
					if (OverlayColor.R - (int)(255f * num / 2000f) < instance.RHue)
					{
						OverlayColor.R = (byte)instance.RHue;
					}
					else
					{
						OverlayColor.R -= (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.G < instance.GHue)
				{
					if (OverlayColor.G + (int)(255f * num / 2000f) > instance.GHue)
					{
						OverlayColor.G = (byte)instance.GHue;
					}
					else
					{
						OverlayColor.G += (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.G > instance.GHue)
				{
					if (OverlayColor.G - (int)(255f * num / 2000f) < instance.GHue)
					{
						OverlayColor.G = (byte)instance.GHue;
					}
					else
					{
						OverlayColor.G -= (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.B < instance.BHue)
				{
					if (OverlayColor.B + (int)(255f * num / 2000f) > instance.BHue)
					{
						OverlayColor.B = (byte)instance.BHue;
					}
					else
					{
						OverlayColor.B += (byte)(255f * num / 2000f);
					}
				}
				if (OverlayColor.B > instance.BHue)
				{
					if (OverlayColor.B - (int)(255f * num / 2000f) < instance.BHue)
					{
						OverlayColor.B = (byte)instance.BHue;
					}
					else
					{
						OverlayColor.B -= (byte)(255f * num / 2000f);
					}
				}
			}
		}
		DrawGameTexture(Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), CurrentView, OverlayColor);
		sOverlayUpdate = Timing.Global.MillisecondsUtc;
	}

	public static FloatRect GetSourceRect(IGameTexture gameTexture)
	{
		if (gameTexture != null)
		{
			return new FloatRect(0f, 0f, gameTexture.Width, gameTexture.Height);
		}
		return default(FloatRect);
	}

	public static void DrawFullScreenTexture(IGameTexture tex, float alpha = 1f)
	{
		if (Renderer != null)
		{
			int num = Renderer.ScreenWidth / 2 - tex.Width / 2;
			int num2 = Renderer.ScreenHeight / 2 - tex.Height / 2;
			int num3 = tex.Width;
			int num4 = tex.Height;
			if (num3 < Renderer.ScreenWidth)
			{
				int num5 = Renderer.ScreenWidth - num3;
				num -= num5 / 2;
				num3 += num5;
			}
			if (num4 < Renderer.ScreenHeight)
			{
				int num5 = Renderer.ScreenHeight - num4;
				num2 -= num5 / 2;
				num4 += num5;
			}
			DrawGameTexture(tex, GetSourceRect(tex), new FloatRect((float)num + Renderer.GetView().X, (float)num2 + Renderer.GetView().Y, num3, num4), new Color((int)(alpha * 255f), 255, 255, 255));
		}
	}

	public static void DrawFullScreenTextureCentered(IGameTexture tex, float alpha = 1f)
	{
		if (Renderer != null)
		{
			int num = Renderer.ScreenWidth / 2 - tex.Width / 2;
			int num2 = Renderer.ScreenHeight / 2 - tex.Height / 2;
			int width = tex.Width;
			int height = tex.Height;
			DrawGameTexture(tex, GetSourceRect(tex), new FloatRect((float)num + Renderer.GetView().X, (float)num2 + Renderer.GetView().Y, width, height), new Color((int)(alpha * 255f), 255, 255, 255));
		}
	}

	public static void DrawFullScreenTextureStretched(IGameTexture tex)
	{
		if (Renderer != null)
		{
			DrawGameTexture(tex, GetSourceRect(tex), new FloatRect(Renderer.GetView().X, Renderer.GetView().Y, Renderer.ScreenWidth, Renderer.ScreenHeight), Color.White);
		}
	}

	public static void DrawFullScreenTextureFitWidth(IGameTexture tex)
	{
		if (Renderer != null)
		{
			float num = (float)Renderer.ScreenWidth / (float)tex.Width;
			float h = (float)tex.Height * num;
			float num2 = (float)(Renderer.ScreenHeight - tex.Height) / 2f;
			DrawGameTexture(tex, GetSourceRect(tex), new FloatRect(Renderer.GetView().X, Renderer.GetView().Y + num2, Renderer.ScreenWidth, h), Color.White);
		}
	}

	public static void DrawFullScreenTextureFitHeight(IGameTexture tex)
	{
		if (Renderer != null)
		{
			float num = (float)Renderer.ScreenHeight / (float)tex.Height;
			float num2 = (float)tex.Width * num;
			float num3 = ((float)Renderer.ScreenWidth - num2) / 2f;
			DrawGameTexture(tex, GetSourceRect(tex), new FloatRect(Renderer.GetView().X + num3, Renderer.GetView().Y, num2, Renderer.ScreenHeight), Color.White);
		}
	}

	public static void DrawFullScreenTextureFitMinimum(IGameTexture tex)
	{
		if (Renderer != null)
		{
			if (Renderer.ScreenWidth > Renderer.ScreenHeight)
			{
				DrawFullScreenTextureFitHeight(tex);
			}
			else
			{
				DrawFullScreenTextureFitWidth(tex);
			}
		}
	}

	public static void DrawFullScreenTextureFitMaximum(IGameTexture tex)
	{
		if (Renderer != null)
		{
			if (Renderer.ScreenWidth < Renderer.ScreenHeight)
			{
				DrawFullScreenTextureFitHeight(tex);
			}
			else
			{
				DrawFullScreenTextureFitWidth(tex);
			}
		}
	}

	private static void UpdateView()
	{
		if (Renderer == null)
		{
			return;
		}
		float scale = Renderer.Scale;
		if (Globals.GameState != GameStates.InGame || !MapInstance.TryGet(Globals.Me?.MapId ?? Guid.Empty, out MapInstance instance))
		{
			int screenWidth = Renderer.ScreenWidth;
			int screenHeight = Renderer.ScreenHeight;
			int num = 0;
			int num2 = 0;
			CurrentView = new FloatRect(num, num2, (float)screenWidth / scale, (float)screenHeight / scale);
			return;
		}
		int num3 = Options.Instance.Map.MapWidth * Options.Instance.Map.TileWidth;
		int num4 = Options.Instance.Map.MapHeight * Options.Instance.Map.TileHeight;
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		float num5 = num3;
		float num6 = num4;
		float num7 = num3 * 2;
		float num8 = num4 * 2;
		if (instance.CameraHolds[2])
		{
			num5 -= (float)num3;
		}
		if (instance.CameraHolds[0])
		{
			num6 -= (float)num4;
		}
		if (instance.CameraHolds[3])
		{
			num7 -= (float)num3;
		}
		if (instance.CameraHolds[1])
		{
			num8 -= (float)num4;
		}
		num5 = (float)instance.X - num5;
		num6 = (float)instance.Y - num6;
		num7 = (float)instance.X + num7;
		num8 = (float)instance.Y + num8;
		float w = num7 - num5;
		float h = num8 - num6;
		FloatRect floatRect = new FloatRect(num5, num6, w, h);
		float num9;
		float num10;
		if (me == Globals.Me)
		{
			(num9, num10) = CameraFollow.Lag(me);
		}
		else
		{
			num9 = 0f;
			num10 = 0f;
		}
		FloatRect floatRect2 = new FloatRect((int)Math.Ceiling(me.Center.X - num9 - (float)Renderer.ScreenWidth / scale / 2f), (int)Math.Ceiling(me.Center.Y - num10 - (float)Renderer.ScreenHeight / scale / 2f), (float)Renderer.ScreenWidth / scale, (float)Renderer.ScreenHeight / scale);
		if (floatRect.Width >= floatRect2.Width)
		{
			if (floatRect2.Left < floatRect.Left)
			{
				floatRect2.X = floatRect.Left;
			}
			if (floatRect2.Right > floatRect.Right)
			{
				floatRect2.X = floatRect.Right - floatRect2.Width;
			}
		}
		else if (Options.Instance.Map.GameBorderStyle == GameBorderStyle.Seamed)
		{
			floatRect2.X = floatRect.X - (floatRect2.Width - floatRect.Width) / 2f;
		}
		if (floatRect.Height >= floatRect2.Height)
		{
			if (floatRect2.Top < floatRect.Top)
			{
				floatRect2.Y = floatRect.Top;
			}
			if (floatRect2.Bottom > floatRect.Bottom)
			{
				floatRect2.Y = floatRect.Bottom - floatRect2.Height;
			}
		}
		else if (Options.Instance.Map.GameBorderStyle == GameBorderStyle.Seamed)
		{
			floatRect2.Y = floatRect.Y - (floatRect2.Height - floatRect.Height) / 2f;
		}
		(float, float) shakeOffset = CombatFeedback.ShakeOffset;
		floatRect2.X += shakeOffset.Item1;
		floatRect2.Y += shakeOffset.Item2;
		CurrentView = new FloatRect(MathF.Round(floatRect2.X * scale) / scale, MathF.Round(floatRect2.Y * scale) / scale, floatRect2.Width * scale, floatRect2.Height * scale);
		if (me == Globals.Me)
		{
			TraceDeplacement.Noter(me, num9, num10, CurrentView);
		}
	}

	private static void ClearDarknessTexture()
	{
		if (Globals.Database.EnableLighting && Renderer != null)
		{
			if (sDarknessTexture != null && (sDarknessTexture.Width != Renderer.ScreenWidth || sDarknessTexture.Height != Renderer.ScreenHeight))
			{
				sDarknessTexture.Dispose();
				sDarknessTexture = null;
			}
			if (sDarknessTexture == null)
			{
				sDarknessTexture = Renderer.CreateRenderTexture(Renderer.ScreenWidth, Renderer.ScreenHeight);
			}
			sDarknessTexture.Clear(Color.Black);
		}
	}

	private static void GenerateLightMap()
	{
		if (Renderer == null || sDarknessTexture == null)
		{
			return;
		}
		Guid guid = Globals.Me?.MapId ?? default(Guid);
		if (!(guid == default(Guid)) && Globals.Database.EnableLighting && MapInstance.TryGet(guid, out MapInstance instance))
		{
			FloatRect targetRect = new FloatRect(default(Vector2), sDarknessTexture.Dimensions / WorldScale);
			if (instance.IsIndoors)
			{
				DrawGameTexture(Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), targetRect, new Color((byte)BrightnessLevel, 255, 255, 255), sDarknessTexture, GameBlendModes.Add);
			}
			else
			{
				DrawGameTexture(Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), targetRect, new Color(255, 255, 255, 255), sDarknessTexture, GameBlendModes.Add);
				DrawGameTexture(Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), targetRect, Meteo.CouleurDuMonde(instance) ?? new Color((int)Time.GetTintColor().A, (int)Time.GetTintColor().R, (int)Time.GetTintColor().G, (int)Time.GetTintColor().B), sDarknessTexture);
			}
			Meteo.AjouterLumieres(instance);
			Meteo.AjouterLampadaires(instance);
			CityLife.AddLights();
			DungeonWorld.AddLights();
			AddLight((int)Math.Ceiling(Globals.Me.Center.X), (int)Math.Ceiling(Globals.Me.Center.Y), (int)sPlayerLightSize, (byte)sPlayerLightIntensity, sPlayerLightExpand, Color.FromArgb((int)PlayerLightColor.A, (int)PlayerLightColor.R, (int)PlayerLightColor.G, (int)PlayerLightColor.B));
			DrawLights();
			DrawKiLights();
			Casse.DessinerDansLaLumiere();
			CityLife.DrawLightMap();
			sDarknessTexture.End();
		}
	}

	internal static void AddLightTexture(IGameTexture texture, FloatRect source, FloatRect world, Color color)
	{
		if (sDarknessTexture != null && Globals.Database.EnableLighting)
		{
			DrawGameTexture(texture, source, new FloatRect(world.X - (float)(int)CurrentView.Left, world.Y - (float)(int)CurrentView.Top, world.Width, world.Height), color, sDarknessTexture, GameBlendModes.Add);
		}
	}

	private static void DrawKiLights()
	{
		if (sKiLightBroken || Renderer == null || sDarknessTexture == null)
		{
			return;
		}
		try
		{
			List<KiLight.Light> list = KiLight.Collect();
			if (list.Count == 0)
			{
				return;
			}
			if (sKiLightTexture == null)
			{
				if (!(Renderer is MonoRenderer monoRenderer))
				{
					return;
				}
				sKiLightTexture = monoRenderer.CreerTexturePixels("lumiere-ki", 128, 128, KiLight.BuildTexture());
			}
			FloatRect srcRectangle = new FloatRect(0f, 0f, 128f, 128f);
			foreach (KiLight.Light item in list)
			{
				int num = (int)Math.Clamp(210f * item.Intensity, 0f, 255f);
				if (num > 0 && !(item.Radius <= 1f))
				{
					DrawGameTexture(sKiLightTexture, srcRectangle, new FloatRect(item.X - (float)(int)CurrentView.Left - item.Radius, item.Y - (float)(int)CurrentView.Top - item.Radius, item.Radius * 2f, item.Radius * 2f), new Color(num, item.Color.R, item.Color.G, item.Color.B), sDarknessTexture, GameBlendModes.Add);
				}
			}
		}
		catch (Exception)
		{
			sKiLightBroken = true;
		}
	}

	public static void DrawDarkness()
	{
		if (Globals.Database.EnableLighting && sDarknessTexture != null)
		{
			GameShader shader = Globals.ContentManager.GetShader("radialgradient");
			if (shader != null)
			{
				DrawGameTexture(sDarknessTexture, sDarknessTexture.Bounds, WorldViewport, Color.White, null, GameBlendModes.Multiply);
			}
		}
	}

	public static void AddLight(int x, int y, int size, byte intensity, float expand, Color color)
	{
		if (Globals.Database.EnableLighting && size != 0)
		{
			sLightQueue.Add(new LightDescriptor(0, 0, x, y, intensity, size, expand, color));
			LightsDrawn++;
		}
	}

	private static void DrawLights()
	{
		if (!Globals.Database.EnableLighting || Renderer == null)
		{
			return;
		}
		GameShader shader = Globals.ContentManager.GetShader("radialgradient");
		if (shader == null)
		{
			return;
		}
		foreach (IGrouping<int, LightDescriptor> item in from c in sLightQueue
			group c by c.GetHashCode())
		{
			foreach (LightDescriptor item2 in item)
			{
				int num = item2.OffsetX - ((int)CurrentView.Left + item2.Size);
				int num2 = item2.OffsetY - ((int)CurrentView.Top + item2.Size);
				shader.SetColor("LightColor", new Color(item2.Intensity, item2.Color.R, item2.Color.G, item2.Color.B));
				shader.SetFloat("Expand", item2.Expand / 100f);
				DrawGameTexture(Renderer.WhitePixel, new FloatRect(0f, 0f, 1f, 1f), new FloatRect(num, num2, item2.Size * 2, item2.Size * 2), new Color(255, 255, 255, 255), sDarknessTexture, GameBlendModes.Add, shader);
			}
		}
		sLightQueue.Clear();
	}

	public static void UpdatePlayerLight()
	{
		if (!Globals.Database.EnableLighting || Globals.Me == null || !MapInstance.TryGet(Globals.Me.MapId, out MapInstance instance))
		{
			return;
		}
		float num = Timing.Global.MillisecondsUtc - sLightUpdate;
		float num2 = 255f * num / 2000f;
		byte b = (byte)((float)instance.Brightness / 100f * 255f);
		if (BrightnessLevel < (float)(int)b)
		{
			if (BrightnessLevel + num2 > (float)(int)b)
			{
				BrightnessLevel = (int)b;
			}
			else
			{
				BrightnessLevel += num2;
			}
		}
		if (BrightnessLevel > (float)(int)b)
		{
			if (BrightnessLevel - num2 < (float)(int)b)
			{
				BrightnessLevel = (int)b;
			}
			else
			{
				BrightnessLevel -= num2;
			}
		}
		if (PlayerLightColor.R != (float)(int)instance.PlayerLightColor.R || PlayerLightColor.G != (float)(int)instance.PlayerLightColor.G || PlayerLightColor.B != (float)(int)instance.PlayerLightColor.B)
		{
			if (PlayerLightColor.R < (float)(int)instance.PlayerLightColor.R)
			{
				if (PlayerLightColor.R + num2 > (float)(int)instance.PlayerLightColor.R)
				{
					PlayerLightColor.R = (int)instance.PlayerLightColor.R;
				}
				else
				{
					PlayerLightColor.R += num2;
				}
			}
			if (PlayerLightColor.R > (float)(int)instance.PlayerLightColor.R)
			{
				if (PlayerLightColor.R - num2 < (float)(int)instance.PlayerLightColor.R)
				{
					PlayerLightColor.R = (int)instance.PlayerLightColor.R;
				}
				else
				{
					PlayerLightColor.R -= num2;
				}
			}
			if (PlayerLightColor.G < (float)(int)instance.PlayerLightColor.G)
			{
				if (PlayerLightColor.G + num2 > (float)(int)instance.PlayerLightColor.G)
				{
					PlayerLightColor.G = (int)instance.PlayerLightColor.G;
				}
				else
				{
					PlayerLightColor.G += num2;
				}
			}
			if (PlayerLightColor.G > (float)(int)instance.PlayerLightColor.G)
			{
				if (PlayerLightColor.G - num2 < (float)(int)instance.PlayerLightColor.G)
				{
					PlayerLightColor.G = (int)instance.PlayerLightColor.G;
				}
				else
				{
					PlayerLightColor.G -= num2;
				}
			}
			if (PlayerLightColor.B < (float)(int)instance.PlayerLightColor.B)
			{
				if (PlayerLightColor.B + num2 > (float)(int)instance.PlayerLightColor.B)
				{
					PlayerLightColor.B = (int)instance.PlayerLightColor.B;
				}
				else
				{
					PlayerLightColor.B += num2;
				}
			}
			if (PlayerLightColor.B > (float)(int)instance.PlayerLightColor.B)
			{
				if (PlayerLightColor.B - num2 < (float)(int)instance.PlayerLightColor.B)
				{
					PlayerLightColor.B = (int)instance.PlayerLightColor.B;
				}
				else
				{
					PlayerLightColor.B -= num2;
				}
			}
		}
		if (sPlayerLightSize != (float)instance.PlayerLightSize)
		{
			if (sPlayerLightSize < (float)instance.PlayerLightSize)
			{
				if (sPlayerLightSize + 500f * num / 2000f > (float)instance.PlayerLightSize)
				{
					sPlayerLightSize = instance.PlayerLightSize;
				}
				else
				{
					sPlayerLightSize += 500f * num / 2000f;
				}
			}
			if (sPlayerLightSize > (float)instance.PlayerLightSize)
			{
				if (sPlayerLightSize - 500f * num / 2000f < (float)instance.PlayerLightSize)
				{
					sPlayerLightSize = instance.PlayerLightSize;
				}
				else
				{
					sPlayerLightSize -= 500f * num / 2000f;
				}
			}
		}
		if (sPlayerLightIntensity < (float)(int)instance.PlayerLightIntensity)
		{
			if (sPlayerLightIntensity + num2 > (float)(int)instance.PlayerLightIntensity)
			{
				sPlayerLightIntensity = (int)instance.PlayerLightIntensity;
			}
			else
			{
				sPlayerLightIntensity += num2;
			}
		}
		if (sPlayerLightIntensity > (float)instance.AHue)
		{
			if (sPlayerLightIntensity - num2 < (float)(int)instance.PlayerLightIntensity)
			{
				sPlayerLightIntensity = (int)instance.PlayerLightIntensity;
			}
			else
			{
				sPlayerLightIntensity -= num2;
			}
		}
		if (sPlayerLightExpand < instance.PlayerLightExpand)
		{
			if (sPlayerLightExpand + 100f * num / 2000f > instance.PlayerLightExpand)
			{
				sPlayerLightExpand = instance.PlayerLightExpand;
			}
			else
			{
				sPlayerLightExpand += 100f * num / 2000f;
			}
		}
		if (sPlayerLightExpand > instance.PlayerLightExpand)
		{
			if (sPlayerLightExpand - 100f * num / 2000f < instance.PlayerLightExpand)
			{
				sPlayerLightExpand = instance.PlayerLightExpand;
			}
			else
			{
				sPlayerLightExpand -= 100f * num / 2000f;
			}
		}
		sPlayerLightIntensity = (float)MathHelper.Clamp(sPlayerLightIntensity, 0.0, 255.0);
		sLightUpdate = Timing.Global.MillisecondsUtc;
	}

	public static Vector2 ConvertToWorldPoint(Vector2 windowPoint)
	{
		float num = WorldScale * EchelleDuMonde;
		return new Vector2((int)Math.Floor(windowPoint.X / num + CurrentView.Left), (int)Math.Floor(windowPoint.Y / num + CurrentView.Top));
	}

	public static Vector2 ConvertToWorldPointNoZoom(Vector2 windowPoint)
	{
		return new Vector2((int)Math.Floor(windowPoint.X + CurrentView.Left), (int)Math.Floor(windowPoint.Y + CurrentView.Top));
	}

	public static void DrawGameTexture(IGameTexture tex, float x, float y, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool drawImmediate = false)
	{
		FloatRect targetRect = new FloatRect(x, y, tex.Width, tex.Height);
		FloatRect srcRectangle = new FloatRect(0f, 0f, tex.Width, tex.Height);
		DrawGameTexture(tex, srcRectangle, targetRect, Color.White, renderTarget, blendMode, shader, rotationDegrees, drawImmediate);
	}

	public static void DrawGameTexture(IGameTexture tex, float x, float y, Color renderColor, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool drawImmediate = false)
	{
		FloatRect targetRect = new FloatRect(x, y, tex.Width, tex.Height);
		FloatRect srcRectangle = new FloatRect(0f, 0f, tex.Width, tex.Height);
		DrawGameTexture(tex, srcRectangle, targetRect, renderColor, renderTarget, blendMode, shader, rotationDegrees, drawImmediate);
	}

	public static void DrawGameTexture(IGameTexture tex, float dx, float dy, float sx, float sy, float w, float h, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool drawImmediate = false)
	{
		if (tex != null)
		{
			Renderer?.DrawTexture(tex, sx, sy, w, h, dx, dy, w, h, Color.White, renderTarget, blendMode, shader, rotationDegrees, isUi: false, drawImmediate);
		}
	}

	public static void DrawGameTexture(IGameTexture tex, FloatRect srcRectangle, FloatRect targetRect, Color renderColor, IGameRenderTexture? renderTarget = null, GameBlendModes blendMode = GameBlendModes.None, GameShader? shader = null, float rotationDegrees = 0f, bool drawImmediate = false)
	{
		if (tex != null)
		{
			Renderer?.DrawTexture(tex, srcRectangle.X, srcRectangle.Y, srcRectangle.Width, srcRectangle.Height, targetRect.X, targetRect.Y, targetRect.Width, targetRect.Height, Color.FromArgb(renderColor.A, renderColor.R, renderColor.G, renderColor.B), renderTarget, blendMode, shader, rotationDegrees, isUi: false, drawImmediate);
		}
	}

	public static float EchellePour(int largeurEcran, int hauteurEcran)
	{
#if !ANDROID
		if (largeurEcran > 0 && largeurEcran < 1600)
		{
			return (float)largeurEcran / 1600f;
		}
#else
		if (hauteurEcran > 0)
		{
			return MathF.Max(1f, (float)hauteurEcran / 600f);
		}
#endif
		if (largeurEcran <= 0 || hauteurEcran <= 0)
		{
			return 1f;
		}
		float num = MathF.Min((float)largeurEcran / 1920f, (float)hauteurEcran / 1080f);
		if (!(num >= 1.25f))
		{
			return 1f;
		}
		return num;
	}

	public static (int Largeur, int Hauteur) TailleDeLInterface(int largeurEcran, int hauteurEcran)
	{
		float num = EchellePour(largeurEcran, hauteurEcran);
		if (num == 1f)
		{
			return (Largeur: largeurEcran, Hauteur: hauteurEcran);
		}
		if (!(num < 1f))
		{
			return (Largeur: (int)MathF.Round((float)largeurEcran / num), Hauteur: (int)MathF.Round((float)hauteurEcran / num));
		}
		return (Largeur: 1600, Hauteur: (int)MathF.Ceiling((float)hauteurEcran / num));
	}

	private static bool CommencerInterfaceReduite()
	{
		if (!(Renderer is MonoRenderer monoRenderer) || EchelleDeLInterface == 1f)
		{
			return false;
		}
		var (largeur, hauteur) = TailleDeLInterface(monoRenderer.LargeurReelle, monoRenderer.HauteurReelle);
		return monoRenderer.CommencerInterfaceReduite(largeur, hauteur);
	}

	private static void FinirInterfaceReduite()
	{
		if (Renderer is MonoRenderer monoRenderer)
		{
			monoRenderer.FinirInterfaceReduite();
		}
	}

	public static (int Largeur, int Hauteur) TailleDuMonde(int largeurEcran, int hauteurEcran)
	{
		if (!(EchellePour(largeurEcran, hauteurEcran) > 1f))
		{
			return (Largeur: largeurEcran, Hauteur: hauteurEcran);
		}
		return TailleDeLInterface(largeurEcran, hauteurEcran);
	}

	private static bool CommencerMondeAgrandi(bool enJeu)
	{
		EchelleDuMonde = 1f;
		if (!enJeu || !(Renderer is MonoRenderer monoRenderer))
		{
			return false;
		}
		float num = EchellePour(monoRenderer.LargeurReelle, monoRenderer.HauteurReelle);
		if (num <= 1f)
		{
			return false;
		}
		var (largeur, hauteur) = TailleDuMonde(monoRenderer.LargeurReelle, monoRenderer.HauteurReelle);
		if (!monoRenderer.CommencerMondeAgrandi(largeur, hauteur))
		{
			return false;
		}
		EchelleDuMonde = num;
		return true;
	}

	private static void FinirMondeAgrandi()
	{
		if (Renderer is MonoRenderer monoRenderer)
		{
			monoRenderer.FinirMondeAgrandi();
		}
	}

	private static void CommencerVueLissee()
	{
		if (_mondeLisseOuvert || !(Renderer is MonoRenderer monoRenderer) || WorldScale <= 0f)
		{
			return;
		}
		GameDatabase? database = Globals.Database;
		if (database != null && database.ModePerformances)
		{
			return;
		}
		int num = Math.Max(1, Options.Instance?.Map?.TileWidth ?? 32);
		float num2 = MathF.Round(ZoomDeLaVue * (float)num) / (float)num;
		if (!(num2 >= 1.999f))
		{
			int num3 = (int)MathF.Ceiling((float)monoRenderer.ScreenWidth * 2f / num2);
			int num4 = (int)MathF.Ceiling((float)monoRenderer.ScreenHeight * 2f / num2);
			float num5 = MathF.Max(1f / (float)num, MathF.Round(WorldScale * 2f / num2 * (float)num) / (float)num);
			FloatRect currentView = CurrentView;
			if (monoRenderer.CommencerMondeLisse(num3, num4))
			{
				_vueDeLEcran = currentView;
				_echelleDeLEcran = WorldScale;
				_mondeLisseOuvert = true;
				WorldScale = num5;
				monoRenderer.Scale = num5;
				CurrentView = new FloatRect(MathF.Round(currentView.X * num5) / num5, MathF.Round(currentView.Y * num5) / num5, num3, num4);
			}
		}
	}

	private static void FinirVueLissee()
	{
		if (_mondeLisseOuvert && Renderer is MonoRenderer monoRenderer)
		{
			_mondeLisseOuvert = false;
			FloatRect currentView = CurrentView;
			monoRenderer.FinirMondeLisse(_echelleDeLEcran / WorldScale, (currentView.X - _vueDeLEcran.X) * _echelleDeLEcran, (currentView.Y - _vueDeLEcran.Y) * _echelleDeLEcran);
			WorldScale = _echelleDeLEcran;
			monoRenderer.Scale = _echelleDeLEcran;
			CurrentView = _vueDeLEcran;
		}
	}

	public static int TailleDeRepere(int points)
	{
		return Math.Max(7, (int)MathF.Round((float)points * ReductionDesReperes));
	}

	private static float ZoomAdapte(float voulu, int largeurEcran, int hauteurEcran)
	{
		int num = Math.Max(1, Options.Instance?.Map?.TileWidth ?? 32);
		float num2 = MathF.Round(voulu * (float)num) / (float)num;
		Guid? guid = Globals.Me?.MapId;
		if (guid.HasValue)
		{
			Guid valueOrDefault = guid.GetValueOrDefault();
			if (MapInstance.TryGet(valueOrDefault, out MapInstance instance))
			{
				bool[] cameraHolds = instance.CameraHolds;
				if (cameraHolds != null && cameraHolds.Length >= 4)
				{
					MapOptions mapOptions = Options.Instance?.Map;
					if (mapOptions == null)
					{
						return num2;
					}
					int num3 = mapOptions.MapWidth * mapOptions.TileWidth;
					int num4 = mapOptions.MapHeight * mapOptions.TileHeight;
					if (cameraHolds[2] && cameraHolds[3] && num3 > 0)
					{
						num2 = MathF.Max(num2, MathF.Ceiling((float)largeurEcran / (float)num3 * (float)num) / (float)num);
					}
					if (cameraHolds[0] && cameraHolds[1] && num4 > 0)
					{
						num2 = MathF.Max(num2, MathF.Ceiling((float)hauteurEcran / (float)num4 * (float)num) / (float)num);
					}
					return num2;
				}
			}
		}
		return num2;
	}
}
