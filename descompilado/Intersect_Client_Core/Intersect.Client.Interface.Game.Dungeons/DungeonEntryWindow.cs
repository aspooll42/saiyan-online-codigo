using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Intersect.Client.Dungeons;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Game.Housing;
using Intersect.Client.Interface.Game.Techniques;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework.Core;
using Intersect.Framework.Core.GameObjects.Dungeons;
using Intersect.Framework.Core.GameObjects.Items;
using Intersect.Framework.Core.GameObjects.Titles;
using Intersect.Localization;
using Intersect.Models;

namespace Intersect.Client.Interface.Game.Dungeons;

internal sealed class DungeonEntryWindow : CharteScreen
{
	private const float Left = 760f;

	private const float Top = 52f;

	private const float CardWidth = 780f;

	private const float CardHeight = 797f;

	private const int RouteCards = 4;

	private const long RefuseMs = 350L;

	private static readonly Color KiLight = new Color(255, 255, 179, 92);

	private static readonly Color BodyLight = new Color(255, 196, 204, 220);

	private long _pendingUntil;

	private DungeonDifficulty _difficulty;

	private int _refused = -1;

	private long _refusedAt;

	private int _viewed = -1;

	public override bool IsBlockingInput => !base.IsHidden;

	public Guid DungeonId { get; private set; }

	private DungeonDescriptor? Descriptor => DatabaseObject<DungeonDescriptor>.Get(DungeonId);

	private static string? OtherLeader
	{
		get
		{
			Player me = Globals.Me;
			if (me == null || me.Party.Count <= 0 || !(me.Party[0].Id != me.Id))
			{
				return null;
			}
			return me.Party[0].Name;
		}
	}

	private bool CanEnter
	{
		get
		{
			if (Descriptor != null && OtherLeader == null && Unlocked(_difficulty) && Timing.Global.MillisecondsUtc >= _pendingUntil)
			{
				return !Dungeon.InRun;
			}
			return false;
		}
	}

	public DungeonEntryWindow(Canvas gameCanvas)
		: base(gameCanvas, "DungeonEntryWindow")
	{
		Interface.InputBlockingComponents.Add(this);
	}

	public void Open(DungeonDescriptor dungeon)
	{
		DungeonId = dungeon.Id;
		_pendingUntil = 0L;
		_refused = -1;
		_viewed = -1;
		_difficulty = Dungeon.HighestUnlocked(dungeon.Id);
		Show();
	}

	private bool Unlocked(DungeonDifficulty difficulty)
	{
		return DungeonRules.IsUnlocked(Dungeon.StatusOf(DungeonId)?.ClearedMask ?? 0, difficulty);
	}

	public bool TryConfirm()
	{
		if (base.IsHidden)
		{
			return false;
		}
		Enter();
		return true;
	}

	private void Enter()
	{
		if (CanEnter)
		{
			_pendingUntil = Timing.Global.MillisecondsUtc + 2000;
			PacketSender.SendDungeonEnter(DungeonId, _difficulty);
		}
	}

	private void Choose(int index)
	{
		DungeonDifficulty difficulty = (DungeonDifficulty)index;
		if (!Unlocked(difficulty))
		{
			_refused = index;
			_refusedAt = Timing.Global.MillisecondsUtc;
		}
		else
		{
			_difficulty = difficulty;
			_viewed = -1;
		}
	}

	protected override void DrawScreen(Intersect.Client.Framework.Gwen.Renderer.Base renderer, long now)
	{
		DungeonDescriptor descriptor = Descriptor;
		if (descriptor == null)
		{
			return;
		}
		Player me = Globals.Me;
		if (me != null)
		{
			if (!Unlocked(_difficulty))
			{
				_difficulty = Dungeon.HighestUnlocked(descriptor.Id);
			}
			Rectangle rect = new Rectangle(0, 0, base.Width, base.Height);
			TechniqueArt.Fill(renderer, rect, A(new Color(255, 4, 6, 11), 0.55f));
			TechniqueArt.Image(renderer, "tech_voile.png", rect, A(TechniqueArt.White));
			AddHit(rect, "voile", delegate
			{
			});
			Rectangle rectangle = _d.R(760f, 52f, 780f, 797f);
			HouseArt.Window(renderer, rectangle, _fade);
			AddHit(rectangle, "carte", delegate
			{
			});
			string detail = (string.IsNullOrWhiteSpace(descriptor.Zone) ? Strings.Dungeons.CardDetailNoZone.ToString() : Strings.Dungeons.CardDetail.ToString(descriptor.Zone));
			HouseArt.WindowHeader(renderer, _d, rectangle, descriptor.Name, detail, _fade);
			DungeonStatusInfo status = Dungeon.StatusOf(descriptor.Id);
			DrawHero(renderer, descriptor, status, 120f);
			DrawDifficulties(renderer, descriptor, status, me, 191f);
			DrawRoute(renderer, descriptor, 329f);
			DrawGroup(renderer, descriptor, me, 780f, 483f);
			DrawChest(renderer, descriptor, 1156f, 483f);
			DrawAchievements(renderer, descriptor, status, me, 714f);
			float y = 785f;
			string text = Strings.Dungeons.Later.ToString();
			float num = (float)TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(14f), text).X / _d.S + 40f;
			Rectangle rect2 = _d.R(780f, y, num, 48f);
			HouseArt.Hollow(renderer, _d, rect2, text, IsHot("plus-tard"), _fade);
			AddHit(rect2, "plus-tard", Hide);
			Rectangle rect3 = _d.R(780f + num + 10f, y, 740f - num - 10f, 48f);
			LocalizedString localizedString = ((me.Party.Count > 1) ? Strings.Dungeons.EnterGroup : Strings.Dungeons.EnterSolo);
			DrawEnter(renderer, rect3, localizedString, Strings.Dungeons.EnterIn.ToString(Dungeon.DifficultyName(_difficulty)), CanEnter, IsHot("entrer"));
			AddHit(rect3, "entrer", Enter);
		}
	}

	private void DrawEnter(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle rect, string label, string sub, bool enabled, bool hot)
	{
		Color color = new Color(255, 20, 13, 2);
		if (!enabled)
		{
			TechniqueArt.Patch(renderer, "tech_bouton_eteint.png", rect, A(TechniqueArt.White), 14);
		}
		else if (_difficulty == DungeonDifficulty.Hard)
		{
			TechniqueArt.Patch(renderer, hot ? "tech_bouton_survol.png" : "tech_bouton.png", rect, A(TechniqueArt.White), 14);
		}
		else
		{
			Color color4;
			Color color5;
			if (_difficulty != DungeonDifficulty.Heroic)
			{
				Color color2 = new Color(255, 200, 247, 220);
				Color color3 = new Color(255, 52, 184, 119);
				color4 = color3;
				color5 = color2;
			}
			else
			{
				Color color6 = new Color(255, 230, 210, 255);
				Color color3 = new Color(255, 138, 92, 216);
				color4 = color3;
				color5 = color6;
			}
			TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, A(color4), 12);
			TechniqueArt.Patch(renderer, "tech_chanfrein10_degrade.png", rect, A(color5), 12);
			if (hot)
			{
				TechniqueArt.Patch(renderer, "tech_chanfrein10.png", rect, A(TechniqueArt.White, 0.1f), 12);
			}
		}
		Color color7 = (enabled ? color : TechniqueArt.Dim);
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(20f), label.ToUpperInvariant(), rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - _d.L(6f), _d.Em(0.06f, 20f), A(color7));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), sub.ToUpperInvariant(), rect.X + rect.Width / 2, rect.Y + rect.Height / 2 + _d.L(12f), _d.Em(0.12f, 12f), A(color7, enabled ? 0.72f : 1f));
	}

	private void DrawHero(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, DungeonStatusInfo? status, float top)
	{
		Rectangle rect = _d.R(780f, top, 740f, 60f);
		IGameTexture gameTexture = (string.IsNullOrWhiteSpace(dungeon.Image) ? null : GameContentManager.Current.GetTexture(TextureType.Image, dungeon.Image));
		if (gameTexture != null && gameTexture.Width > 0 && gameTexture.Height > 0)
		{
			float num = Math.Max((float)rect.Width / (float)gameTexture.Width, (float)rect.Height / (float)gameTexture.Height);
			float num2 = (float)rect.Width / num;
			float num3 = (float)rect.Height / num;
			float num4 = ((float)gameTexture.Width - num2) / 2f / (float)gameTexture.Width;
			float num5 = ((float)gameTexture.Height - num3) / 2f / (float)gameTexture.Height;
			SaiyanHud.Draw(renderer, gameTexture, rect.X, rect.Y, rect.Width, rect.Height, A(TechniqueArt.White), num4, num5, 1f - num4, 1f - num5);
		}
		else
		{
			TechniqueArt.FillHorizontal(renderer, rect, A(new Color(255, 40, 22, 12)), A(new Color(255, 22, 16, 18)), A(new Color(255, 10, 14, 24)));
		}
		TechniqueArt.FillHorizontal(renderer, rect, A(new Color(209, 4, 6, 11)), A(new Color(120, 4, 6, 11)), A(new Color(51, 4, 6, 11)));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line2));
		List<(string, bool)> list = new List<(string, bool)>();
		list.Add((Strings.Dungeons.TagLevel.ToString(dungeon.Settings(DungeonDifficulty.Normal).LevelMin, dungeon.Settings(DungeonDifficulty.Heroic).LevelMax), true));
		list.Add((Strings.Dungeons.TagPlayers.ToString(4), false));
		list.Add((Strings.Dungeons.TagRooms.ToString(dungeon.Rooms.Count), false));
		List<(string, bool)> list2 = list;
		int x = rect.X + _d.L(14f);
		int num6 = _d.L(26f);
		int y = rect.Bottom - _d.L(10f) - num6;
		foreach (var item3 in list2)
		{
			string item = item3.Item1;
			bool item2 = item3.Item2;
			string text = (item2 ? item.ToUpperInvariant() : item);
			int w = (item2 ? TechniqueArt.SpacedWidth(renderer, TechniqueArt.Heavy, Pt(15f), text, _d.Em(0.1f, 15f)) : TechniqueArt.Measure(renderer, TechniqueArt.Label, Pt(13f), text).X) + _d.L(20f);
			Rectangle rect2 = new Rectangle(x, y, w, num6);
			if (item2)
			{
				TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.Gold));
				TechniqueArt.SpacedCenter(renderer, TechniqueArt.Heavy, Pt(15f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.1f, 15f), A(HouseArt.Dark));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect2, A(new Color(217, 6, 8, 14)));
				TechniqueArt.Outline(renderer, rect2, A(TechniqueArt.Line2));
				TechniqueArt.TextCenter(renderer, TechniqueArt.Label, Pt(13f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, A(TechniqueArt.Text));
			}
			x = rect2.Right + _d.L(6f);
		}
	}

	private void SectionTitle(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string title, string? note, float centerY)
	{
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), title.ToUpperInvariant(), _d.X(780f), _d.Y(centerY), _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		if (!string.IsNullOrEmpty(note))
		{
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), note, _d.X(1520f), _d.Y(centerY), A(TechniqueArt.Soft));
		}
	}

	private void DrawDifficulties(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, DungeonStatusInfo? status, Player me, float top)
	{
		SectionTitle(renderer, Strings.Dungeons.Difficulty, Strings.Dungeons.DifficultyReminder, top + 6f);
		byte b = status?.ClearedMask ?? 0;
		int players = Math.Clamp((me.Party.Count <= 0) ? 1 : me.Party.Count((IPartyMember member) => IsNear(dungeon, member.Id)), 1, 4);
		float num = 8f;
		float num2 = (740f - num * 2f) / 3f;
		long millisecondsUtc = Timing.Global.MillisecondsUtc;
		IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, "haricot_magique.png");
		for (int num3 = 0; num3 < 3; num3++)
		{
			DungeonDifficulty dungeonDifficulty = (DungeonDifficulty)num3;
			DungeonDifficultySettings dungeonDifficultySettings = dungeon.Settings(dungeonDifficulty);
			bool flag = DungeonRules.IsUnlocked(b, dungeonDifficulty);
			bool flag2 = (b & DungeonRules.Bit(dungeonDifficulty)) != 0;
			bool flag3 = dungeonDifficulty == _difficulty;
			bool flag4 = IsHot($"diff-{num3}");
			Color color = Dungeon.DifficultyColor(dungeonDifficulty);
			float num4 = 0f;
			if (_refused == num3 && millisecondsUtc - _refusedAt < 350)
			{
				float num5 = (float)(millisecondsUtc - _refusedAt) / 350f;
				num4 = MathF.Sin(num5 * (float)Math.PI * 2f) * -4f;
			}
			Rectangle rect = _d.R(780f + (float)num3 * (num2 + num) + num4, top + 19f, num2, 108f);
			if (flag3)
			{
				TechniqueArt.GlowRect(renderer, rect, A(color, 0.14f));
				TechniqueArt.FillVertical(renderer, rect, A(color, 0.16f), A(TechniqueArt.White, 0.02f));
				TechniqueArt.Fill(renderer, rect, A(new Color(255, 12, 17, 28), 0.35f));
				TechniqueArt.FillVertical(renderer, rect, A(color, 0.14f), A(TechniqueArt.White, 0.01f));
				TechniqueArt.Outline(renderer, rect, A(color));
				TechniqueArt.Outline(renderer, new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), A(color, 0.45f));
				TechniqueArt.Fill(renderer, new Rectangle(rect.X, rect.Y, _d.L(3f), rect.Height), A(color));
				TechniqueArt.Glow(renderer, rect.X + _d.L(2f), (float)rect.Y + (float)rect.Height / 2f, _d.L(14f), A(color, 0.35f));
			}
			else
			{
				TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, (flag && flag4) ? 0.05f : 0.03f));
				TechniqueArt.Outline(renderer, rect, A((flag && flag4) ? TechniqueArt.Line2 : TechniqueArt.Line));
			}
			float num6 = (flag ? 1f : 0.38f);
			int num7 = rect.X + _d.L(14f);
			int centerY = rect.Y + _d.L(21f);
			int num8 = _d.L(12f);
			for (int num9 = 0; num9 < 3; num9++)
			{
				int num10 = num7 + num9 * (num8 + _d.L(2f));
				DungeonHud.Diamond(renderer, num10, centerY, num8, A(color, num6 * 0.9f));
				if (num9 > num3)
				{
					DungeonHud.Diamond(renderer, num10 + _d.L(2f), centerY, num8 - _d.L(4f), A(new Color(255, 14, 20, 33), num6));
				}
			}
			TechniqueArt.Spaced(renderer, TechniqueArt.Heavy, Pt(21f), Dungeon.DifficultyName(dungeonDifficulty).ToUpperInvariant(), num7 + 3 * (num8 + _d.L(2f)) + _d.L(6f), centerY, _d.Em(0.07f, 21f), A(color, num6));
			if (!flag)
			{
				HouseArt.Padlock(renderer, new Rectangle(rect.Right - _d.L(28f), rect.Y + _d.L(12f), _d.L(16f), _d.L(18f)), A(TechniqueArt.Soft));
			}
			else if (flag2)
			{
				string text = Strings.Dungeons.TileBadge.ToString().ToUpperInvariant();
				int num11 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.1f, 12f)) + _d.L(12f);
				Rectangle rect2 = new Rectangle(rect.Right - _d.L(10f) - num11, rect.Y + _d.L(10f), num11, _d.L(17f));
				TechniqueArt.Fill(renderer, rect2, A(TechniqueArt.Green));
				TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.1f, 12f), A(new Color(255, 6, 20, 12)));
			}
			int centerY2 = rect.Y + _d.L(46f);
			string text2 = Strings.Dungeons.TileLevel.ToString().ToUpperInvariant();
			int num12 = TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text2, num7, centerY2, _d.Em(0.12f, 12f), A(TechniqueArt.Soft, num6));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), $"{dungeonDifficultySettings.LevelMin} – {dungeonDifficultySettings.LevelMax}", num7 + num12 + _d.L(5f), centerY2, A(TechniqueArt.Text, num6));
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), Strings.Dungeons.TileExperience.ToString(Dungeon.Multiplier(dungeonDifficultySettings.ExperiencePercent)), rect.Right - _d.L(12f), centerY2, A(color, num6));
			int num13 = rect.Y + _d.L(67f);
			int num14 = DungeonRules.HealthPercent(dungeon, players, dungeonDifficulty);
			int num15 = num7;
			num15 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Dungeons.TileHealth.ToString(num14), num15, num13, A(TechniqueArt.Soft, num6)) + _d.L(10f);
			num15 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Dungeons.TileDamage.ToString(dungeonDifficultySettings.DamagePercent), num15, num13, A(TechniqueArt.Soft, num6)) + _d.L(10f);
			SaiyanHud.Draw(renderer, texture, num15, num13 - _d.L(9f), _d.L(18f), _d.L(18f), A(TechniqueArt.White, num6));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), $"×{Math.Clamp(dungeonDifficultySettings.Beans, 0, 9)}", num15 + _d.L(20f), num13, A(TechniqueArt.Soft, num6));
			int num16 = status?.BestFor(dungeonDifficulty) ?? 0;
			string text3 = ((!flag) ? Strings.Dungeons.TileLocked.ToString(Dungeon.DifficultyName(dungeonDifficulty - 1)) : (flag2 ? ((num16 > 0) ? Strings.Dungeons.TileCleared.ToString(Dungeon.Clock(num16)) : string.Empty) : ((dungeonDifficulty == DungeonDifficulty.Normal) ? Strings.Dungeons.TileDiscover.ToString() : Strings.Dungeons.TileNotYet.ToString())));
			string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text3, rect.Width - _d.L(26f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text4, num7, rect.Y + _d.L(89f), A(flag ? TechniqueArt.Soft : KiLight));
			int choice = num3;
			AddHit(rect, $"diff-{num3}", delegate
			{
				Choose(choice);
			});
		}
	}

	private void DrawRoute(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, float top)
	{
		List<DungeonRoom> rooms = dungeon.Rooms;
		SectionTitle(renderer, Strings.Dungeons.Route, (rooms.Count > 4) ? Strings.Dungeons.MoreRooms.ToString(rooms.Count - 4) : null, top + 6f);
		int num = Math.Min(4, rooms.Count);
		float num2 = 8f;
		float num3 = (740f - num2 * 3f) / 4f;
		Color color = Dungeon.DifficultyColor(_difficulty);
		for (int i = 0; i < num; i++)
		{
			int num4 = ((rooms.Count > 4 && i == num - 1) ? (rooms.Count - 1) : i);
			DungeonRoom dungeonRoom = rooms[num4];
			bool flag = num4 == rooms.Count - 1;
			Rectangle rectangle = _d.R(780f + (float)i * (num3 + num2), top + 19f, num3, 124f);
			if (flag)
			{
				TechniqueArt.FillVertical(renderer, rectangle, A(new Color(31, 255, 85, 102)), A(new Color(5, 255, 255, 255)));
				TechniqueArt.Outline(renderer, rectangle, A(new Color(140, 255, 85, 102)));
			}
			else
			{
				TechniqueArt.Fill(renderer, rectangle, A(TechniqueArt.White, 0.03f));
				TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Line));
			}
			Rectangle previous = TechniqueArt.BeginClip(renderer, rectangle);
			TechniqueArt.Styled(renderer, TechniqueArt.Display, Pt(28f), $"{num4 + 1}", rectangle.Right - _d.L(28f), rectangle.Y + _d.L(4f), TechniqueArt.Stops.Solid(new Color(20, 255, 255, 255)), _fade);
			Rectangle box = new Rectangle(rectangle.X + _d.L(10f), rectangle.Y + _d.L(10f), rectangle.Width - _d.L(20f), _d.L(44f));
			DungeonStatusInfo dungeonStatusInfo = Dungeon.StatusOf(dungeon.Id);
			string text = ((dungeonRoom.Objective == DungeonObjective.KillNpc && !string.IsNullOrWhiteSpace(dungeonRoom.NpcSprite)) ? dungeonRoom.NpcSprite : dungeonStatusInfo?.RoomSprites.ElementAtOrDefault(num4));
			string text2 = dungeonStatusInfo?.RoomBosses.ElementAtOrDefault(num4);
			if (!string.IsNullOrWhiteSpace(text))
			{
				DrawSpriteStrip(renderer, text, box);
			}
			else
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Display, Pt(24f), (dungeonRoom.Objective == DungeonObjective.None) ? "»" : "×", box.X + _d.L(4f), box.Y + box.Height / 2, A(TechniqueArt.Dim));
			}
			string text3 = (string.IsNullOrWhiteSpace(dungeonRoom.Name) ? Strings.Dungeons.RoomDefault.ToString(num4 + 1) : dungeonRoom.Name);
			int x = rectangle.X + _d.L(10f);
			string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(17f), text3, rectangle.Width - _d.L(20f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(17f), text4, x, box.Bottom + _d.L(12f), A(flag ? new Color(255, 255, 194, 201) : TechniqueArt.Text));
			string text5 = dungeonRoom.Objective switch
			{
				DungeonObjective.KillAll => string.IsNullOrWhiteSpace(text2) ? Strings.Dungeons.ObjectiveKillAll.ToString() : text2, 
				DungeonObjective.KillNpc => Strings.Dungeons.ObjectiveKillNpc.ToString(dungeonRoom.NpcName), 
				_ => Strings.Dungeons.ObjectiveNone.ToString(), 
			};
			string text6 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text5, rectangle.Width - _d.L(20f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text6, x, box.Bottom + _d.L(30f), A(TechniqueArt.Soft));
			string text7 = Dungeon.RoomModifier(dungeon, num4, _difficulty);
			if (!string.IsNullOrEmpty(text7))
			{
				List<string> list = TechniqueArt.Wrap(renderer, TechniqueArt.Label, Pt(12f), text7, rectangle.Width - _d.L(20f), 2);
				int num5 = rectangle.Bottom - _d.L(14f) - _d.L(13f);
				foreach (string item in list)
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), item, x, num5, A(color));
					num5 += _d.L(13f);
				}
			}
			TechniqueArt.EndClip(renderer, previous);
		}
		if (rooms.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(13f), Strings.Dungeons.ResultInvalid, _d.X(780f), _d.Y(top + 80f), A(TechniqueArt.Red));
		}
	}

	internal static (float Width, float Height) FrameSize(IGameTexture texture)
	{
		float num = (float)texture.Width / (float)Math.Max(1, Options.Instance.Sprites.NormalFrames);
		float num2 = (float)texture.Height / (float)Math.Max(1, Options.Instance.Sprites.Directions);
		if (!(num2 > num * 2f))
		{
			return (Width: num, Height: num2);
		}
		return (Width: num, Height: num);
	}

	internal static (IGameTexture Texture, float X, float Y, float Width, float Height)? StandingFrame(string? sprite)
	{
		if (!string.IsNullOrWhiteSpace(sprite))
		{
			IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Entity, sprite);
			if (texture != null && texture.Width > 0 && texture.Height > 0)
			{
				if (SpriteSheets.ForEntity(sprite) != null && SpriteSheets.TryPortraitFrame(sprite, texture, out (int, int, int, int) frame))
				{
					return (texture, frame.Item1, frame.Item2, frame.Item3, frame.Item4);
				}
				string name = Path.GetFileNameWithoutExtension(sprite) + "_idle" + Path.GetExtension(sprite);
				IGameTexture texture2 = GameContentManager.Current.GetTexture(TextureType.Entity, name);
				if (texture2 != null && texture2.Width > 0 && texture2.Height > 0)
				{
					int num = Math.Max(1, Entity.SheetColumns(texture2, texture, Options.Instance.Sprites.IdleFrames));
					return (texture2, 0f, 0f, (float)texture2.Width / (float)num, (float)texture2.Height / (float)Math.Max(1, Options.Instance.Sprites.Directions));
				}
				var (item, item2) = FrameSize(texture);
				return (texture, 0f, 0f, item, item2);
			}
		}
		return null;
	}

	private void DrawSpriteStrip(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string sprite, Rectangle box)
	{
		(IGameTexture, float, float, float, float)? tuple = StandingFrame(sprite);
		if (tuple.HasValue)
		{
			var (gameTexture, num, num2, num3, num4) = tuple.GetValueOrDefault();
			if (!(num3 < 1f) && !(num4 < 1f))
			{
				float num5 = (float)_d.L(62f) / num3;
				int num6 = (int)MathF.Round(num3 * num5);
				int height = (int)MathF.Round(num4 * num5);
				int columns = Math.Max(1, (int)MathF.Round((float)gameTexture.Width / num3));
				int rows = Math.Max(1, (int)MathF.Round((float)gameTexture.Height / num4));
				float num7 = (float)SpriteMeasure.TopMargin(gameTexture, columns, rows) * num5;
				Rectangle previous = TechniqueArt.BeginClip(renderer, box);
				SaiyanHud.Draw(renderer, gameTexture, box.X + (box.Width - num6) / 2, (int)MathF.Round((float)(box.Y + _d.L(3f)) - num7), num6, height, A(TechniqueArt.White), num / (float)gameTexture.Width, num2 / (float)gameTexture.Height, (num + num3) / (float)gameTexture.Width, (num2 + num4) / (float)gameTexture.Height);
				TechniqueArt.EndClip(renderer, previous);
			}
		}
	}

	private void DrawSpriteFace(Intersect.Client.Framework.Gwen.Renderer.Base renderer, string sprite, Rectangle box)
	{
		(IGameTexture, float, float, float, float)? tuple = StandingFrame(sprite);
		if (tuple.HasValue)
		{
			var (gameTexture, num, num2, num3, num4) = tuple.GetValueOrDefault();
			if (!(num3 < 1f) && !(num4 < 1f))
			{
				int num5 = Math.Max(1, (int)Math.Floor((float)box.Width / num3));
				int num6 = (int)(num3 * (float)num5);
				int height = (int)(num4 * (float)num5);
				int columns = Math.Max(1, (int)MathF.Round((float)gameTexture.Width / num3));
				int rows = Math.Max(1, (int)MathF.Round((float)gameTexture.Height / num4));
				int num7 = SpriteMeasure.TopMargin(gameTexture, columns, rows) * num5;
				Rectangle previous = TechniqueArt.BeginClip(renderer, box);
				SaiyanHud.Draw(renderer, gameTexture, box.X + (box.Width - num6) / 2, (int)MathF.Round(box.Y + _d.L(1f) - num7), num6, height, A(TechniqueArt.White), num / (float)gameTexture.Width, num2 / (float)gameTexture.Height, (num + num3) / (float)gameTexture.Width, (num2 + num4) / (float)gameTexture.Height);
				TechniqueArt.EndClip(renderer, previous);
			}
		}
	}

	private void DrawGroup(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, Player me, float left, float top)
	{
		Rectangle rect = _d.R(left, top, 364f, 220f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		List<(Guid Id, string Name, int Level)> list;
		if (me.Party.Count <= 0)
		{
			int num = 1;
			list = new List<(Guid, string, int)>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<(Guid, string, int)> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			span[num2] = (me.Id, me.Name, me.Level);
			num2++;
		}
		else
		{
			list = me.Party.Select((IPartyMember member) => (Id: member.Id, Name: member.Name, Level: member.Level)).ToList();
		}
		List<(Guid, string, int)> list2 = list;
		int num3 = list2.Count<(Guid, string, int)>(((Guid Id, string Name, int Level) member) => IsNear(dungeon, member.Id));
		string text = ((list2.Count > 1) ? Strings.Dungeons.Group.ToString(Math.Min(num3, 4), 4) : Strings.Dungeons.GroupSolo.ToString());
		TechniqueArt.Spaced(renderer, TechniqueArt.Label, Pt(12f), text.ToUpperInvariant(), rect.X + _d.L(14f), rect.Y + _d.L(16f), _d.Em(0.18f, 12f), A(TechniqueArt.Dim));
		int num4 = rect.Y + _d.L(28f);
		int num5 = _d.L(29f);
		for (int num6 = 0; num6 < 4; num6++)
		{
			int num7 = num4 + num6 * num5 + num5 / 2;
			if (num6 < 3)
			{
				TechniqueArt.Fill(renderer, new Rectangle(rect.X + _d.L(14f), num4 + (num6 + 1) * num5, rect.Width - _d.L(28f), 1), A(TechniqueArt.White, 0.05f));
			}
			Rectangle rectangle = new Rectangle(rect.X + _d.L(14f), num7 - _d.L(13f), _d.L(26f), _d.L(26f));
			TechniqueArt.Fill(renderer, rectangle, A(new Color(255, 10, 18, 32)));
			TechniqueArt.Outline(renderer, rectangle, A(TechniqueArt.Line));
			int x = rectangle.Right + _d.L(10f);
			if (num6 >= list2.Count)
			{
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), Strings.Dungeons.FreeSlot, x, num7 - _d.L(6f), A(TechniqueArt.Soft));
				TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Dungeons.FreeSlotDetail, x, num7 + _d.L(7f), A(TechniqueArt.Dim));
				TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), "—", rect.Right - _d.L(14f), num7, A(TechniqueArt.Dim));
				continue;
			}
			var (guid, text2, num8) = list2[num6];
			if (Globals.Entities.TryGetValue(guid, out Entity value))
			{
				DrawSpriteFace(renderer, value.Sprite, rectangle);
			}
			bool flag = num6 == 0 && list2.Count > 1;
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(14f), text2, x, num7 - _d.L(6f), A(TechniqueArt.Text));
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), flag ? Strings.Dungeons.MemberLeader.ToString(num8) : Strings.Dungeons.MemberLevel.ToString(num8), x, num7 + _d.L(7f), A(TechniqueArt.Dim));
			bool flag2 = IsNear(dungeon, guid);
			TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), (flag2 ? Strings.Dungeons.MemberReady : Strings.Dungeons.MemberFar).ToString().ToUpperInvariant(), rect.Right - _d.L(14f), num7, A(flag2 ? TechniqueArt.Green : TechniqueArt.Gold));
		}
		int num9 = Math.Clamp(num3, 1, 4);
		DungeonDifficultySettings dungeonDifficultySettings = dungeon.Settings(_difficulty);
		int num10 = DungeonRules.HealthPercent(dungeon, num9, _difficulty);
		string text3 = Dungeon.DifficultyName(_difficulty);
		string text4 = Dungeon.Multiplier(dungeonDifficultySettings.ExperiencePercent);
		string text5 = ((num9 <= 1) ? Strings.Dungeons.ScaleSoloDifficulty.ToString(text3, num10, dungeonDifficultySettings.DamagePercent, text4) : Strings.Dungeons.ScaleGroupDifficulty.ToString(num9, text3, num10, dungeonDifficultySettings.DamagePercent, text4));
		int num11 = num4 + num5 * 4 + _d.L(11f);
		foreach (string item in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text5, rect.Width - _d.L(28f), 2))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item, rect.X + _d.L(14f), num11, A(TechniqueArt.Soft));
			num11 += _d.L(15f);
		}
		string text6 = null;
		Color color = Dungeon.Violet;
		string otherLeader = OtherLeader;
		if (otherLeader != null)
		{
			text6 = Strings.Dungeons.NotLeaderNote.ToString(otherLeader);
			color = TechniqueArt.Red;
		}
		else if (num3 < list2.Count)
		{
			text6 = Strings.Dungeons.FarNote.ToString();
			color = TechniqueArt.Gold;
		}
		else if (list2.Count > 1 && (int)_difficulty > 0)
		{
			text6 = Strings.Dungeons.CarryNote.ToString(text3);
		}
		if (text6 != null)
		{
			Rectangle rect2 = new Rectangle(rect.X + _d.L(14f), rect.Bottom - _d.L(10f) - _d.L(26f), rect.Width - _d.L(28f), _d.L(26f));
			TechniqueArt.Fill(renderer, rect2, A(color, 0.08f));
			TechniqueArt.Fill(renderer, new Rectangle(rect2.X, rect2.Y, _d.L(2f), rect2.Height), A(color));
			string text7 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), text6, rect2.Width - _d.L(16f), 1).FirstOrDefault();
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text7, rect2.X + _d.L(9f), rect2.Y + rect2.Height / 2, A(BodyLight));
		}
	}

	private static bool IsNear(DungeonDescriptor dungeon, Guid id)
	{
		if (!Globals.Entities.TryGetValue(id, out Entity value) || value.MapId != dungeon.EntranceMapId)
		{
			return false;
		}
		return Math.Max(Math.Abs(value.X - dungeon.EntranceX), Math.Abs(value.Y - dungeon.EntranceY)) <= 8;
	}

	private void DrawChest(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, float left, float top)
	{
		Rectangle rect = _d.R(left, top, 364f, 220f);
		TechniqueArt.Fill(renderer, rect, A(TechniqueArt.White, 0.03f));
		TechniqueArt.Outline(renderer, rect, A(TechniqueArt.Line));
		DungeonDifficultySettings settings = dungeon.Settings(_difficulty);
		long num = Dungeon.ChestReadyInSeconds(dungeon.Id);
		string text = ((num <= 0) ? Strings.Dungeons.ChestReady.ToString() : Strings.Dungeons.ChestLater.ToString(Dungeon.Duration(num))).ToUpperInvariant();
		int num2 = TechniqueArt.SpacedWidth(renderer, TechniqueArt.Label, Pt(12f), text, _d.Em(0.12f, 12f)) + _d.L(16f);
		Rectangle rect2 = new Rectangle(rect.Right - _d.L(14f) - num2, rect.Y + _d.L(11f), num2, _d.L(20f));
		TechniqueArt.Fill(renderer, rect2, A((num <= 0) ? TechniqueArt.Green : TechniqueArt.Gold));
		TechniqueArt.SpacedCenter(renderer, TechniqueArt.Label, Pt(12f), text, rect2.X + rect2.Width / 2, rect2.Y + rect2.Height / 2, _d.Em(0.12f, 12f), A(HouseArt.Dark));
		string text2 = Strings.Dungeons.ChestTitleDifficulty.ToString(Dungeon.DifficultyName(_difficulty));
		string text3 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(19f), text2, rect2.X - rect.X - _d.L(24f), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(19f), text3, rect.X + _d.L(14f), rect.Y + _d.L(21f), A(TechniqueArt.Text));
		int num3 = _d.L(49f);
		int num4 = _d.L(6f);
		int num5 = Math.Max(1, (rect.Width - _d.L(28f) + num4) / (num3 + num4));
		List<Action<Rectangle>> list = new List<Action<Rectangle>>();
		if (settings.ChestZeni > 0)
		{
			list.Add(delegate(Rectangle box)
			{
				DrawCell(renderer, box, TechniqueArt.Line2);
				HouseArt.Zeni(renderer, new Rectangle(box.X + (box.Width - _d.L(32f)) / 2, box.Y + (box.Height - _d.L(32f)) / 2, _d.L(32f), _d.L(32f)), (int)(Timing.Global.MillisecondsUtc / 200), _fade);
				TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), MarketWindow.FormatAmount(settings.ChestZeni), box.Right - _d.L(3f), box.Bottom - _d.L(8f), A(TechniqueArt.Text));
			});
		}
		foreach (DungeonReward reward in dungeon.Chest.Where((DungeonReward dungeonReward) => dungeonReward.DropsIn(_difficulty)))
		{
			ItemDescriptor item = DatabaseObject<ItemDescriptor>.Get(reward.ItemId);
			if (item == null)
			{
				continue;
			}
			list.Add(delegate(Rectangle box)
			{
				Color border = ((reward.Chance <= 15) ? Dungeon.Violet : ((reward.Chance < 100) ? TechniqueArt.Cyan : TechniqueArt.Line2));
				DrawCell(renderer, box, border);
				IGameTexture texture = GameContentManager.Current.GetTexture(TextureType.Item, item.Icon);
				SaiyanHud.Draw(renderer, texture, box.X + (box.Width - _d.L(32f)) / 2, box.Y + (box.Height - _d.L(32f)) / 2, _d.L(32f), _d.L(32f), A(TechniqueArt.White));
				if (reward.Chance < 100)
				{
					TechniqueArt.TextMiddle(renderer, TechniqueArt.Label, Pt(12f), $"{reward.Chance} %", box.X + _d.L(4f), box.Bottom - _d.L(8f), A(TechniqueArt.Soft));
				}
				if (reward.Quantity > 1)
				{
					TechniqueArt.TextRight(renderer, TechniqueArt.Label, Pt(12f), $"{reward.Quantity}", box.Right - _d.L(3f), box.Bottom - _d.L(8f), A(TechniqueArt.Text));
				}
			});
		}
		int num6 = rect.Y + _d.L(40f);
		if (list.Count == 0)
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), Strings.Dungeons.ChestEmpty, rect.X + _d.L(14f), num6 + num3 / 2, A(TechniqueArt.Dim));
			num6 += num3;
		}
		else
		{
			int num7 = Math.Min(2, (list.Count + num5 - 1) / num5);
			for (int num8 = 0; num8 < Math.Min(list.Count, num7 * num5); num8++)
			{
				Rectangle obj = new Rectangle(rect.X + _d.L(14f) + num8 % num5 * (num3 + num4), num6 + num8 / num5 * (num3 + num4), num3, num3);
				list[num8](obj);
			}
			num6 += num7 * num3 + (num7 - 1) * num4;
		}
		if (_difficulty == DungeonDifficulty.Heroic)
		{
			TitleDescriptor titleDescriptor = DatabaseObject<TitleDescriptor>.Get(dungeon.TitleId);
			if (titleDescriptor != null)
			{
				Rectangle rect3 = new Rectangle(rect.X + _d.L(14f), num6 + _d.L(8f), rect.Width - _d.L(28f), _d.L(26f));
				TechniqueArt.Fill(renderer, rect3, A(Dungeon.Violet, 0.08f));
				TechniqueArt.Outline(renderer, rect3, A(Dungeon.Violet, 0.35f));
				int num9 = rect3.X + _d.L(8f);
				num9 += TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), Strings.Dungeons.HeroicTitle, num9, rect3.Y + rect3.Height / 2, A(BodyLight)) + _d.L(6f);
				TechniqueArt.Spaced(renderer, TechniqueArt.Heavy, Pt(15f), titleDescriptor.Name, num9, rect3.Y + rect3.Height / 2, _d.Em(0.05f, 15f), A(Dungeon.Violet));
				num6 = rect3.Bottom;
			}
		}
		string text4 = ((_difficulty == DungeonDifficulty.Heroic) ? Strings.Dungeons.ChestRuleDungeon.ToString(6) : Strings.Dungeons.ChestRuleRicher.ToString(6));
		int num10 = num6 + _d.L(16f);
		foreach (string item2 in TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12.5f), text4, rect.Width - _d.L(28f), Math.Max(1, (rect.Bottom - num10) / _d.L(15f))))
		{
			TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12.5f), item2, rect.X + _d.L(14f), num10, A(TechniqueArt.Soft));
			num10 += _d.L(15f);
		}
	}

	private void DrawCell(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle box, Color border)
	{
		TechniqueArt.Fill(renderer, box, A(new Color(255, 10, 18, 32)));
		TechniqueArt.Outline(renderer, box, A(border));
		if (border != TechniqueArt.Line2)
		{
			TechniqueArt.GlowRect(renderer, box, A(border, 0.12f));
		}
	}

	private void DrawAchievements(Intersect.Client.Framework.Gwen.Renderer.Base renderer, DungeonDescriptor dungeon, DungeonStatusInfo? status, Player me, float top)
	{
		DungeonAchievementInfo[] array = status?.Achievements ?? Array.Empty<DungeonAchievementInfo>();
		DungeonAchievementInfo[] array2 = array.Where((DungeonAchievementInfo achievement) => achievement.Obtained).ToArray();
		string text = Strings.Dungeons.AchievementsCount.ToString(array2.Length, array.Length, array2.Sum((DungeonAchievementInfo a) => a.Points), array.Sum((DungeonAchievementInfo a) => a.Points));
		SectionTitle(renderer, Strings.Dungeons.Achievements, (array.Length != 0) ? text : null, top + 6f);
		if (array.Length == 0)
		{
			return;
		}
		bool solo = me.Party.Count <= 1;
		int num = _d.L(40f);
		int num2 = _d.L(6f);
		int num3 = _d.X(780f);
		int num4 = _d.Y(top + 19f);
		int num5 = -1;
		bool flag = false;
		for (int num6 = 0; num6 < array.Length; num6++)
		{
			DungeonAchievementInfo dungeonAchievementInfo = array[num6];
			if (dungeonAchievementInfo.Custom && !flag)
			{
				flag = true;
				num3 += _d.L(4f);
				TechniqueArt.Fill(renderer, new Rectangle(num3, num4, _d.L(2f), num), A(TechniqueArt.Line2));
				num3 += _d.L(12f);
			}
			Rectangle rect = new Rectangle(num3, num4, num, num);
			string key = $"succes-{num6}";
			bool flag2 = IsHot(key);
			if (flag2)
			{
				num5 = num6;
			}
			bool flag3 = OutOfMode(dungeonAchievementInfo, solo) != null;
			DungeonMedal.Draw(renderer, _d, rect, dungeonAchievementInfo, flag3 && !dungeonAchievementInfo.Obtained, flag2 || _viewed == num6, _fade);
			int chosen = num6;
			AddHit(rect, key, delegate
			{
				_viewed = chosen;
			});
			num3 += num + num2;
		}
		int val = ((num5 >= 0) ? num5 : ((_viewed >= 0 && _viewed < array.Length) ? _viewed : Array.FindIndex(array, (DungeonAchievementInfo achievement) => !achievement.Obtained && OutOfMode(achievement, solo) == null)));
		DungeonAchievementInfo dungeonAchievementInfo2 = array[Math.Max(0, val)];
		int num7 = num3 - num2 + _d.L(14f);
		int num8 = _d.X(1520f);
		TechniqueArt.Fill(renderer, new Rectangle(num7, num4, 1, num), A(TechniqueArt.Line));
		int num9 = num7 + _d.L(12f);
		int num10 = num8 - num9;
		string text2 = Strings.Dungeons.AchievementPoints.ToString(dungeonAchievementInfo2.Points);
		int num11 = TechniqueArt.TextRight(renderer, TechniqueArt.Heavy, Pt(13f), text2, num8, num4 + _d.L(4f), A(TechniqueArt.Gold));
		string text3 = (dungeonAchievementInfo2.Custom ? Strings.Dungeons.AchievementCustom.ToString(dungeonAchievementInfo2.Name) : dungeonAchievementInfo2.Name);
		string text4 = TechniqueArt.Wrap(renderer, TechniqueArt.Heavy, Pt(16f), text3, num10 - num11 - _d.L(8f), 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Heavy, Pt(16f), text4, num9, num4 + _d.L(4f), A(TechniqueArt.Text));
		string text5 = TechniqueArt.Wrap(renderer, TechniqueArt.Body, Pt(12f), dungeonAchievementInfo2.Description, num10, 1).FirstOrDefault();
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text5, num9, num4 + _d.L(21f), A(TechniqueArt.Soft));
		string text6 = OutOfMode(dungeonAchievementInfo2, solo);
		Color color;
		string text8;
		if (!dungeonAchievementInfo2.Obtained)
		{
			if (dungeonAchievementInfo2.Objective <= 1)
			{
				if (text6 == null)
				{
					string text7 = Strings.Dungeons.AchievementPossible.ToString();
					Color green = TechniqueArt.Green;
					color = green;
					text8 = text7;
				}
				else
				{
					Color green = KiLight;
					color = green;
					text8 = text6;
				}
			}
			else
			{
				string text9 = Strings.Dungeons.AchievementRuns.ToString(dungeonAchievementInfo2.Value, dungeonAchievementInfo2.Objective);
				Color green = TechniqueArt.Soft;
				color = green;
				text8 = text9;
			}
		}
		else
		{
			string text10 = Strings.Dungeons.AchievementObtained.ToString();
			Color green = TechniqueArt.Green;
			color = green;
			text8 = text10;
		}
		TechniqueArt.TextMiddle(renderer, TechniqueArt.Body, Pt(12f), text8, num9, num4 + _d.L(37f), A(color));
	}

	private string? OutOfMode(DungeonAchievementInfo achievement, bool solo)
	{
		if ((int)_difficulty < (int)achievement.MinDifficulty)
		{
			return Strings.Dungeons.AchievementNotHere.ToString(Dungeon.DifficultyName(_difficulty), Dungeon.DifficultyName(achievement.MinDifficulty));
		}
		if (!achievement.SoloOnly || solo)
		{
			return null;
		}
		return Strings.Dungeons.AchievementSoloOnly.ToString();
	}
}
