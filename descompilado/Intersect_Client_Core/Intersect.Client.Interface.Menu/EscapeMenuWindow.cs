using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Entities;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game;
using Intersect.Client.Interface.Game.EntityPanel;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.MonoGame;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Menu;

public class EscapeMenuWindow : Window
{
	private sealed class PauseHint : Intersect.Client.Framework.Gwen.Control.Base
	{
		public string Hint { get; set; } = string.Empty;

		public string? Key { get; set; }

		public PauseHint(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
			: base(parent, name)
		{
			base.MouseInputEnabled = false;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
			int num = base.Width;
			if (!string.IsNullOrEmpty(Key))
			{
				num -= DropdownPanel.DrawKey(renderer, Key, num, base.Height / 2) + 8;
			}
			SaiyanHud.TextRight(renderer, SaiyanHud.SmallFont, 10, Hint, num, 0, base.Height, new Color(255, 150, 160, 182), shadow: false);
		}
	}

	private sealed class PauseIdentity : Intersect.Client.Framework.Gwen.Control.Base
	{
		private static readonly Color White = new Color(255, 242, 242, 242);

		private static readonly Color Gold = new Color(255, 245, 190, 90);

		private static readonly Color Soft = new Color(255, 176, 186, 206);

		public PauseIdentity(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
			: base(parent, name)
		{
			base.MouseInputEnabled = false;
		}

		protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
		{
			Player me = Globals.Me;
			if (me != null)
			{
				Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
				SaiyanHud.TextLeft(renderer, SaiyanHud.NameFont, 16, me.Name, 0, 9, 30, White);
				string text = Strings.DropdownMenus.LevelAndKarma.ToString(me.Level, KarmaDisplay.TierName(me.KarmaTier));
				SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 10, text, 0, 38, 20, Gold, shadow: false);
				string text2 = me.MapInstance?.Name;
				if (text2 != null && text2.Length > 0)
				{
					SaiyanHud.TextLeft(renderer, SaiyanHud.SmallFont, 10, text2, 0, 56, 18, Soft, shadow: false);
				}
			}
		}
	}

	private const int WindowWidth = 380;

	private const int WindowHeight = 486;

	private const long RefreshMs = 200L;

	private static readonly Color Night = new Color(255, 11, 14, 23);

	private readonly Func<SettingsWindow> _settingsWindowProvider;

	private readonly Label _title;

	private readonly PauseHint _hint;

	private readonly HexPortrait _portrait;

	private readonly PauseIdentity _identity;

	private readonly Button _resume;

	private readonly DropdownPanel _actions;

	private readonly Panel _versionPanel;

	private int _laidOutTitleBarHeight = -1;

	private long _nextRefresh;

	public EscapeMenuWindow(Canvas canvas, Func<SettingsWindow> settingsWindowProvider)
		: base(canvas, Strings.EscapeMenu.Title, modal: false, "EscapeMenuWindow_" + (ClientContext.IsSinglePlayer ? "singleplayer" : "online"))
	{
		_settingsWindowProvider = settingsWindowProvider;
		base.IsClosable = false;
		base.IsResizable = false;
		base.Titlebar.MouseInputEnabled = false;
		base.Titlebar.ShouldDrawBackground = false;
		base.TitleLabel.IsHidden = true;
		base.MinimumSize = new Point(380, 486);
		SetSize(380, 486);
		base.Alignment = new Alignments[1] { Alignments.Center };
		GameContentManager current = GameContentManager.Current;
		IGameTexture texture = current.GetTexture(TextureType.Gui, "saiyan_echap_carte.png");
		SetImage(texture, "saiyan_echap_carte.png", ControlState.Active);
		SetImage(texture, "saiyan_echap_carte.png", ControlState.Inactive);
		_title = new Label(this, "PauseTitle")
		{
			Font = current.GetFont("refuel"),
			FontSize = 20,
			AutoSizeToContents = false,
			MouseInputEnabled = false,
			TextAlign = (Pos.Left | Pos.CenterV),
			Text = Strings.EscapeMenu.Title.ToString().ToUpperInvariant()
		};
		_title.SetTextColor(new Color(255, 242, 242, 242), ComponentState.Normal);
		WindowTitle.Apply(_title);
		_hint = new PauseHint(this, "PauseHint")
		{
			Hint = Strings.PauseMenu.ResumeHint
		};
		_portrait = new HexPortrait(this, "PausePortrait", 35)
		{
			Accent = new Color(255, 255, 172, 82)
		};
		_identity = new PauseIdentity(this, "PauseIdentity");
		_resume = new Button(this, "PauseResume")
		{
			Font = current.GetFont("rajdhanibold"),
			FontSize = 14,
			TextAlign = Pos.Center,
			UsesHeadingFace = false,
			Text = Strings.PauseMenu.Resume.ToString().ToUpperInvariant()
		};
		_resume.SetStateTexture(ComponentState.Normal, "saiyan_cta_348x46.png");
		_resume.SetStateTexture(ComponentState.Hovered, "saiyan_cta_348x46_hover.png");
		_resume.SetStateTexture(ComponentState.Active, "saiyan_cta_348x46_clicked.png");
		_resume.SetStateTexture(ComponentState.Disabled, "saiyan_cta_348x46_disabled.png");
		_resume.SetTextColor(Night, ComponentState.Normal);
		_resume.SetTextColor(Night, ComponentState.Hovered);
		_resume.SetTextColor(Night, ComponentState.Active);
		_resume.Clicked += delegate
		{
			Hide();
		};
		ImagePanel imagePanel = new ImagePanel(_resume, "PauseResumeIcon")
		{
			Texture = current.GetTexture(TextureType.Gui, "saiyan_echap_reprendre.png"),
			MouseInputEnabled = false,
			RenderColor = Night
		};
		imagePanel.SetBounds(22, 15, 16, 16);
		_actions = new DropdownPanel(this, "PauseActions")
		{
			HeaderHeight = 0,
			RowHeight = 46,
			MinimumWidth = 368,
			RowRules = true
		};
		_versionPanel = new VersionPanel(this, "_versionPanel");
		Interface.InputBlockingComponents.Add(this);
		RefreshContent();
		Layout();
	}

	protected override void EnsureInitialized()
	{
		Layout();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Interface.InputBlockingComponents.Remove(this);
	}

	protected override void OnVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
	{
		base.OnVisibilityChanged(sender, eventArgs);
		if (eventArgs.IsVisibleInTree)
		{
			Modal parent = MakeModal(dim: true);
			_versionPanel.Parent = parent;
			RefreshContent();
		}
		else
		{
			_versionPanel.Parent = this;
			RemoveModal();
		}
	}

	public override void ToggleHidden()
	{
		SettingsWindow settingsWindow = _settingsWindowProvider();
		if (!settingsWindow.IsVisibleInTree)
		{
			base.ToggleHidden();
		}
	}

	public void Update()
	{
		if (!base.IsHidden)
		{
			BringToFront();
			if (base.TitleBarBounds.Height != _laidOutTitleBarHeight)
			{
				Layout();
			}
			if (Timing.Global.Milliseconds >= _nextRefresh)
			{
				RefreshContent();
			}
		}
	}

	public void OpenSettingsWindow(bool returnToMenu = false)
	{
		SettingsWindow settingsWindow = _settingsWindowProvider();
		settingsWindow.Show(returnToMenu ? this : null);
		Hide();
	}

	private new int Y(int windowY)
	{
		return windowY - _laidOutTitleBarHeight;
	}

	private void Layout()
	{
		_laidOutTitleBarHeight = base.TitleBarBounds.Height;
		_title.SetBounds(24, Y(10), 220, 40);
		_hint.SetBounds(138, Y(23), 220, 18);
		_portrait.SetPosition(20, Y(71));
		_identity.SetBounds(112, Y(70), 252, 78);
		_resume.SetBounds(16, Y(166), 348, 46);
		_actions.SetPosition(6, Y(226));
	}

	private void RefreshContent()
	{
		_nextRefresh = Timing.Global.Milliseconds + 200;
		_portrait.Entity = Globals.Me;
		_hint.Key = DropdownPanel.KeyOf(Control.OpenMenu);
		_actions.SetRows(BuildRows());
		_actions.Fit();
	}

	private IEnumerable<DropdownRow> BuildRows()
	{
		bool inCombat = Globals.Me?.CombatTimer > Timing.Global.Milliseconds;
		yield return new DropdownRow
		{
			Label = Strings.EscapeMenu.Settings,
			Icon = "saiyan_echap_options.png",
			TintIcon = true,
			Key = DropdownPanel.KeyOf(Control.OpenSettings),
			Action = delegate
			{
				OpenSettingsWindow(returnToMenu: true);
			}
		};
		if (Interface.HasInGameUI)
		{
			yield return new DropdownRow
			{
				Label = Strings.Disposition.Titre,
				Icon = "saiyan_option_glyphe_interface.png",
				TintIcon = true,
				Action = delegate
				{
					Hide();
					Interface.GameUi.OuvrirLaDisposition();
				}
			};
		}
		if (Options.Instance.Player.MaxCharacters > 1)
		{
			yield return new DropdownRow
			{
				Label = Strings.PauseMenu.ChangeCharacter,
				Icon = "saiyan_echap_personnages.png",
				TintIcon = true,
				IsUnavailable = inCombat,
				Info = (inCombat ? Strings.DropdownMenus.InCombat.ToString() : null),
				InfoKind = DropdownInfoKind.Refusal,
				Action = ReturnToCharacterSelection
			};
		}
		if (!ClientContext.IsSinglePlayer)
		{
			yield return new DropdownRow
			{
				Label = Strings.PauseMenu.Logout,
				Icon = "saiyan_echap_deconnexion.png",
				TintIcon = true,
				Action = Logout
			};
		}
		yield return new DropdownRow
		{
			Label = Strings.PauseMenu.Quit,
			Icon = "saiyan_echap_quitter.png",
			TintIcon = true,
			IsDanger = true,
			SeparatorAbove = true,
			Action = ExitToDesktop
		};
	}

	private void ReturnToCharacterSelection()
	{
		Hide();
		if (Globals.Me?.CombatTimer > Timing.Global.Milliseconds || ConfirmCardWindow.OutlawChip() != null)
		{
			ConfirmCardWindow.OpenLogoutInCombat(toCharacterSelect: true, LogoutToCharacterSelect);
		}
		else
		{
			LogoutToCharacterSelect();
		}
	}

	private static void LogoutToCharacterSelect()
	{
		if (Globals.Me != null)
		{
			Globals.Me.CombatTimer = 0L;
		}
		Main.Logout(characterSelect: true);
	}

	private void Logout()
	{
		Hide();
		if (Globals.Me?.CombatTimer > Timing.Global.Milliseconds || ConfirmCardWindow.OutlawChip() != null)
		{
			ConfirmCardWindow.OpenLogoutInCombat(toCharacterSelect: false, LogoutToMainMenu);
		}
		else
		{
			LogoutToMainMenu();
		}
	}

	private static void LogoutToMainMenu()
	{
		if (Globals.Me != null)
		{
			Globals.Me.CombatTimer = 0L;
		}
		Main.Logout(characterSelect: false);
	}

	private void ExitToDesktop()
	{
		Hide();
		if (IntersectGame._isShowingExitConfirmation)
		{
			return;
		}
		ConfirmCardWindow.OpenQuit(Globals.Me?.CombatTimer > Timing.Global.Milliseconds, delegate
		{
			if (Globals.Me != null)
			{
				Globals.Me.CombatTimer = 0L;
			}
			IntersectGame._isShowingExitConfirmation = false;
			Globals.IsRunning = false;
		}, delegate
		{
			IntersectGame._isShowingExitConfirmation = false;
		});
		IntersectGame._isShowingExitConfirmation = true;
	}
}
