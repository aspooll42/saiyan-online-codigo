using System;
using System.Collections.Generic;
using System.Numerics;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;

namespace Intersect.Client.Framework.Gwen.Renderer;

public class Base : IDisposable, ITextHelper
{
	private Rectangle mClipRegion;

	private Point mRenderOffset;

	private readonly Stack<Color> _drawColor = new Stack<Color>();

	private readonly Stack<int> _lineThickness = new Stack<int>();

	private Color _legacyDrawColor;

	public float Scale { get; set; }

	public virtual Color DrawColor
	{
		get
		{
			if (_drawColor.Count <= 0)
			{
				return _legacyDrawColor;
			}
			return _drawColor.Peek();
		}
		set
		{
			_legacyDrawColor = value;
		}
	}

	public Point RenderOffset
	{
		get
		{
			return mRenderOffset;
		}
		set
		{
			mRenderOffset = value;
		}
	}

	public Rectangle ClipRegion
	{
		get
		{
			return mClipRegion;
		}
		set
		{
			mClipRegion = value;
		}
	}

	public bool ClipRegionVisible
	{
		get
		{
			if (mClipRegion.Width <= 0 || mClipRegion.Height <= 0)
			{
				return false;
			}
			return true;
		}
	}

	public virtual ICacheToTexture Ctt => null;

	protected Base()
	{
		mRenderOffset = Point.Empty;
		Scale = 1f;
		if (Ctt != null)
		{
			Ctt.Initialize();
		}
		_lineThickness.Push(1);
	}

	public virtual void Dispose()
	{
		if (Ctt != null)
		{
			Ctt.ShutDown();
		}
		GC.SuppressFinalize(this);
	}

	public virtual void Begin()
	{
	}

	public virtual void End()
	{
	}

	public virtual IGameTexture GetWhiteTexture()
	{
		return null;
	}

	public virtual void DrawLine(int x, int y, int a, int b)
	{
	}

	public virtual void DrawFilledRect(Rectangle rect)
	{
	}

	public virtual void StartClip()
	{
	}

	public virtual void EndClip()
	{
	}

	public virtual void LoadTexture(IGameTexture t)
	{
	}

	public virtual void FreeTexture(IGameTexture t)
	{
	}

	public virtual void DrawTexturedRect(IGameTexture? texture, Rectangle targetBounds, Color color, float u1 = 0f, float v1 = 0f, float u2 = 1f, float v2 = 1f)
	{
	}

	public virtual void DrawMissingImage(Rectangle rect)
	{
		DrawColor = Color.Red;
		DrawFilledRect(rect);
	}

	public virtual bool LoadFont(IFont font)
	{
		return false;
	}

	public virtual void FreeFont(IFont font)
	{
	}

	public virtual Point MeasureText(IFont? font, int fontSize, string? text, float scale = 1f)
	{
		return default(Point);
	}

	public virtual void RenderText(IFont? font, int fontSize, Point position, string text, float scale = 1f)
	{
	}

	public virtual void DrawLinedRect(Rectangle rect)
	{
		DrawFilledRect(new Rectangle(rect.X, rect.Y, rect.Width, _lineThickness.Peek()));
		DrawFilledRect(new Rectangle(rect.X, rect.Y + rect.Height - _lineThickness.Peek(), rect.Width, _lineThickness.Peek()));
		DrawFilledRect(new Rectangle(rect.X, rect.Y, _lineThickness.Peek(), rect.Height));
		DrawFilledRect(new Rectangle(rect.X + rect.Width - _lineThickness.Peek(), rect.Y, _lineThickness.Peek(), rect.Height));
	}

	public void PushLineThickness(int lineThickness)
	{
		_lineThickness.Push(Math.Max(0, lineThickness));
	}

	public int PopLineThickness()
	{
		return _lineThickness.Pop();
	}

	public void PushDrawColor(Color color)
	{
		_drawColor.Push(color);
	}

	public Color PopDrawColor()
	{
		return _drawColor.Pop();
	}

	public virtual void DrawPixel(int x, int y)
	{
		DrawFilledRect(new Rectangle(x, y, 1, 1));
	}

	public virtual Color PixelColor(IGameTexture texture, uint x, uint y)
	{
		return PixelColor(texture, x, y, Color.White);
	}

	public virtual Color PixelColor(IGameTexture texture, uint x, uint y, Color defaultColor)
	{
		return defaultColor;
	}

	public virtual void DrawShavedCornerRect(Rectangle rect, bool slight = false)
	{
		rect.Width--;
		rect.Height--;
		if (slight)
		{
			DrawFilledRect(new Rectangle(rect.X + 1, rect.Y, rect.Width - 1, 1));
			DrawFilledRect(new Rectangle(rect.X + 1, rect.Y + rect.Height, rect.Width - 1, 1));
			DrawFilledRect(new Rectangle(rect.X, rect.Y + 1, 1, rect.Height - 1));
			DrawFilledRect(new Rectangle(rect.X + rect.Width, rect.Y + 1, 1, rect.Height - 1));
			return;
		}
		DrawPixel(rect.X + 1, rect.Y + 1);
		DrawPixel(rect.X + rect.Width - 1, rect.Y + 1);
		DrawPixel(rect.X + 1, rect.Y + rect.Height - 1);
		DrawPixel(rect.X + rect.Width - 1, rect.Y + rect.Height - 1);
		DrawFilledRect(new Rectangle(rect.X + 2, rect.Y, rect.Width - 3, 1));
		DrawFilledRect(new Rectangle(rect.X + 2, rect.Y + rect.Height, rect.Width - 3, 1));
		DrawFilledRect(new Rectangle(rect.X, rect.Y + 2, 1, rect.Height - 3));
		DrawFilledRect(new Rectangle(rect.X + rect.Width, rect.Y + 2, 1, rect.Height - 3));
	}

	private int TranslateX(int x)
	{
		int num = x + mRenderOffset.X;
		return Util.Ceil((float)num * Scale);
	}

	private int TranslateY(int y)
	{
		int num = y + mRenderOffset.Y;
		return Util.Ceil((float)num * Scale);
	}

	public void Translate(ref int x, ref int y)
	{
		x += mRenderOffset.X;
		y += mRenderOffset.Y;
		x = Util.Ceil((float)x * Scale);
		y = Util.Ceil((float)y * Scale);
	}

	public Point Translate(Point p)
	{
		int x = p.X;
		int y = p.Y;
		Translate(ref x, ref y);
		return new Point(x, y);
	}

	public Rectangle Translate(Rectangle rect)
	{
		return new Rectangle(TranslateX(rect.X), TranslateY(rect.Y), Util.Ceil((float)rect.Width * Scale), Util.Ceil((float)rect.Height * Scale));
	}

	public void AddRenderOffset(Rectangle offset)
	{
		mRenderOffset = new Point(mRenderOffset.X + offset.X, mRenderOffset.Y + offset.Y);
	}

	public void AddClipRegion(Rectangle rect)
	{
		rect.X = mRenderOffset.X;
		rect.Y = mRenderOffset.Y;
		Rectangle rectangle = rect;
		if (rect.X < mClipRegion.X)
		{
			rectangle.Width -= mClipRegion.X - rectangle.X;
			rectangle.X = mClipRegion.X;
		}
		if (rect.Y < mClipRegion.Y)
		{
			rectangle.Height -= mClipRegion.Y - rectangle.Y;
			rectangle.Y = mClipRegion.Y;
		}
		if (rect.Right > mClipRegion.Right)
		{
			rectangle.Width = mClipRegion.Right - rectangle.X;
		}
		if (rect.Bottom > mClipRegion.Bottom)
		{
			rectangle.Height = mClipRegion.Bottom - rectangle.Y;
		}
		mClipRegion = rectangle;
	}

	public Vector2 MeasureText(string? text, IFont? font, int size, float fontScale)
	{
		return MeasureText(font, size, text, fontScale);
	}
}
