using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Enums;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

public sealed class TargetContextMenu : ContextMenu
{
	private const int CardHeight = 60;

	private const int CardTextLeft = 60;

	private const int NameSize = 12;

	private const int CardLineSize = 10;

	private const long RefreshMs = 200L;

	private readonly DropdownPanel _content;

	private readonly HexPortrait _portrait;

	private readonly Player? _me;

	private Player? _target;

	private bool _showCard;

	private Intersect.Client.Framework.Gwen.Control.Base? _button;

	private long _closedOnButtonAt;

	private long _nextRefresh;

	public TargetContextMenu(Canvas gameCanvas)
		: base(gameCanvas, "TargetContextMenu")
	{
		base.IsHidden = true;
		base.IconMarginDisabled = true;
		base.Padding = Padding.Zero;
		base.Overflow = OverflowBehavior.Hidden;
		ClearChildren();
		_me = Globals.Me;
		_content = new DropdownPanel(this, "Content")
		{
			RowHeight = 40,
			MinimumWidth = 300,
			MeasureHeader = MeasureCard,
			DrawHeader = DrawCard
		};
		_portrait = new HexPortrait(_content, "Portrait", 18)
		{
			Accent = DropdownPanel.OrangeLight
		};
		_portrait.SetPosition(10, 11);
	}

	public void ToggleHidden(object? target)
	{
		if (target == null || _me == null)
		{
			return;
		}
		if (!base.IsHidden)
		{
			if (!Globals.InputManager.IsMouseButtonDown(MouseButton.Right))
			{
				Close();
			}
			return;
		}
		Button button = null;
		Player player2;
		int num;
		int num2;
		if (!(target is Button button2))
		{
			if (!(target is Player player) || player == _me)
			{
				return;
			}
			Vector2 vector = Graphics.ConvertToWorldPoint(Globals.InputManager.MousePosition);
			if (!player.WorldPos.Contains(vector.X, vector.Y))
			{
				return;
			}
			player2 = player;
			num = InputHandler.MousePosition.X;
			num2 = InputHandler.MousePosition.Y;
		}
		else
		{
			button = button2;
			player2 = _me.TargetBox?.MyEntity as Player;
			Point point = button2.ToCanvas(Point.Empty);
			num = point.X;
			num2 = point.Y + button2.Height;
		}
		if (player2 == null || player2 == _me)
		{
			return;
		}
		if (button != null && button == _button && Timing.Global.Milliseconds - _closedOnButtonAt < 1500)
		{
			_closedOnButtonAt = 0L;
			return;
		}
		_target = player2;
		_button = button;
		_showCard = button == null;
		Refresh();
		Canvas canvas = base.Canvas;
		if (canvas != null)
		{
			if (num + base.Width >= canvas.Width)
			{
				num -= base.Width - (button?.Width ?? 0);
			}
			if (num2 + base.Height >= canvas.Height)
			{
				num2 -= base.Height + (button?.Height ?? 0);
			}
			num = Math.Clamp(num, 0, Math.Max(0, canvas.Width - base.Width));
			num2 = Math.Clamp(num2, 0, Math.Max(0, canvas.Height - base.Height));
		}
		Open(Pos.None);
		SetPosition(num, num2);
	}

	public override void Close()
	{
		if (!base.IsHidden && _button != null && InputHandler.HoveredControl == _button)
		{
			_closedOnButtonAt = Timing.Global.Milliseconds;
		}
		base.Close();
	}

	public override void Think()
	{
		base.Think();
		if (!base.IsHidden && Timing.Global.Milliseconds >= _nextRefresh)
		{
			if (_target == null || !Globals.Entities.ContainsKey(_target.Id))
			{
				Close();
			}
			else
			{
				Refresh();
			}
		}
	}

	protected override void OnPositioningBeforeOpen()
	{
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void RenderUnder(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		DropdownPanel.DrawPlate(skin.Renderer, base.Width, base.Height);
	}

	private void Refresh()
	{
		_nextRefresh = Timing.Global.Milliseconds + 200;
		_portrait.IsHidden = !_showCard;
		_portrait.Entity = _target;
		_content.HeaderHeight = (_showCard ? 60 : 6);
		_content.SetRows(BuildRows());
		Point point = _content.Fit();
		if (base.Width != point.X || base.Height != point.Y)
		{
			SetSize(point.X, point.Y);
		}
	}

	private IEnumerable<DropdownRow> BuildRows()
	{
		Player me = _me;
		if (me == null)
		{
			yield break;
		}
		Player target = _target;
		if (target != null)
		{
			string inCombatReason = ((me.CombatTimer >= Timing.Global.Milliseconds) ? Strings.DropdownMenus.InCombat.ToString() : null);
			yield return new DropdownRow
			{
				Label = Strings.EntityContextMenu.Trade,
				Icon = "saiyan_carte_icone_echange.png",
				Info = inCombatReason,
				InfoKind = DropdownInfoKind.Refusal,
				IsUnavailable = (inCombatReason != null),
				Action = RequestTrade
			};
			string text = (me.IsInMyParty(target) ? Strings.DropdownMenus.InYourParty.ToString() : ((me.Party.Count >= Math.Max(1, Options.Instance.Party.MaximumMembers)) ? Strings.DropdownMenus.PartyFull.ToString() : inCombatReason));
			yield return new DropdownRow
			{
				Label = Strings.EntityContextMenu.InviteToParty,
				Icon = "saiyan_carte_icone_groupe.png",
				Info = text,
				InfoKind = DropdownInfoKind.Refusal,
				IsUnavailable = (text != null),
				Action = InviteToParty
			};
			string text2 = (me.IsFriend(target) ? Strings.DropdownMenus.AlreadyFriend.ToString() : inCombatReason);
			yield return new DropdownRow
			{
				RuleAbove = true,
				Label = Strings.EntityContextMenu.AddFriend,
				Icon = "saiyan_carte_icone_ami.png",
				Info = text2,
				InfoKind = DropdownInfoKind.Refusal,
				IsUnavailable = (text2 != null),
				Action = AddFriend
			};
			yield return new DropdownRow
			{
				Label = Strings.EntityContextMenu.PrivateMessage,
				Icon = "saiyan_amis_icone_message.png",
				Action = SendPrivateMessage
			};
			if (me.GuildRank?.Permissions?.Invite == true)
			{
				string text3 = ((!string.IsNullOrWhiteSpace(target.Guild)) ? Strings.DropdownMenus.HasGuild.ToString() : inCombatReason);
				yield return new DropdownRow
				{
					RuleAbove = true,
					Label = Strings.EntityContextMenu.InviteToGuild,
					Icon = "saiyan_carte_icone_clan.png",
					Info = text3,
					InfoKind = DropdownInfoKind.Refusal,
					IsUnavailable = (text3 != null),
					Action = InviteToGuild
				};
			}
		}
	}

	private string CardLine()
	{
		Player target = _target;
		if (target == null)
		{
			return string.Empty;
		}
		return Strings.DropdownMenus.LevelAndKarma.ToString(target.Level, KarmaDisplay.TierName(target.KarmaTier));
	}

	private int MeasureCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer)
	{
		if (_showCard)
		{
			Player target = _target;
			if (target != null)
			{
				int x = SaiyanHud.Measure(renderer, SaiyanHud.NameFont, 12, target.Name).X;
				int x2 = SaiyanHud.Measure(renderer, SaiyanHud.SmallFont, 10, CardLine()).X;
				return 60 + Math.Max(x, x2) + 10;
			}
		}
		return 0;
	}

	private void DrawCard(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int width)
	{
		if (_showCard)
		{
			Player target = _target;
			if (target != null)
			{
				Color color = KarmaDisplay.NameColors(target.KarmaTier).Name ?? DropdownPanel.SoftInk;
				SaiyanHud.TextLeft(renderer, SaiyanHud.NameFont, 12, target.Name, 60, 7, 24, color);
				SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 10, CardLine(), 60, 29, 20, DropdownPanel.SoftInk, shadow: false);
				renderer.DrawColor = DropdownPanel.RuleColor;
				renderer.DrawFilledRect(new Rectangle(10, 58, width - 20, 1));
			}
		}
	}

	private void InviteToParty()
	{
		if (_me != null && _target != null)
		{
			if (_me.CombatTimer < Timing.Global.Milliseconds)
			{
				PacketSender.SendPartyInvite(_target.Id);
			}
			else
			{
				PacketSender.SendChatMsg(Strings.Parties.InFight.ToString(), 4);
			}
		}
	}

	private void RequestTrade()
	{
		if (_me != null && _target != null)
		{
			if (_me.CombatTimer < Timing.Global.Milliseconds)
			{
				PacketSender.SendTradeRequest(_target.Id);
			}
			else
			{
				PacketSender.SendChatMsg(Strings.Trading.InFight.ToString(), 4);
			}
		}
	}

	private void AddFriend()
	{
		if (_me != null && _target != null)
		{
			if (_me.CombatTimer < Timing.Global.Milliseconds)
			{
				PacketSender.SendAddFriend(_target.Name);
			}
			else
			{
				PacketSender.SendChatMsg(Strings.Friends.InFight.ToString(), 4);
			}
		}
	}

	private void InviteToGuild()
	{
		if (_me == null || _target == null)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(_target.Guild))
		{
			ChatboxMsg.AddMessage(new ChatboxMsg(Strings.Guilds.InviteAlreadyInGuild, Color.Red, ChatMessageType.Guild));
		}
		else if (_me.GuildRank?.Permissions?.Invite == true)
		{
			if (_me.CombatTimer < Timing.Global.Milliseconds)
			{
				PacketSender.SendInviteGuild(_target.Name);
			}
			else
			{
				PacketSender.SendChatMsg(Strings.Friends.InFight.ToString(), 4);
			}
		}
	}

	private void SendPrivateMessage()
	{
		if (_target != null)
		{
			Interface.GameUi.SetChatboxText("/pm " + _target.Name + " ");
		}
	}
}
