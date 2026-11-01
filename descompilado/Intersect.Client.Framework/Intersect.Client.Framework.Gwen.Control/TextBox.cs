using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;
using Intersect.Core;
using Intersect.Framework.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;

public class TextBox : Label
{
	public enum Sounds
	{
		AddText,
		RemoveText,
		Submit
	}

	protected Rectangle mCaretBounds;

	private int mCursorEnd;

	private int mCursorPos;

	protected float mLastInputTime;

	private int mMaxmimumLength = -1;

	private bool mSelectAll;

	protected Rectangle mSelectionBounds;

	private string? _soundNameAddText;

	private string? _soundNameRemoveText;

	private string? _soundNameSubmit;

	private readonly Text _placeholder;

	protected override bool AccelOnlyFocus => true;

	protected override bool NeedsInputChars => true;

	public bool SelectAllOnFocus
	{
		get
		{
			return mSelectAll;
		}
		set
		{
			mSelectAll = value;
			if (value)
			{
				SelectAll();
			}
		}
	}

	public virtual bool HasSelection => mCursorPos != mCursorEnd;

	public int CursorPos
	{
		get
		{
			return mCursorPos;
		}
		set
		{
			if (mCursorPos != value)
			{
				mCursorPos = value;
				RefreshCursorBounds();
			}
		}
	}

	public int CursorEnd
	{
		get
		{
			return mCursorEnd;
		}
		set
		{
			if (mCursorEnd != value)
			{
				mCursorEnd = value;
				RefreshCursorBounds();
			}
		}
	}

	public override IFont? Font
	{
		get
		{
			return base.Font;
		}
		set
		{
			base.Font = value;
			_placeholder.Font = Font;
		}
	}

	public override int FontSize
	{
		get
		{
			return base.FontSize;
		}
		set
		{
			base.FontSize = value;
			_placeholder.FontSize = value;
		}
	}

	public int MaximumLength
	{
		get
		{
			return mMaxmimumLength;
		}
		set
		{
			mMaxmimumLength = value;
		}
	}

	public string? PlaceholderText
	{
		get
		{
			return _placeholder.DisplayedText;
		}
		set
		{
			_placeholder.DisplayedText = value;
		}
	}

	public event GwenEventHandler<TextBox, ValueChangedEventArgs<string>>? TextChanged;

	public event GwenEventHandler<TextBox, EventArgs>? SubmitPressed;

	public TextBox(Base parent, string? name = null)
		: base(parent, name)
	{
		base.AutoSizeToContents = false;
		SetSize(200, 20);
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = true;
		base.Padding = new Padding(4, 2, 4, 2);
		base.TextAlign = Pos.Left | Pos.CenterV;
		mCursorPos = 0;
		mCursorEnd = 0;
		mSelectAll = false;
		base.TextColor = Color.FromArgb(255, 50, 50, 50);
		base.IsTabable = true;
		AddAccelerator("Ctrl + C", OnCopy);
		AddAccelerator("Ctrl + X", OnCut);
		AddAccelerator("Ctrl + V", OnPaste);
		AddAccelerator("Ctrl + A", OnSelectAll);
		AddAccelerator("Ctrl+C", OnCopy);
		AddAccelerator("Ctrl+X", OnCut);
		AddAccelerator("Ctrl+V", OnPaste);
		AddAccelerator("Ctrl+A", OnSelectAll);
		_placeholder = new Text(this)
		{
			ColorOverride = new Color(255, 143, 143, 143),
			IsVisibleInTree = false
		};
	}

	protected override void OnKeyboardFocus()
	{
		base.OnKeyboardFocus();
		ApplicationContext.CurrentContext.Logger.LogDebug("{Node} ({NodeType}) gained keyboard focus", base.CanonicalName, GetType().GetName());
	}

	protected override void OnLostKeyboardFocus()
	{
		base.OnLostKeyboardFocus();
		ApplicationContext.CurrentContext.Logger.LogDebug("{Node} ({NodeType}) lost keyboard focus", base.CanonicalName, GetType().GetName());
	}

	protected virtual bool IsTextAllowed(string text, int position)
	{
		if (MaximumLength >= 0)
		{
			return Text.Length + (text?.Length ?? 0) <= MaximumLength;
		}
		return true;
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
	}

	protected override void OnTextChanged()
	{
		base.OnTextChanged();
		if (mCursorPos > base.TextLength)
		{
			mCursorPos = base.TextLength;
		}
		if (mCursorEnd > base.TextLength)
		{
			mCursorEnd = base.TextLength;
		}
		UpdatePlaceholder();
		RefreshCursorBounds();
		this.TextChanged?.Invoke(this, new ValueChangedEventArgs<string>
		{
			Value = (Text ?? string.Empty)
		});
	}

	protected override bool OnChar(char chr)
	{
		base.OnChar(chr);
		if (chr == '\t')
		{
			return false;
		}
		InsertText(chr.ToString());
		return true;
	}

	public void SetSound(Sounds sound, string? name)
	{
		switch (sound)
		{
		case Sounds.AddText:
			_soundNameAddText = name;
			break;
		case Sounds.RemoveText:
			_soundNameRemoveText = name;
			break;
		case Sounds.Submit:
			_soundNameSubmit = name;
			break;
		default:
			throw new ArgumentOutOfRangeException("sound", sound, null);
		}
	}

	protected void InsertText(string text, bool skipEvents = false)
	{
		ReplaceSelection(text, playSound: true, skipEvents);
		PlaySound(_soundNameAddText);
	}

	private void ValidateCursor()
	{
		int val = Math.Min(mCursorPos, mCursorEnd);
		int val2 = Math.Max(mCursorPos, mCursorEnd);
		mCursorPos = Math.Min(val, base.TextLength);
		mCursorEnd = Math.Min(val2, base.TextLength);
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.ShouldDrawBackground)
		{
			skin.DrawTextBox(this);
		}
		if (base.HasFocus)
		{
			if (mCursorPos != mCursorEnd)
			{
				skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
				skin.Renderer.DrawFilledRect(mSelectionBounds);
			}
			float num = Neutral.GetTimeInSeconds() - mLastInputTime;
			if (num % 1f <= 0.5f)
			{
				Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
				Color textColorOverride = base.TextColorOverride;
				renderer.DrawColor = (((object)textColorOverride != null && textColorOverride.A > 0) ? textColorOverride : (base.TextColor ?? Color.White));
				skin.Renderer.DrawFilledRect(mCaretBounds);
			}
		}
	}

	protected virtual void RefreshCursorBounds()
	{
		mLastInputTime = Neutral.GetTimeInSeconds();
		MakeCaretVisible();
		Point characterPosition = GetCharacterPosition(mCursorPos);
		Point characterPosition2 = GetCharacterPosition(mCursorEnd);
		mSelectionBounds.X = Math.Min(characterPosition.X, characterPosition2.X);
		mSelectionBounds.Y = base.TextY - 1;
		mSelectionBounds.Width = Math.Max(characterPosition.X, characterPosition2.X) - mSelectionBounds.X;
		mSelectionBounds.Height = base.TextHeight + 2;
		mCaretBounds.X = characterPosition.X;
		mCaretBounds.Y = base.TextY - 1;
		mCaretBounds.Width = 1;
		mCaretBounds.Height = base.TextHeight + 2;
		Redraw();
	}

	protected override void OnPaste(Base from, EventArgs args)
	{
		base.OnPaste(from, args);
		if (!IsDisabledByTree)
		{
			InsertText(Neutral.GetClipboardText());
		}
	}

	protected override void OnCopy(Base from, EventArgs args)
	{
		if (HasSelection)
		{
			base.OnCopy(from, args);
			Neutral.SetClipboardText(GetSelection());
		}
	}

	protected override void OnCut(Base from, EventArgs args)
	{
		if (IsDisabledByTree)
		{
			OnCopy(from, args);
		}
		else if (HasSelection)
		{
			base.OnCut(from, args);
			Neutral.SetClipboardText(GetSelection());
			EraseSelection();
		}
	}

	public void SelectAll()
	{
		OnSelectAll(this, EventArgs.Empty);
	}

	protected override void OnSelectAll(Base from, EventArgs args)
	{
		mCursorEnd = 0;
		mCursorPos = base.TextLength;
		RefreshCursorBounds();
	}

	protected override void OnMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDoubleClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			SelectAll();
		}
	}

	protected override bool OnKeyReturn(bool down)
	{
		if (base.OnKeyReturn(down))
		{
			return true;
		}
		if (down)
		{
			return true;
		}
		OnReturn();
		OnKeyTab(down: true);
		if (base.HasFocus)
		{
			Blur();
		}
		return true;
	}

	protected override bool OnKeyBackspace(bool down)
	{
		base.OnKeyBackspace(down);
		if (!down)
		{
			return true;
		}
		if (HasSelection)
		{
			EraseSelection();
			return true;
		}
		if (mCursorPos == 0)
		{
			return true;
		}
		DeleteText(mCursorPos - 1, 1);
		return true;
	}

	protected override bool OnKeyDelete(bool down)
	{
		base.OnKeyDelete(down);
		if (!down)
		{
			return true;
		}
		if (HasSelection)
		{
			EraseSelection();
			return true;
		}
		if (mCursorPos >= base.TextLength)
		{
			return true;
		}
		DeleteText(mCursorPos, 1);
		return true;
	}

	protected override bool OnKeyLeft(bool down)
	{
		base.OnKeyLeft(down);
		if (!down)
		{
			return true;
		}
		if (mCursorPos > 0)
		{
			mCursorPos--;
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyRight(bool down)
	{
		base.OnKeyRight(down);
		if (!down)
		{
			return true;
		}
		if (mCursorPos < base.TextLength)
		{
			mCursorPos++;
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyHome(bool down)
	{
		base.OnKeyHome(down);
		if (!down)
		{
			return true;
		}
		mCursorPos = 0;
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyEnd(bool down)
	{
		base.OnKeyEnd(down);
		mCursorPos = base.TextLength;
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		RefreshCursorBounds();
		return true;
	}

	public string GetSelection()
	{
		if (!HasSelection)
		{
			return string.Empty;
		}
		int num = Math.Min(mCursorPos, mCursorEnd);
		int num2 = Math.Max(mCursorPos, mCursorEnd);
		return Text?.Substring(num, num2 - num) ?? string.Empty;
	}

	public virtual void DeleteText(int startPos, int length, bool playSound = true, bool skipEvents = false)
	{
		ReplaceText(startPos, length, string.Empty, playSound, skipEvents);
	}

	public virtual void ReplaceText(int startPos, int length, string? replacement, bool playSound = true, bool skipEvents = false)
	{
		try
		{
			string text = Text ?? string.Empty;
			if (startPos < 0)
			{
				length = ((length > -startPos) ? (length + startPos) : 0);
				startPos = 0;
				mCursorPos = Math.Max(startPos, mCursorPos);
			}
			if (length > 0)
			{
				text = text.Remove(startPos, length);
			}
			text = text.Insert(startPos, replacement ?? string.Empty);
			SetText(text, !skipEvents);
			if (mCursorPos > startPos)
			{
				CursorPos = Math.Max(0, mCursorPos) + (replacement?.Length ?? 0) - length;
			}
			CursorEnd = mCursorPos;
			if (length > 0 && playSound)
			{
				PlaySound(_soundNameRemoveText);
			}
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogWarning(exception, "Failed to replace {Length} characters in '{Text}' starting at {StartPosition} with '{ReplacementText}", length, Text, startPos, replacement);
		}
	}

	public virtual void ReplaceSelection(string? replacement, bool playSound = true, bool skipEvents = false)
	{
		ValidateCursor();
		string text = Text ?? string.Empty;
		int num = Math.Min(mCursorPos, mCursorEnd);
		int length = text.Length;
		if (length > 0 && num < 0)
		{
			num = Math.Max(-length, num);
			num += length;
		}
		int num2 = Math.Max(num, Math.Max(mCursorPos, mCursorEnd));
		int num3 = num2 - num;
		int num4 = length - num3;
		int num5 = ((MaximumLength < 0) ? int.MaxValue : MaximumLength);
		int val = num5 - num4;
		val = Math.Max(0, val);
		int val2 = replacement?.Length ?? 0;
		val2 = Math.Min(val2, val);
		string replacement2 = replacement?.Substring(0, val2);
		ReplaceText(num, num3, replacement2, playSound, skipEvents);
		mCursorPos += val2;
		mCursorEnd = mCursorPos;
		RefreshCursorBounds();
	}

	public virtual void EraseSelection(bool playSound = true)
	{
		int num = Math.Min(mCursorPos, mCursorEnd);
		int num2 = Math.Max(mCursorPos, mCursorEnd);
		DeleteText(num, num2 - num, playSound);
		mCursorPos = num;
		mCursorEnd = num;
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mouseButton != MouseButton.Left)
		{
			return;
		}
		if (mSelectAll)
		{
			OnSelectAll(this, EventArgs.Empty);
			return;
		}
		int cursorEnd = (CursorPos = GetClosestCharacter(mousePosition).X);
		if (!InputHandler.IsShiftDown)
		{
			CursorEnd = cursorEnd;
		}
		InputHandler.MouseFocus = this;
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left && InputHandler.MouseFocus == this)
		{
			int x = GetClosestCharacter(mousePosition).X;
			CursorPos = x;
			InputHandler.MouseFocus = null;
		}
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		if (InputHandler.MouseFocus == this)
		{
			int x2 = GetClosestCharacter(x, y).X;
			CursorPos = x2;
		}
	}

	protected virtual void MakeCaretVisible()
	{
		int num = GetCharacterPosition(mCursorPos).X - base.TextX;
		int num2 = num + base.TextX;
		if (!((float)num2 > (float)base.Width * 0.1f) || !((float)num2 < (float)base.Width * 0.9f))
		{
			int num3 = (int)((float)(-num) + (float)base.Width * 0.5f);
			if (num3 + base.TextWidth < base.Width - base.Padding.Right)
			{
				num3 = -base.TextWidth + (base.Width - base.Padding.Right);
			}
			if (num3 > base.Padding.Left)
			{
				num3 = base.Padding.Left;
			}
			SetTextPosition(num3, base.TextY);
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		UpdatePlaceholder();
		RefreshCursorBounds();
	}

	private void UpdatePlaceholder()
	{
		_placeholder.IsVisibleInTree = string.IsNullOrEmpty(Text) && !string.IsNullOrWhiteSpace(PlaceholderText);
		AlignTextElement(_placeholder);
	}

	public override void UpdateColors()
	{
		Color color = GetTextColor(ComponentState.Normal) ?? base.Skin.Colors.Label.Normal;
		if (IsDisabledByTree)
		{
			color = GetTextColor(ComponentState.Disabled) ?? base.Skin.Colors.Label.Disabled;
		}
		if (color == null)
		{
			ApplicationContext.CurrentContext.Logger.LogError("Text color for the current control state of '{ComponentName}' is somehow null IsDisabled={IsDisabled} IsActive={IsActive} IsHovered={IsHovered}", base.ParentQualifiedName, base.IsDisabled, base.IsActive, IsHovered);
			color = new Color(255, 0, 255);
		}
		base.TextColor = color;
	}

	protected virtual void OnReturn()
	{
		this.SubmitPressed?.Invoke(this, EventArgs.Empty);
		PlaySound(_soundNameSubmit);
	}

	public void SetMaxLength(int val)
	{
		MaximumLength = val;
	}

	public override JObject? GetJson(bool isRoot = false, bool onlySerializeIfNotEmpty = false)
	{
		JObject json = base.GetJson(isRoot, onlySerializeIfNotEmpty);
		if (json == null)
		{
			return null;
		}
		json.Add("AddTextSound", _soundNameAddText);
		json.Add("RemoveTextSound", _soundNameRemoveText);
		json.Add("SubmitSound", _soundNameSubmit);
		return base.FixJson(json);
	}

	public override void LoadJson(JToken obj, bool isRoot = false)
	{
		base.LoadJson(obj, isRoot: false);
		if (obj["AddTextSound"] != null)
		{
			_soundNameAddText = (string?)obj["AddTextSound"];
		}
		if (obj["RemoveTextSound"] != null)
		{
			_soundNameRemoveText = (string?)obj["RemoveTextSound"];
		}
		if (obj["SubmitSound"] != null)
		{
			_soundNameSubmit = (string?)obj["SubmitSound"];
		}
	}
}
