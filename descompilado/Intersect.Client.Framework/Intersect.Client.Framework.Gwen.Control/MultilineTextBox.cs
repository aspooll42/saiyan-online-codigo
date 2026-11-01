using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Framework.Input;

namespace Intersect.Client.Framework.Gwen.Control;

public class MultilineTextBox : Label
{
	private readonly ScrollControl mScrollControl;

	protected Rectangle mCaretBounds;

	private Point mCursorEnd;

	private Point mCursorPos;

	private float mLastInputTime;

	private bool mSelectAll;

	private List<string> _textLines = new List<string>();

	private Point StartPoint
	{
		get
		{
			if (CursorPosition.Y == mCursorEnd.Y)
			{
				if (CursorPosition.X >= CursorEnd.X)
				{
					return CursorEnd;
				}
				return CursorPosition;
			}
			if (CursorPosition.Y >= CursorEnd.Y)
			{
				return CursorEnd;
			}
			return CursorPosition;
		}
	}

	private Point EndPoint
	{
		get
		{
			if (CursorPosition.Y == mCursorEnd.Y)
			{
				if (CursorPosition.X <= CursorEnd.X)
				{
					return CursorEnd;
				}
				return CursorPosition;
			}
			if (CursorPosition.Y <= CursorEnd.Y)
			{
				return CursorEnd;
			}
			return CursorPosition;
		}
	}

	public bool HasSelection => mCursorPos != mCursorEnd;

	public Point CursorPosition
	{
		get
		{
			if (_textLines == null || _textLines.Count() == 0)
			{
				return new Point(0, 0);
			}
			int y = mCursorPos.Y;
			y = Math.Max(y, 0);
			y = Math.Min(y, _textLines.Count() - 1);
			int x = mCursorPos.X;
			x = Math.Max(x, 0);
			x = Math.Min(x, _textLines[y].Length);
			return new Point(x, y);
		}
		set
		{
			mCursorPos.X = value.X;
			mCursorPos.Y = value.Y;
			RefreshCursorBounds();
		}
	}

	public Point CursorEnd
	{
		get
		{
			if (_textLines == null || _textLines.Count() == 0)
			{
				return new Point(0, 0);
			}
			int y = mCursorEnd.Y;
			y = Math.Max(y, 0);
			y = Math.Min(y, _textLines.Count() - 1);
			int x = mCursorEnd.X;
			x = Math.Max(x, 0);
			x = Math.Min(x, _textLines[y].Length);
			return new Point(x, y);
		}
		set
		{
			mCursorEnd.X = value.X;
			mCursorEnd.Y = value.Y;
			RefreshCursorBounds();
		}
	}

	public bool AcceptTabs { get; set; }

	public int TotalLines => _textLines.Count;

	public override string Text
	{
		get
		{
			return string.Join(Environment.NewLine, _textLines);
		}
		set
		{
			SetText(value);
		}
	}

	public event GwenEventHandler<EventArgs> TextChanged;

	public MultilineTextBox(Base parent)
		: base(parent)
	{
		base.AutoSizeToContents = false;
		SetSize(200, 20);
		base.MouseInputEnabled = true;
		base.KeyboardInputEnabled = true;
		base.Padding = new Padding(4, 2, 4, 2);
		base.TextAlign = Pos.Left | Pos.Top;
		mCursorPos = new Point(0, 0);
		mCursorEnd = new Point(0, 0);
		mSelectAll = false;
		base.TextColor = Color.FromArgb(255, 50, 50, 50);
		base.IsTabable = false;
		AcceptTabs = true;
		mScrollControl = new ScrollControl(this, "_innerPanel");
		mScrollControl.Dock = Pos.Fill;
		mScrollControl.SetOverflow(OverflowBehavior.Auto, OverflowBehavior.Auto);
		mScrollControl.Margin = Margin.One;
		_innerPanel = mScrollControl;
		_textElement.Parent = _innerPanel;
		mScrollControl.InnerPanel.BoundsChanged += ScrollChanged;
		_textLines.Add(string.Empty);
		mScrollControl.SetInnerSize(1000, 1000);
		AddAccelerator("Ctrl + C", OnCopy);
		AddAccelerator("Ctrl + X", OnCut);
		AddAccelerator("Ctrl + V", OnPaste);
		AddAccelerator("Ctrl + A", OnSelectAll);
		AddAccelerator("Ctrl+C", OnCopy);
		AddAccelerator("Ctrl+X", OnCut);
		AddAccelerator("Ctrl+V", OnPaste);
		AddAccelerator("Ctrl+A", OnSelectAll);
	}

	public string GetTextLine(int index)
	{
		return _textLines[index];
	}

	public void SetTextLine(int index, string value)
	{
		_textLines[index] = value;
	}

	private void ScrollChanged(Base control, ValueChangedEventArgs<Rectangle> args)
	{
		RefreshCursorBounds();
	}

	protected override void OnTextChanged()
	{
		base.OnTextChanged();
		if (this.TextChanged != null)
		{
			this.TextChanged(this, EventArgs.Empty);
		}
	}

	protected override bool OnChar(char chr)
	{
		if (chr == '\t' && !AcceptTabs)
		{
			return false;
		}
		InsertText(chr.ToString());
		return true;
	}

	protected void InsertText(string text)
	{
		if (HasSelection)
		{
			EraseSelection();
		}
		string text2 = _textLines[mCursorPos.Y];
		text2 = text2.Insert(CursorPosition.X, text);
		_textLines[mCursorPos.Y] = text2;
		mCursorPos.X = CursorPosition.X + text.Length;
		mCursorEnd = mCursorPos;
		Invalidate();
		RefreshCursorBounds();
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Render(skin);
		if (base.ShouldDrawBackground)
		{
			skin.DrawTextBox(this);
		}
		if (!base.HasFocus)
		{
			return;
		}
		int num = 2 - mScrollControl.VerticalScroll;
		int height = 16;
		if (mCursorPos != mCursorEnd)
		{
			if (StartPoint.Y == EndPoint.Y)
			{
				Point characterPosition = GetCharacterPosition(StartPoint);
				Point characterPosition2 = GetCharacterPosition(EndPoint);
				Rectangle rect = default(Rectangle);
				rect.X = Math.Min(characterPosition.X, characterPosition2.X);
				rect.Y = characterPosition.Y - num;
				rect.Width = Math.Max(characterPosition.X, characterPosition2.X) - rect.X;
				rect.Height = height;
				skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
				skin.Renderer.DrawFilledRect(rect);
			}
			else
			{
				Point characterPosition3 = GetCharacterPosition(StartPoint);
				Point characterPosition4 = GetCharacterPosition(new Point(_textLines[StartPoint.Y].Length, StartPoint.Y));
				Rectangle rect2 = default(Rectangle);
				rect2.X = Math.Min(characterPosition3.X, characterPosition4.X);
				rect2.Y = characterPosition3.Y - num;
				rect2.Width = Math.Max(characterPosition3.X, characterPosition4.X) - rect2.X;
				rect2.Height = height;
				skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
				skin.Renderer.DrawFilledRect(rect2);
				for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
				{
					characterPosition3 = GetCharacterPosition(new Point(0, StartPoint.Y + i));
					characterPosition4 = GetCharacterPosition(new Point(_textLines[StartPoint.Y + i].Length, StartPoint.Y + i));
					rect2 = default(Rectangle);
					rect2.X = Math.Min(characterPosition3.X, characterPosition4.X);
					rect2.Y = characterPosition3.Y - num;
					rect2.Width = Math.Max(characterPosition3.X, characterPosition4.X) - rect2.X;
					rect2.Height = height;
					skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
					skin.Renderer.DrawFilledRect(rect2);
				}
				characterPosition3 = GetCharacterPosition(new Point(0, EndPoint.Y));
				characterPosition4 = GetCharacterPosition(EndPoint);
				rect2 = default(Rectangle);
				rect2.X = Math.Min(characterPosition3.X, characterPosition4.X);
				rect2.Y = characterPosition3.Y - num;
				rect2.Width = Math.Max(characterPosition3.X, characterPosition4.X) - rect2.X;
				rect2.Height = height;
				skin.Renderer.DrawColor = Color.FromArgb(200, 50, 170, 255);
				skin.Renderer.DrawFilledRect(rect2);
			}
		}
		float num2 = Neutral.GetTimeInSeconds() - mLastInputTime;
		if (num2 % 1f <= 0.5f)
		{
			skin.Renderer.DrawColor = Color.Black;
			skin.Renderer.DrawFilledRect(mCaretBounds);
		}
	}

	protected void RefreshCursorBounds()
	{
		mLastInputTime = Neutral.GetTimeInSeconds();
		MakeCaretVisible();
		Point characterPosition = GetCharacterPosition(CursorPosition);
		Point characterPosition2 = GetCharacterPosition(mCursorEnd);
		mCaretBounds.X = characterPosition.X;
		mCaretBounds.Y = characterPosition.Y + 1;
		mCaretBounds.Y += mScrollControl.VerticalScroll;
		mCaretBounds.Width = 1;
		mCaretBounds.Height = 12;
		Redraw();
	}

	protected override void OnPaste(Base from, EventArgs args)
	{
		base.OnPaste(from, args);
		InsertText(Neutral.GetClipboardText());
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
		if (HasSelection)
		{
			base.OnCut(from, args);
			Neutral.SetClipboardText(GetSelection());
			EraseSelection();
		}
	}

	protected override void OnSelectAll(Base from, EventArgs args)
	{
		mCursorEnd = new Point(0, 0);
		mCursorPos = new Point(_textLines.Last().Length, _textLines.Count());
		RefreshCursorBounds();
	}

	protected override void OnMouseDoubleClicked(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDoubleClicked(mouseButton, mousePosition, userAction);
		if (mouseButton == MouseButton.Left)
		{
			OnSelectAll(this, EventArgs.Empty);
		}
	}

	protected override bool OnKeyReturn(bool down)
	{
		if (down)
		{
			return true;
		}
		string text = _textLines[mCursorPos.Y];
		string value = text.Substring(0, CursorPosition.X);
		string item = text.Substring(CursorPosition.X);
		_textLines[mCursorPos.Y] = value;
		_textLines.Insert(mCursorPos.Y + 1, item);
		OnKeyDown(down: true);
		OnKeyHome(down: true);
		if (mCursorPos.Y == TotalLines - 1)
		{
			mScrollControl.ScrollToBottom();
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyBackspace(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (HasSelection)
		{
			EraseSelection();
			return true;
		}
		if (mCursorPos.X == 0)
		{
			if (mCursorPos.Y == 0)
			{
				return true;
			}
			string text = _textLines[mCursorPos.Y - 1];
			string text2 = _textLines[mCursorPos.Y];
			_textLines.RemoveAt(mCursorPos.Y);
			OnKeyUp(down: true);
			OnKeyEnd(down: true);
			_textLines[mCursorPos.Y] = text + text2;
		}
		else
		{
			string text3 = _textLines[mCursorPos.Y];
			string text4 = text3.Substring(0, CursorPosition.X - 1);
			string text5 = text3.Substring(CursorPosition.X);
			_textLines[mCursorPos.Y] = text4 + text5;
			OnKeyLeft(down: true);
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyDelete(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (HasSelection)
		{
			EraseSelection();
			return true;
		}
		if (mCursorPos.X == _textLines[mCursorPos.Y].Length)
		{
			if (mCursorPos.Y == _textLines.Count - 1)
			{
				return true;
			}
			string text = _textLines[mCursorPos.Y];
			string text2 = _textLines[mCursorPos.Y + 1];
			_textLines.RemoveAt(mCursorPos.Y + 1);
			OnKeyEnd(down: true);
			_textLines[mCursorPos.Y] = text + text2;
		}
		else
		{
			string text3 = _textLines[mCursorPos.Y];
			string text4 = text3.Substring(0, CursorPosition.X);
			string text5 = text3.Substring(CursorPosition.X + 1);
			_textLines[mCursorPos.Y] = text4 + text5;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyUp(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (mCursorPos.Y > 0)
		{
			mCursorPos.Y--;
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyDown(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (mCursorPos.Y < TotalLines - 1)
		{
			mCursorPos.Y++;
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyLeft(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (mCursorPos.X > 0)
		{
			mCursorPos.X = Math.Min(mCursorPos.X - 1, _textLines[mCursorPos.Y].Length);
		}
		else if (mCursorPos.Y > 0)
		{
			OnKeyUp(down);
			OnKeyEnd(down);
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyRight(bool down)
	{
		if (!down)
		{
			return true;
		}
		if (mCursorPos.X < _textLines[mCursorPos.Y].Length)
		{
			mCursorPos.X = Math.Min(mCursorPos.X + 1, _textLines[mCursorPos.Y].Length);
		}
		else if (mCursorPos.Y < _textLines.Count - 1)
		{
			OnKeyDown(down);
			OnKeyHome(down);
		}
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyHome(bool down)
	{
		if (!down)
		{
			return true;
		}
		mCursorPos.X = 0;
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyEnd(bool down)
	{
		if (!down)
		{
			return true;
		}
		mCursorPos.X = _textLines[mCursorPos.Y].Length;
		if (!InputHandler.IsShiftDown)
		{
			mCursorEnd = mCursorPos;
		}
		Invalidate();
		RefreshCursorBounds();
		return true;
	}

	protected override bool OnKeyTab(bool down, bool shift = false)
	{
		if (!AcceptTabs)
		{
			return base.OnKeyTab(down, shift);
		}
		if (!down)
		{
			return false;
		}
		OnChar('\t');
		return true;
	}

	public string GetSelection()
	{
		if (!HasSelection)
		{
			return string.Empty;
		}
		string empty = string.Empty;
		if (StartPoint.Y == EndPoint.Y)
		{
			int x = StartPoint.X;
			int x2 = EndPoint.X;
			empty = _textLines[mCursorPos.Y];
			return empty.Substring(x, x2 - x);
		}
		empty = string.Empty;
		empty += _textLines[StartPoint.Y].Substring(StartPoint.X);
		for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
		{
			empty += _textLines[StartPoint.Y + i];
		}
		return empty + _textLines[EndPoint.Y].Substring(0, EndPoint.X);
	}

	public void EraseSelection()
	{
		if (StartPoint.Y == EndPoint.Y)
		{
			int x = StartPoint.X;
			int x2 = EndPoint.X;
			_textLines[StartPoint.Y] = _textLines[StartPoint.Y].Remove(x, x2 - x);
		}
		else
		{
			if (StartPoint.X < _textLines[StartPoint.Y].Length)
			{
				_textLines[StartPoint.Y] = _textLines[StartPoint.Y].Remove(StartPoint.X);
			}
			for (int i = 1; i < EndPoint.Y - StartPoint.Y; i++)
			{
				_textLines.RemoveAt(StartPoint.Y + 1);
			}
			if (EndPoint.X < _textLines[StartPoint.Y + 1].Length)
			{
				_textLines[StartPoint.Y] += _textLines[StartPoint.Y + 1].Substring(EndPoint.X);
			}
			_textLines.RemoveAt(StartPoint.Y + 1);
		}
		mCursorPos = StartPoint;
		mCursorEnd = StartPoint;
		Invalidate();
		RefreshCursorBounds();
	}

	protected override void OnMouseDown(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseDown(mouseButton, mousePosition, userAction);
		if (mSelectAll)
		{
			OnSelectAll(this, EventArgs.Empty);
			return;
		}
		Point cursorEnd = (CursorPosition = GetClosestCharacter(mousePosition));
		if (!InputHandler.IsShiftDown)
		{
			CursorEnd = cursorEnd;
		}
		InputHandler.MouseFocus = this;
		Invalidate();
		RefreshCursorBounds();
	}

	protected override void OnMouseUp(MouseButton mouseButton, Point mousePosition, bool userAction = true)
	{
		base.OnMouseUp(mouseButton, mousePosition, userAction);
		if (mSelectAll)
		{
			OnSelectAll(this, EventArgs.Empty);
			return;
		}
		Point closestCharacter = GetClosestCharacter(mousePosition);
		if (InputHandler.MouseFocus == this)
		{
			CursorPosition = closestCharacter;
			InputHandler.MouseFocus = null;
		}
		Invalidate();
		RefreshCursorBounds();
	}

	protected override Point GetClosestCharacter(int px, int py)
	{
		Point point = CanvasPosToLocal(new Point(px, py));
		double num = double.MaxValue;
		Point result = new Point(0, 0);
		string text = string.Empty;
		for (int i = 0; i < _textLines.Count(); i++)
		{
			text = text + _textLines[i] + Environment.NewLine;
			double num2 = Math.Abs(base.Skin.Renderer.MeasureText(Font, FontSize, text).Y - point.Y);
			if (num2 < num)
			{
				num = num2;
				result.Y = i;
			}
		}
		text = string.Empty;
		num = double.MaxValue;
		for (int j = 0; j <= _textLines[result.Y].Count(); j++)
		{
			text = ((j >= _textLines[result.Y].Count()) ? (text + " ") : (text + _textLines[result.Y][j]));
			double num3 = Math.Abs(base.Skin.Renderer.MeasureText(Font, FontSize, text).X - point.X);
			if (num3 < num)
			{
				num = num3;
				result.X = j;
			}
		}
		return result;
	}

	protected override void OnMouseMoved(int x, int y, int dx, int dy)
	{
		base.OnMouseMoved(x, y, dx, dy);
		if (InputHandler.MouseFocus == this)
		{
			Point closestCharacter = GetClosestCharacter(x, y);
			CursorPosition = closestCharacter;
			Invalidate();
			RefreshCursorBounds();
		}
	}

	protected virtual void MakeCaretVisible()
	{
		int num = GetCharacterPosition(CursorPosition).X - base.TextX;
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

	protected override void OnChildBoundsChanged(Base child, Rectangle oldChildBounds, Rectangle newChildBounds)
	{
		if (mScrollControl != null)
		{
			mScrollControl.UpdateScrollBars();
		}
	}

	public override void SetText(string? text, bool doEvents = true)
	{
		string[] collection = (text?.Replace("\r\n", "\n").Replace('\r', '\n'))?.Split('\n') ?? Array.Empty<string>();
		_textLines = new List<string>(collection);
		Invalidate();
		RefreshCursorBounds();
	}

	public override void Invalidate()
	{
		if (_textElement != null)
		{
			_textElement.DisplayedText = Text;
		}
		if (base.AutoSizeToContents)
		{
			SizeToContents();
		}
		base.Invalidate();
		InvalidateParent();
		OnTextChanged();
	}

	private Point GetCharacterPosition(Point cursorPosition)
	{
		if (_textLines.Count == 0)
		{
			return new Point(0, 0);
		}
		string text = _textLines[cursorPosition.Y].Substring(0, Math.Min(cursorPosition.X, _textLines[cursorPosition.Y].Length));
		string text2 = string.Empty;
		for (int i = 0; i < cursorPosition.Y; i++)
		{
			text2 = text2 + _textLines[i] + "\n";
		}
		Point point = new Point(base.Skin.Renderer.MeasureText(Font, FontSize, text).X, base.Skin.Renderer.MeasureText(Font, FontSize, text2).Y);
		return new Point(point.X + _textElement.X, point.Y + _textElement.Y + base.Padding.Top);
	}

	protected override bool OnMouseWheeled(int delta)
	{
		return mScrollControl.InputMouseWheeled(delta);
	}
}
