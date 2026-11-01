using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Input;
using Intersect.Client.General;
using Intersect.Client.Interface.Game.Chat;
using Intersect.Client.Localization;
using Intersect.Client.Networking;
using Intersect.Framework;
using Intersect.Localization;
using Intersect.Security;
using Intersect.Utilities;

namespace Intersect.Client.Interface.Menu;

public class PasswordChangeWindow : Window
{
	private readonly MainMenu _mainMenu;

	private readonly Window? _previousWindow;

	private readonly IFont? _defaultFont;

	private readonly Panel _inputPanel;

	private readonly Panel _buttonPanel;

	private readonly Button _submitButton;

	private readonly Button _backButton;

	private readonly Panel _tokenPanel;

	private readonly Label _tokenLabel;

	private readonly TextBox _tokenInput;

	private readonly Panel _passwordPanel;

	private readonly Label _passwordLabel;

	private readonly TextBoxPassword _passwordInput;

	private readonly Panel _passwordConfirmationPanel;

	private readonly Label _passwordConfirmationLabel;

	private readonly TextBoxPassword _passwordConfirmationInput;

	private readonly PasswordChangeMode _changeMode;

	public string? Target { get; set; }

	public PasswordChangeWindow(Canvas parent, MainMenu mainMenu, Window? previousWindow, PasswordChangeMode passwordChangeMode)
		: base(parent, Strings.PasswordChange.Title, modal: false, $"{"PasswordChangeWindow"}{passwordChangeMode}")
	{
		_mainMenu = mainMenu;
		_previousWindow = previousWindow;
		_changeMode = passwordChangeMode;
		_defaultFont = GameContentManager.Current.GetFont("sourcesansproblack");
		base.Alignment = new Alignments[1] { Alignments.Center };
		base.MinimumSize = new Point(600, 148);
		base.IsClosable = false;
		base.IsResizable = false;
		base.InnerPanelPadding = new Padding(8);
		base.Titlebar.MouseInputEnabled = false;
		base.TitleLabel.FontSize = 14;
		base.TitleLabel.TextColorOverride = Color.White;
		SkipRender();
		_buttonPanel = new Panel(this, "_buttonPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Right,
			DockChildSpacing = new Padding(8),
			Margin = new Margin(8, 0, 0, 0),
			MinimumSize = new Point(120, 0)
		};
		_submitButton = new Button(_buttonPanel, "_submitButton")
		{
			AutoSizeToContents = true,
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			TabOrder = 4,
			Text = Strings.ForgotPass.Submit
		};
		_submitButton.Clicked += SubmitButtonOnClicked;
		_backButton = new Button(_buttonPanel, "_backButton")
		{
			AutoSizeToContents = true,
			Dock = Pos.Top,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(120, 24),
			Padding = new Padding(8, 4),
			TabOrder = 5,
			Text = Strings.ForgotPass.Back
		};
		_backButton.Clicked += BackButtonOnClicked;
		_inputPanel = new Panel(this, "_inputPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Fill,
			DockChildSpacing = new Padding(8)
		};
		_tokenPanel = new Panel(_inputPanel, "_tokenPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Top,
			DockChildSpacing = new Padding(4),
			MinimumSize = new Point(0, 28)
		};
		LocalizedString localizedString = _changeMode switch
		{
			PasswordChangeMode.ResetToken => Strings.PasswordChange.InputLabelResetCode, 
			PasswordChangeMode.ExistingPassword => Strings.PasswordChange.InputLabelPassword, 
			_ => throw Exceptions.UnreachableInvalidEnum(_changeMode), 
		};
		_tokenLabel = new Label(_tokenPanel, "_passwordLabel")
		{
			AutoSizeToContents = false,
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12,
			Padding = new Padding(0, 0, 10, 0),
			Text = localizedString,
			TextAlign = (Pos.Right | Pos.CenterV)
		};
		_tokenInput = CreateTokenInput(_defaultFont, _tokenPanel, _changeMode);
		_tokenInput.SubmitPressed += InputTextboxOnSubmitPressed;
		_tokenInput.Clicked += InputTextboxOnClicked;
		_tokenInput.SetSound(TextBox.Sounds.AddText, "menu_hover.wav");
		_tokenInput.SetSound(TextBox.Sounds.RemoveText, "menu_back.wav");
		_tokenInput.SetSound(TextBox.Sounds.Submit, "menu_click.wav");
		_passwordPanel = new Panel(_inputPanel, "_passwordPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Top,
			DockChildSpacing = new Padding(4),
			MinimumSize = new Point(0, 28)
		};
		_passwordLabel = new Label(_passwordPanel, "_passwordLabel")
		{
			AutoSizeToContents = false,
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12,
			Padding = new Padding(0, 0, 10, 0),
			Text = Strings.PasswordChange.NewPassword,
			TextAlign = (Pos.Right | Pos.CenterV)
		};
		_passwordInput = new TextBoxPassword(_passwordPanel, "_passwordInput")
		{
			Dock = Pos.Right,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(240, 0),
			Padding = new Padding(4, 2),
			TabOrder = 2,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		_passwordInput.SubmitPressed += InputTextboxOnSubmitPressed;
		_passwordInput.Clicked += InputTextboxOnClicked;
		_passwordInput.SetSound(TextBox.Sounds.AddText, "menu_hover.wav");
		_passwordInput.SetSound(TextBox.Sounds.RemoveText, "menu_back.wav");
		_passwordInput.SetSound(TextBox.Sounds.Submit, "menu_click.wav");
		_passwordConfirmationPanel = new Panel(_inputPanel, "_passwordConfirmationPanel")
		{
			BackgroundColor = Color.Transparent,
			Dock = Pos.Top,
			DockChildSpacing = new Padding(4),
			MinimumSize = new Point(0, 28)
		};
		_passwordConfirmationLabel = new Label(_passwordConfirmationPanel, "_passwordConfirmationLabel")
		{
			AutoSizeToContents = false,
			Dock = Pos.Fill,
			Font = _defaultFont,
			FontSize = 12,
			Padding = new Padding(0, 0, 10, 0),
			Text = Strings.PasswordChange.ConfirmNewPassword,
			TextAlign = (Pos.Right | Pos.CenterV)
		};
		_passwordConfirmationInput = new TextBoxPassword(_passwordConfirmationPanel, "_passwordConfirmationInput")
		{
			Dock = Pos.Right,
			Font = _defaultFont,
			FontSize = 12,
			MinimumSize = new Point(240, 0),
			Padding = new Padding(4, 2),
			TabOrder = 3,
			TextAlign = (Pos.Left | Pos.CenterV)
		};
		_passwordConfirmationInput.SubmitPressed += InputTextboxOnSubmitPressed;
		_passwordConfirmationInput.Clicked += InputTextboxOnClicked;
		_passwordConfirmationInput.SetSound(TextBox.Sounds.AddText, "menu_hover.wav");
		_passwordConfirmationInput.SetSound(TextBox.Sounds.RemoveText, "menu_back.wav");
		_passwordConfirmationInput.SetSound(TextBox.Sounds.Submit, "menu_click.wav");
	}

	private static TextBox CreateTokenInput(IFont? font, Panel tokenPanel, PasswordChangeMode changeMode)
	{
		return changeMode switch
		{
			PasswordChangeMode.ResetToken => new TextBox(tokenPanel, "_passwordInput")
			{
				Dock = Pos.Right,
				Font = font,
				FontSize = 12,
				IsTabable = true,
				MinimumSize = new Point(240, 0),
				Padding = new Padding(4, 2),
				TabOrder = 1,
				TextAlign = (Pos.Left | Pos.CenterV)
			}, 
			PasswordChangeMode.ExistingPassword => new TextBoxPassword(tokenPanel, "_passwordInput")
			{
				Dock = Pos.Right,
				Font = font,
				FontSize = 12,
				MinimumSize = new Point(240, 0),
				Padding = new Padding(4, 2),
				TabOrder = 1,
				TextAlign = (Pos.Left | Pos.CenterV)
			}, 
			_ => throw Exceptions.UnreachableInvalidEnum(changeMode), 
		};
	}

	private void BackButtonOnClicked(Base sender, MouseButtonState arguments)
	{
		ReturnToPreviousWindow();
	}

	private void SubmitButtonOnClicked(Base sender, MouseButtonState arguments)
	{
		TrySendRequest();
	}

	private void InputTextboxOnSubmitPressed(Base sender, EventArgs arguments)
	{
		TrySendRequest();
	}

	private void InputTextboxOnClicked(Base sender, MouseButtonState arguments)
	{
		if (sender is TextBox textBox)
		{
			bool flag = textBox is TextBoxPassword;
			Globals.InputManager.OpenKeyboard(flag ? KeyboardType.Password : KeyboardType.Normal, textBox.Text ?? string.Empty, autoCorrection: false, multiLine: false, flag);
		}
	}

	private void ReturnToPreviousWindow()
	{
		Window previousWindow = _previousWindow;
		if (previousWindow != null)
		{
			_mainMenu.ReturnToWindow(previousWindow);
		}
		else
		{
			_mainMenu.NotifyOpenLogin();
		}
		DelayedDelete();
	}

	private void ReturnToMainMenu()
	{
		_mainMenu.ReturnToMain();
		DelayedDelete();
	}

	public override void Show()
	{
		_tokenInput.Text = null;
		_passwordInput.Text = null;
		_passwordConfirmationInput.Text = null;
		base.Show();
	}

	private void TrySendRequest()
	{
		if (Globals.WaitingOnServer)
		{
			return;
		}
		if (!Intersect.Client.Networking.Network.IsConnected)
		{
			Interface.ShowAlert(Strings.Errors.NotConnected);
			return;
		}
		string text = Target?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			Interface.ShowAlert(Strings.Errors.InvalidStateReturnToMainMenu);
			ReturnToMainMenu();
			return;
		}
		string text2 = _tokenInput.Text?.Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			Interface.ShowAlert(_changeMode switch
			{
				PasswordChangeMode.ResetToken => Strings.PasswordChange.ErrorNoResetCode, 
				PasswordChangeMode.ExistingPassword => Strings.PasswordChange.ErrorNoOldPassword, 
				_ => throw Exceptions.UnreachableInvalidEnum(_changeMode), 
			});
			return;
		}
		string text3 = _passwordInput.Text?.Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			Interface.ShowAlert(Strings.PasswordChange.ErrorNoNewPassword);
			return;
		}
		string text4 = _passwordConfirmationInput.Text?.Trim();
		if (string.IsNullOrWhiteSpace(text4))
		{
			Interface.ShowAlert(Strings.PasswordChange.ErrorNoNewPasswordConfirmation);
			return;
		}
		if (!FieldChecking.IsValidPassword(text3, Strings.Regex.Password))
		{
			Interface.ShowAlert(Strings.Errors.PasswordInvalid);
			return;
		}
		if (MotDePasse.TropCourt(text3))
		{
			Interface.ShowAlert(Strings.Errors.PasswordTooShort.ToString(8));
			return;
		}
		if (!string.Equals(text3, text4, StringComparison.Ordinal))
		{
			Interface.ShowAlert(Strings.Registration.PasswordMismatch);
			return;
		}
		if (_changeMode == PasswordChangeMode.ExistingPassword)
		{
			text2 = PasswordUtils.ComputePasswordHash(text2);
		}
		string passwordHash = PasswordUtils.ComputePasswordHash(text3);
		PacketSender.SendPasswordChangeRequest(text, text2, passwordHash);
		Globals.WaitingOnServer = true;
		ChatboxMsg.ClearMessages();
	}

	protected override void EnsureInitialized()
	{
		LoadJsonUi(GameContentManager.UI.Menu, Graphics.Renderer.GetResolutionString());
	}
}
