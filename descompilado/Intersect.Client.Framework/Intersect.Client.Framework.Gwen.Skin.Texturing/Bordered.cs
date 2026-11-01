using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin.Texturing;

public struct Bordered : IEquatable<Bordered>, IAtlasDrawable
{
	private readonly float mX;

	private readonly float mY;

	private IGameTexture _texture;

	private readonly SubRect[] _subRects;

	private Margin _margin;

	private float _width;

	private float _height;

	public Margin Margin => _margin;

	public static implicit operator FivePatch(Bordered bordered)
	{
		return new FivePatch(bordered._texture, (int)bordered.mX, (int)bordered.mY, (int)bordered._width, (int)bordered._height, bordered._margin, default(Margin));
	}

	public Bordered(IGameTexture texture, float x, float y, float w, float h, Margin inMargin)
	{
		this = default(Bordered);
		mX = x;
		mY = y;
		_subRects = new SubRect[9];
		for (int i = 0; i < _subRects.Length; i++)
		{
			_subRects[i].Uv = new float[4];
		}
		Init(texture, x, y, w, h, inMargin);
	}

	private void DrawRect(Intersect.Client.Framework.Gwen.Renderer.Base render, int i, int x, int y, int w, int h, Color clr)
	{
		render.DrawTexturedRect(_texture, new Rectangle(x, y, w, h), clr, _subRects[i].Uv[0], _subRects[i].Uv[1], _subRects[i].Uv[2], _subRects[i].Uv[3]);
	}

	private void SetRect(int num, float x, float y, float w, float h)
	{
		if (_texture != null)
		{
			float num2 = _texture.Width;
			float num3 = _texture.Height;
			_subRects[num].Uv[0] = x / num2;
			_subRects[num].Uv[1] = y / num3;
			_subRects[num].Uv[2] = (x + w) / num2;
			_subRects[num].Uv[3] = (y + h) / num3;
		}
	}

	private void Init(IGameTexture texture, float x, float y, float w, float h, Margin inMargin, float drawMarginScale = 1f)
	{
		_texture = texture;
		_margin = inMargin;
		SetRect(0, x, y, _margin.Left, _margin.Top);
		SetRect(1, x + (float)_margin.Left, y, w - (float)_margin.Left - (float)_margin.Right, _margin.Top);
		SetRect(2, x + w - (float)_margin.Right, y, _margin.Right, _margin.Top);
		SetRect(3, x, y + (float)_margin.Top, _margin.Left, h - (float)_margin.Top - (float)_margin.Bottom);
		SetRect(4, x + (float)_margin.Left, y + (float)_margin.Top, w - (float)_margin.Left - (float)_margin.Right, h - (float)_margin.Top - (float)_margin.Bottom);
		SetRect(5, x + w - (float)_margin.Right, y + (float)_margin.Top, _margin.Right, h - (float)_margin.Top - (float)_margin.Bottom);
		SetRect(6, x, y + h - (float)_margin.Bottom, _margin.Left, _margin.Bottom);
		SetRect(7, x + (float)_margin.Left, y + h - (float)_margin.Bottom, w - (float)_margin.Left - (float)_margin.Right, _margin.Bottom);
		SetRect(8, x + w - (float)_margin.Right, y + h - (float)_margin.Bottom, _margin.Right, _margin.Bottom);
		_width = w;
		_height = h;
	}

	public void Draw(Intersect.Client.Framework.Gwen.Renderer.Base render, Rectangle r, Color col)
	{
		if (_texture != null)
		{
			render.DrawColor = col;
			if ((float)r.Width < _width && (float)r.Height < _height)
			{
				render.DrawTexturedRect(_texture, r, col, _subRects[0].Uv[0], _subRects[0].Uv[1], _subRects[8].Uv[2], _subRects[8].Uv[3]);
				return;
			}
			DrawRect(render, 0, r.X, r.Y, _margin.Left, _margin.Top, col);
			DrawRect(render, 1, r.X + _margin.Left, r.Y, r.Width - _margin.Left - _margin.Right, _margin.Top, col);
			DrawRect(render, 2, r.X + r.Width - _margin.Right, r.Y, _margin.Right, _margin.Top, col);
			DrawRect(render, 3, r.X, r.Y + _margin.Top, _margin.Left, r.Height - _margin.Top - _margin.Bottom, col);
			DrawRect(render, 4, r.X + _margin.Left, r.Y + _margin.Top, r.Width - _margin.Left - _margin.Right, r.Height - _margin.Top - _margin.Bottom, col);
			DrawRect(render, 5, r.X + r.Width - _margin.Right, r.Y + _margin.Top, _margin.Right, r.Height - _margin.Top - _margin.Bottom, col);
			DrawRect(render, 6, r.X, r.Y + r.Height - _margin.Bottom, _margin.Left, _margin.Bottom, col);
			DrawRect(render, 7, r.X + _margin.Left, r.Y + r.Height - _margin.Bottom, r.Width - _margin.Left - _margin.Right, _margin.Bottom, col);
			DrawRect(render, 8, r.X + r.Width - _margin.Right, r.Y + r.Height - _margin.Bottom, _margin.Right, _margin.Bottom, col);
		}
	}

	public bool Equals(Bordered other)
	{
		IGameTexture texture = _texture;
		if (texture != null && texture.Equals(other._texture) && _subRects.Equals(other._subRects) && _margin.Equals(other._margin) && _width.Equals(other._width))
		{
			return _height.Equals(other._height);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is Bordered other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_texture, _subRects, _margin, _width, _height);
	}

	public static bool operator ==(Bordered lhs, Bordered rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Bordered lhs, Bordered rhs)
	{
		return !lhs.Equals(rhs);
	}
}
