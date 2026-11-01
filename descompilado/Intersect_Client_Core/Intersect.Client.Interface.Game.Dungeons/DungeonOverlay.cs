using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Maps;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Maps;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Dungeons;

internal sealed class DungeonOverlay : Intersect.Client.Framework.Gwen.Control.Base
{
	private const int FacadeMax = 8;

	protected override bool ShouldClip => false;

	public DungeonOverlay(Canvas canvas)
		: base(canvas, "DungeonOverlay")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		SetBounds(0, 0, canvas.Width, canvas.Height);
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		base.IsHidden = Globals.Me == null || Dungeon.InRun;
	}

	private Point ToLocal(float worldX, float worldY)
	{
		float num = ((base.Parent as Canvas)?.Scale ?? 1f) * Graphics.EchelleDeLInterface;
		FloatRect currentView = Graphics.CurrentView;
		float num2 = Graphics.WorldScale * Graphics.EchelleDuMonde;
		return new Point((int)MathF.Round((worldX - currentView.X) * num2 / num), (int)MathF.Round((worldY - currentView.Y) * num2 / num));
	}

	private static int HauteurDeLaFacade(MapInstance carte, DungeonDescriptor donjon)
	{
		if (donjon.EntranceY != 0 || carte.Up == Guid.Empty || !MapInstance.TryGet(carte.Up, out MapInstance instance) || instance.Layers == null)
		{
			return 0;
		}
		int entranceX = donjon.EntranceX;
		Guid guid = Guid.Empty;
		int num = 0;
		for (int num2 = Options.Instance.Map.MapHeight - 1; num2 >= 0; num2--)
		{
			bool flag = false;
			foreach (var (text2, array2) in instance.Layers)
			{
				if (text2 == "Ground" || array2 == null || entranceX < 0 || entranceX >= array2.GetLength(0) || num2 >= array2.GetLength(1))
				{
					continue;
				}
				Tile tile = array2[entranceX, num2];
				if (!(tile.TilesetId == Guid.Empty))
				{
					if (num == 0)
					{
						guid = tile.TilesetId;
						flag = true;
					}
					else if (tile.TilesetId == guid)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				break;
			}
			num++;
			if (num > 8)
			{
				return 0;
			}
		}
		return num;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		Guid[,] mapGrid = Globals.MapGrid;
		if (mapGrid == null)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		CharteScreen.Design d = CharteScreen.Design.For(base.Width, base.Height);
		int tileWidth = Options.Instance.Map.TileWidth;
		int tileHeight = Options.Instance.Map.TileHeight;
		foreach (DungeonDescriptor item in DatabaseObject<DungeonDescriptor>.Lookup.Values.OfType<DungeonDescriptor>())
		{
			if (item.Rooms.Count != 0 && MapInstance.TryGet(item.EntranceMapId, out MapInstance instance) && mapGrid.Cast<Guid>().Contains(instance.Id))
			{
				int num = HauteurDeLaFacade(instance, item);
				float num2 = ((num > 0) ? (0f - ((float)num + 0.1f)) : (-1.5f));
				Point point = ToLocal((float)instance.X + ((float)item.EntranceX + 0.5f) * (float)tileWidth, (float)instance.Y + ((float)item.EntranceY + num2) * (float)tileHeight);
				if (point.X >= -d.L(300f) && point.X <= base.Width + d.L(300f) && point.Y >= 0 && point.Y <= base.Height + d.L(80f))
				{
					string detail = Strings.Dungeons.PlateDetail.ToString(item.Settings(DungeonDifficulty.Normal).LevelMin, item.Settings(DungeonDifficulty.Heroic).LevelMax, 4);
					HouseArt.Plate(renderer, d, point.X, point.Y, new HouseArt.PlateContent(item.Name, TechniqueArt.Gold, detail, TechniqueArt.GoldPale, ForSale: true, Locked: false), 1f, Dungeon.Icone);
				}
			}
		}
		DungeonDescriptor dungeonDescriptor = Dungeon.EntranceNear(me);
		if (dungeonDescriptor != null)
		{
			long num3 = Dungeon.ChestReadyInSeconds(dungeonDescriptor.Id);
			string detail2 = ((num3 <= 0) ? Strings.Dungeons.PlateChestReady.ToString() : Strings.Dungeons.PlateChestLater.ToString(Dungeon.Duration(num3)));
			FloatRect bodyWorldPos = me.BodyWorldPos;
			Point point2 = ToLocal(bodyWorldPos.X + bodyWorldPos.Width / 2f, bodyWorldPos.Y);
			float num4 = (float)(Timing.Global.MillisecondsUtc % 1600) / 1600f;
			int num5 = (int)MathF.Round(-5f * d.S * (1f - MathF.Cos(num4 * ((float)Math.PI * 2f))) / 2f);
			HouseArt.Pill(renderer, d, point2.X, point2.Y - d.L(14f) + num5, Strings.Housing.PromptKey, Strings.Dungeons.PromptEnter, detail2, refused: false);
		}
	}
}
