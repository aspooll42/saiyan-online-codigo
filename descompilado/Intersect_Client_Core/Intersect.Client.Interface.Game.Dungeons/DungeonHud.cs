using System;
using System.Linq;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.NPCs;
using Intersect.Network.Packets.Server;

namespace Intersect.Client.Interface.Game.Dungeons;

internal sealed class DungeonHud : Intersect.Client.Framework.Gwen.Control.Base
{
	private readonly DungeonLeaveButton _leave;

	internal Intersect.Client.Framework.Gwen.Control.Base BoutonAbandon => _leave;

	protected override bool ShouldClip => false;

	private static bool EnDonjon
	{
		get
		{
			Player me = Globals.Me;
			if (me != null && Dungeon.InRun)
			{
				DungeonRoom currentRoom = Dungeon.CurrentRoom;
				if (currentRoom != null)
				{
					return currentRoom.MapId == me.MapId;
				}
			}
			return false;
		}
	}

	private static bool Visible
	{
		get
		{
			if (!EnDonjon)
			{
				Player me = Globals.Me;
				if (me != null)
				{
					return BossDHistoire(me) != null;
				}
				return false;
			}
			return true;
		}
	}

	internal static int ReservedBottom { get; private set; }

	public DungeonHud(Canvas canvas)
		: base(canvas, "DungeonHud")
	{
		base.MouseInputEnabled = false;
		base.KeyboardInputEnabled = false;
		base.IsHidden = true;
		SetBounds(0, 0, canvas.Width, canvas.Height);
		_leave = new DungeonLeaveButton(canvas);
	}

	private static Entity? BossDHistoire(Player me)
	{
		Entity result = null;
		int num = int.MaxValue;
		foreach (Entity value in Globals.Entities.Values)
		{
			if (value != null && value.BossVisual == BossVisual.Saiyan && !(value.MapId != me.MapId) && value.Vital[0] > 0)
			{
				int num2 = Math.Max(Math.Abs(value.X - me.X), Math.Abs(value.Y - me.Y));
				if (num2 <= 16 && num2 < num)
				{
					num = num2;
					result = value;
				}
			}
		}
		return result;
	}

	internal static bool EstLeBossAffiche(IEntity? entite)
	{
		if (entite != null)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				Entity entity = ((!EnDonjon) ? BossDHistoire(me) : ((Dungeon.Run != null && Dungeon.Current != null) ? Dungeon.NearestBoss(me) : null));
				if (entity != null)
				{
					return entity.Id == entite.Id;
				}
				return false;
			}
		}
		return false;
	}

	public void Update()
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null && (base.Width != parent.Width || base.Height != parent.Height))
		{
			SetBounds(0, 0, parent.Width, parent.Height);
		}
		base.IsHidden = !Visible;
		_leave.IsHidden = base.IsHidden || !EnDonjon;
		if (base.IsHidden || !EnDonjon)
		{
			ReservedBottom = 0;
		}
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.IsHidden)
		{
			return;
		}
		Player me = Globals.Me;
		if (me == null)
		{
			return;
		}
		if (!EnDonjon)
		{
			Entity entity = BossDHistoire(me);
			if (entity != null)
			{
				CharteScreen.Design d = CharteScreen.Design.For(base.Width, base.Height);
				int num = d.Y(18f);
				ImagePanel imagePanel = me.TargetBox?.EntityWindow;
				if (imagePanel != null && !imagePanel.IsHidden)
				{
					num = Math.Max(num, imagePanel.Y + imagePanel.Height - d.L(10f));
				}
				DrawBossBar(skin.Renderer, d, entity, num + d.L(18f));
			}
			return;
		}
		Player me2 = Globals.Me;
		if (me2 == null)
		{
			return;
		}
		DungeonRunPacket run = Dungeon.Run;
		if (run == null)
		{
			return;
		}
		DungeonDescriptor current = Dungeon.Current;
		if (current == null)
		{
			return;
		}
		DungeonRoom currentRoom = Dungeon.CurrentRoom;
		if (currentRoom != null)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			CharteScreen.Design d2 = CharteScreen.Design.For(base.Width, base.Height);
			Rectangle rectangle = DrawBanner(renderer, d2, run, current, currentRoom);
			Rectangle rectangle2 = DrawInfoRow(renderer, d2, run, current, currentRoom, rectangle.Bottom + d2.L(8f), _leave);
			ReservedBottom = rectangle2.Bottom;
			int num2 = rectangle2.Bottom;
			ImagePanel imagePanel2 = me2.TargetBox?.EntityWindow;
			if (imagePanel2 != null && !imagePanel2.IsHidden)
			{
				num2 = Math.Max(num2, imagePanel2.Y + imagePanel2.Height - d2.L(10f));
			}
			int num3 = num2;
			Entity entity2 = Dungeon.NearestBoss(me2);
			if (entity2 != null)
			{
				num3 = DrawBossBar(renderer, d2, entity2, num2 + d2.L(18f));
			}
			Dungeon.Telegraph? telegraph = Dungeon.CurrentAlert(me2);
			if (telegraph.HasValue)
			{
				Dungeon.Telegraph valueOrDefault = telegraph.GetValueOrDefault();
				DrawAlert(renderer, d2, valueOrDefault, num3 + d2.L(34f));
			}
		}
	}

	private static Rectangle DrawBanner(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, DungeonRunPacket run, DungeonDescriptor dungeon, DungeonRoom room)
	{
		int num = Math.Max(0, run.RoomIndex);
		int count = dungeon.Rooms.Count;
		string text = Strings.Dungeons.BannerRoomDifficulty.ToString(dungeon.Name, Dungeon.DifficultyName(run.Difficulty), num + 1, count).ToUpperInvariant();
		string text2 = (string.IsNullOrWhiteSpace(room.Name) ? Strings.Dungeons.RoomDefault.ToString(num + 1) : room.Name).ToUpperInvariant();
		int val = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), text, d.Em(0.2f, 12f));
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(30f), text2);
		int num2 = d.L(16f);
		int num3 = d.L(30f);
		int num4 = count * num2 + Math.Max(0, count - 1) * num3;
		int num5 = Math.Max(Math.Max(val, point.X), num4) + d.L(56f);
		int h = d.L(93f);
		Rectangle rectangle = new Rectangle(d.X(800f) - num5 / 2, d.Y(18f), num5, h);
		TechniqueArt.FillVertical(renderer, rectangle, new Color(230, 6, 8, 14), new Color(184, 6, 8, 14));
		TechniqueArt.Outline(renderer, rectangle, TechniqueArt.Line2);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, d.Pt(12f), text, rectangle.X + rectangle.Width / 2, rectangle.Y + d.L(15.5f), d.Em(0.2f, 12f), TechniqueArt.Alpha(new Color(255, 255, 179, 92), 1f));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(30f), text2, rectangle.X + (rectangle.Width - point.X) / 2, rectangle.Y + d.L(42f) - point.Y / 2, new TechniqueArt.Stops(TechniqueArt.White, 0.35f, new Color(255, 255, 194, 122), 0.8f, TechniqueArt.Ki));
		int num6 = rectangle.X + (rectangle.Width - num4) / 2;
		int num7 = rectangle.Bottom - d.L(12f) - num2 / 2;
		for (int i = 0; i < count; i++)
		{
			int num8 = num6 + i * (num2 + num3);
			if (i > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(num8 - num3, num7 - d.L(1f), num3, Math.Max(1, d.L(2f))), run.Cleared.ElementAtOrDefault(i - 1) ? TechniqueArt.Green : TechniqueArt.Line2);
			}
			bool flag = i == count - 1;
			bool flag2 = run.Cleared.ElementAtOrDefault(i);
			bool flag3 = i == num;
			Color color = ((!flag3) ? (flag2 ? TechniqueArt.Green : new Color(255, 10, 18, 32)) : (flag ? TechniqueArt.Red : TechniqueArt.Gold));
			Color color2 = (flag3 ? color : (flag2 ? TechniqueArt.Green : (flag ? TechniqueArt.Red : TechniqueArt.Line2)));
			if (flag3)
			{
				TechniqueArt.Glow(renderer, (float)num8 + (float)num2 / 2f, num7, (float)num2 * 1.4f, TechniqueArt.Alpha(color, 0.55f));
			}
			Diamond(renderer, num8, num7, num2, color2);
			Diamond(renderer, num8 + d.L(3f), num7, num2 - d.L(6f), color);
		}
		return rectangle;
	}

	internal static void Diamond(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int left, int centerY, int size, Color color)
	{
		if (size <= 1)
		{
			return;
		}
		float num = (float)size / 2f;
		for (int i = 0; i < size; i++)
		{
			float num2 = MathF.Abs((float)i + 0.5f - num);
			int num3 = (int)MathF.Round((num - num2) * 2f);
			if (num3 > 0)
			{
				TechniqueArt.Fill(renderer, new Rectangle(left + (size - num3) / 2, centerY - size / 2 + i, num3, 1), color);
			}
		}
	}

	private static Rectangle DrawInfoRow(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, DungeonRunPacket run, DungeonDescriptor dungeon, DungeonRoom room, int top, DungeonLeaveButton leave)
	{
		int index = Math.Max(0, run.RoomIndex);
		bool flag = run.Cleared.ElementAtOrDefault(index) || run.Finished;
		int h = d.L(44f);
		int num = d.L(8f);
		string text = ((!run.Finished) ? (room.Objective switch
		{
			DungeonObjective.KillAll => Strings.Dungeons.GoalKillAll.ToString(), 
			DungeonObjective.KillNpc => Strings.Dungeons.GoalKillNpc.ToString(room.NpcName), 
			_ => Strings.Dungeons.GoalNone.ToString(), 
		}) : Strings.Dungeons.EndTitle.ToString());
		string text2 = text;
		string text3 = (flag ? Strings.Dungeons.GoalOpen.ToString() : ((room.Objective == DungeonObjective.None) ? string.Empty : $"{Strings.Dungeons.GoalRemaining} · {run.Remaining}"));
		string text4 = Strings.Dungeons.Objective.ToString().ToUpperInvariant();
		int num2 = Math.Max(TechniqueArt.Measure(renderer, TechniqueArt.Heavy, d.Pt(17f), text2).X, TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), text4, d.Em(0.18f, 12f)));
		int num3 = ((!string.IsNullOrEmpty(text3)) ? (TechniqueArt.Measure(renderer, TechniqueArt.Label, d.Pt(13f), text3).X + d.L(18f)) : 0);
		int num4 = d.L(14f) + num2 + num3 + d.L(14f);
		string text5 = Dungeon.Clock(Dungeon.ElapsedMs / 1000);
		int num5 = Math.Max(TechniqueArt.Measure(renderer, TechniqueArt.Heavy, d.Pt(20f), "00:00").X, TechniqueArt.Measure(renderer, TechniqueArt.Heavy, d.Pt(20f), text5).X) + d.L(28f);
		int num6 = d.L(24f);
		int num7 = Math.Clamp(run.BeansMax, 0, 9);
		string text6 = Strings.Dungeons.Beans.ToString().ToUpperInvariant();
		int num8 = Math.Max(TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, d.Pt(12f), text6, d.Em(0.18f, 12f)), num7 * (num6 + d.L(2f))) + d.L(28f);
		int num9 = d.L(118f);
		int num10 = num4 + num + num5 + num + num8 + num + num9;
		int x = d.X(800f) - num10 / 2;
		Rectangle rect = new Rectangle(x, top, num4, h);
		Box(renderer, rect, flag ? TechniqueArt.Alpha(TechniqueArt.Green, 0.55f) : TechniqueArt.Line2);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), text4, rect.X + d.L(14f), rect.Y + d.L(12f), d.Em(0.18f, 12f), TechniqueArt.Gold);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, d.Pt(17f), text2, rect.X + d.L(14f), rect.Y + d.L(30f), TechniqueArt.Text);
		if (!string.IsNullOrEmpty(text3))
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, d.Pt(13f), text3, rect.Right - d.L(14f), rect.Y + d.L(30f), flag ? TechniqueArt.Green : TechniqueArt.Soft);
		}
		Rectangle rect2 = new Rectangle(rect.Right + num, top, num5, h);
		Box(renderer, rect2, TechniqueArt.Line);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), Strings.Dungeons.Time.ToString().ToUpperInvariant(), rect2.X + d.L(14f), rect2.Y + d.L(12f), d.Em(0.18f, 12f), TechniqueArt.Dim);
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, d.Pt(20f), text5, rect2.X + d.L(14f), rect2.Y + d.L(30f), run.Finished ? TechniqueArt.Green : TechniqueArt.Text);
		Rectangle rect3 = new Rectangle(rect2.Right + num, top, num8, h);
		Box(renderer, rect3, TechniqueArt.Line);
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, d.Pt(12f), text6, rect3.X + d.L(14f), rect3.Y + d.L(12f), d.Em(0.18f, 12f), TechniqueArt.Dim);
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, "haricot_magique.png");
		for (int i = 0; i < num7; i++)
		{
			Color color = ((i < run.BeansLeft) ? TechniqueArt.White : new Color(255, 70, 76, 90));
			SaiyanHud.Draw(renderer, texture, rect3.X + d.L(12f) + i * (num6 + d.L(2f)), rect3.Y + d.L(19f), num6, num6, color);
		}
		leave.Place(new Rectangle(rect3.Right + num, top, num9, h), run.Finished);
		return new Rectangle(x, top, num10, h);
	}

	private static void Box(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, Color border)
	{
		TechniqueArt.Fill(renderer, rect, new Color(214, 6, 8, 14));
		TechniqueArt.Outline(renderer, rect, border);
	}

	private static int DrawBossBar(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, Entity boss, int top)
	{
		long num = Math.Max(1L, boss.MaxVital[0]);
		float num2 = Math.Clamp((float)boss.Vital[0] / (float)num, 0f, 1f);
		bool flag = num2 * 100f <= (float)boss.BossEnragePercent;
		int num3 = d.L(860f);
		int num4 = d.X(800f) - num3 / 2;
		string text = boss.Name.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(28f), text);
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(28f), text, num4, top, new TechniqueArt.Stops(new Color(255, 255, 230, 234), 0.4f, new Color(255, 255, 196, 204), 1f, new Color(255, 255, 150, 165)));
		if (flag)
		{
			string text2 = Strings.Dungeons.BossEnraged.ToString().ToUpperInvariant();
			int num5 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, d.Pt(14f), text2, d.Em(0.12f, 14f)) + d.L(20f);
			Rectangle rect = new Rectangle(num4 + num3 - num5, top + point.Y - d.L(24f), num5, d.L(24f));
			float num6 = 0.5f + 0.5f * MathF.Sin((float)Timing.Global.MillisecondsUtc / 160f);
			TechniqueArt.GlowRect(renderer, rect, TechniqueArt.Alpha(TechniqueArt.Red, 0.35f + 0.35f * num6));
			TechniqueArt.Fill(renderer, rect, new Color(255, 200, 40, 63));
			TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, d.Pt(14f), text2, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, d.Em(0.12f, 14f), TechniqueArt.White);
		}
		int y = top + point.Y + d.L(6f);
		Rectangle rect2 = new Rectangle(num4, y, num3, d.L(20f));
		TechniqueArt.Fill(renderer, rect2, new Color(230, 6, 8, 14));
		TechniqueArt.Outline(renderer, rect2, TechniqueArt.Alpha(TechniqueArt.Red, 0.5f));
		Rectangle rect3 = new Rectangle(rect2.X + d.L(2f), rect2.Y + d.L(2f), (int)((float)(rect2.Width - d.L(4f)) * num2), rect2.Height - d.L(4f));
		TechniqueArt.FillVertical(renderer, rect3, new Color(255, 255, 138, 149), new Color(255, 194, 31, 58));
		int bossEnragePercent = boss.BossEnragePercent;
		bool flag2 = bossEnragePercent > 0 && bossEnragePercent < 100;
		if (flag2)
		{
			int num7 = rect2.X + (int)((float)(rect2.Width * Math.Clamp(boss.BossEnragePercent, 0, 100)) / 100f);
			TechniqueArt.Fill(renderer, new Rectangle(num7 - d.L(1f), rect2.Y - d.L(5f), Math.Max(1, d.L(2f)), rect2.Height + d.L(10f)), TechniqueArt.Gold);
		}
		int num8 = rect2.Bottom + d.L(12f);
		if (flag2)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, d.Pt(12f), Strings.Dungeons.BossEnrageAt.ToString(boss.BossEnragePercent), num4, num8, TechniqueArt.Soft);
		}
		TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, d.Pt(14f), $"{(int)MathF.Ceiling(num2 * 100f)} %", rect2.Right, num8, TechniqueArt.Text);
		return num8 + d.L(8f);
	}

	private static void DrawAlert(Intersect.Client.Framework.Gwen.Renderer.Base renderer, CharteScreen.Design d, Dungeon.Telegraph alert, int top)
	{
		(string, string) tuple;
		switch ((BossAbilityKind)alert.Packet.Kind)
		{
		case BossAbilityKind.Charge:
			tuple = (Strings.Dungeons.AlertCharge.ToString(), Strings.Dungeons.AlertChargeDetail.ToString());
			break;
		case BossAbilityKind.RockFall:
			tuple = (Strings.Dungeons.AlertRocks.ToString(), Strings.Dungeons.AlertRocksDetail.ToString());
			break;
		case BossAbilityKind.EyeBeam:
		{
			DungeonTelegraphShape[] shapes = alert.Packet.Shapes;
			tuple = ((shapes == null || shapes.Length <= 1) ? (Strings.Dungeons.AlertEyeBeam.ToString(), Strings.Dungeons.AlertEyeBeamDetail.ToString()) : (Strings.Dungeons.AlertSweep.ToString(), Strings.Dungeons.AlertSweepDetail.ToString()));
			break;
		}
		case BossAbilityKind.Fissures:
			tuple = (Strings.Dungeons.AlertFissures.ToString(), Strings.Dungeons.AlertFissuresDetail.ToString());
			break;
		case BossAbilityKind.GoldRain:
			tuple = (Strings.Dungeons.AlertGoldRain.ToString(), Strings.Dungeons.AlertGoldRainDetail.ToString());
			break;
		default:
			tuple = (Strings.Dungeons.AlertShockwave.ToString(), Strings.Dungeons.AlertShockwaveDetail.ToString());
			break;
		}
		(string, string) tuple2 = tuple;
		string item = tuple2.Item1;
		string item2 = tuple2.Item2;
		string text = item.ToUpperInvariant();
		Point point = TechniqueArt.Measure(renderer, TechniqueArt.Display, d.Pt(46f), text);
		int num = d.X(800f);
		TechniqueArt.Glow(renderer, num, (float)top + (float)point.Y / 2f, (float)point.X * 0.7f, TechniqueArt.Alpha(TechniqueArt.Red, 0.35f));
		TechniqueArt.Styled(renderer, TechniqueArt.Display, d.Pt(46f), text, num - point.X / 2, top, new TechniqueArt.Stops(TechniqueArt.White, 0.5f, new Color(255, 255, 214, 219), 1f, new Color(255, 255, 160, 170)));
		TechniqueArt.TextCenter(renderer, TechniqueArt.Label, d.Pt(15f), item2, num, top + point.Y + d.L(14f), new Color(255, 255, 208, 213));
	}
}
