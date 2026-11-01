using System;
using System.Runtime.CompilerServices;
using System.Text;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin.Texturing;

public readonly record struct FivePatch : IAtlasDrawable
{
	private readonly IGameTexture _texture;

	private readonly float _textureWidth;

	private readonly float _textureHeight;

	private readonly UVSquare[] _uvSquares;

	private readonly Rectangle[] _debugUVSquares;

	private readonly Margin _exteriorMargin;

	private readonly Margin _interiorMargin;

	private readonly float _x;

	private readonly float _y;

	private readonly float _width;

	private readonly float _height;

	private readonly int _interiorWidth;

	private readonly int _interiorHeight;

	private readonly Rectangle[] _debugDrawSquares;

	public FivePatch(IGameTexture texture, int x, int y, int width, int height, Margin exteriorMargin, Margin interiorMargin)
	{
		this = default(FivePatch);
		_debugDrawSquares = new Rectangle[25];
		_texture = texture;
		_textureWidth = _texture.Width;
		_textureHeight = _texture.Height;
		_x = x;
		_y = y;
		_width = width;
		_height = height;
		_exteriorMargin = exteriorMargin;
		_interiorMargin = interiorMargin;
		_interiorWidth = width - (exteriorMargin.Left + interiorMargin.Left + interiorMargin.Right + exteriorMargin.Right);
		_interiorHeight = height - (exteriorMargin.Top + interiorMargin.Top + interiorMargin.Bottom + exteriorMargin.Bottom);
		_uvSquares = new UVSquare[25];
		_debugUVSquares = new Rectangle[25];
		Init(x, y, width, height);
	}

	private void Init(int x, int y, int w, int h)
	{
		int num = 0;
		SetUVSquare(num, x, y, _exteriorMargin.Left, _exteriorMargin.Top);
		SetUVSquare(num + 1, x + _exteriorMargin.Left, y, _interiorMargin.Left, _exteriorMargin.Top);
		SetUVSquare(num + 2, x + _exteriorMargin.Left + _interiorMargin.Left, y, _interiorWidth, _exteriorMargin.Top);
		SetUVSquare(num + 3, x + w - (_exteriorMargin.Right + _interiorMargin.Right), y, _interiorMargin.Right, _exteriorMargin.Top);
		SetUVSquare(num + 4, x + w - _exteriorMargin.Right, y, _exteriorMargin.Right, _exteriorMargin.Top);
		num = 5;
		SetUVSquare(num, x, y + _exteriorMargin.Top, _exteriorMargin.Left, _interiorMargin.Top);
		SetUVSquare(num + 1, x + _exteriorMargin.Left, y + _exteriorMargin.Top, _interiorMargin.Left, _interiorMargin.Top);
		SetUVSquare(num + 2, x + _exteriorMargin.Left + _interiorMargin.Left, y + _exteriorMargin.Top, _interiorWidth, _interiorMargin.Top);
		SetUVSquare(num + 3, x + w - (_exteriorMargin.Right + _interiorMargin.Right), y + _exteriorMargin.Top, _interiorMargin.Right, _interiorMargin.Top);
		SetUVSquare(num + 4, x + w - _exteriorMargin.Right, y + _exteriorMargin.Top, _exteriorMargin.Right, _interiorMargin.Top);
		num = 10;
		SetUVSquare(num, x, y + _exteriorMargin.Top + _interiorMargin.Top, _exteriorMargin.Left, _interiorHeight);
		SetUVSquare(num + 1, x + _exteriorMargin.Left, y + _exteriorMargin.Top + _interiorMargin.Top, _interiorMargin.Left, _interiorHeight);
		SetUVSquare(num + 2, x + _exteriorMargin.Left + _interiorMargin.Left, y + _exteriorMargin.Top + _interiorMargin.Top, _interiorWidth, _interiorHeight);
		SetUVSquare(num + 3, x + w - (_exteriorMargin.Right + _interiorMargin.Right), y + _exteriorMargin.Top + _interiorMargin.Top, _interiorMargin.Right, _interiorHeight);
		SetUVSquare(num + 4, x + w - _exteriorMargin.Right, y + _exteriorMargin.Top + _interiorMargin.Top, _exteriorMargin.Right, _interiorHeight);
		num = 15;
		SetUVSquare(num, x, y + h - (_exteriorMargin.Bottom + _interiorMargin.Bottom), _exteriorMargin.Left, _interiorMargin.Bottom);
		SetUVSquare(num + 1, x + _exteriorMargin.Left, y + h - (_exteriorMargin.Bottom + _interiorMargin.Bottom), _interiorMargin.Left, _interiorMargin.Bottom);
		SetUVSquare(num + 2, x + _exteriorMargin.Left + _interiorMargin.Left, y + h - (_exteriorMargin.Bottom + _interiorMargin.Bottom), _interiorWidth, _interiorMargin.Bottom);
		SetUVSquare(num + 3, x + w - (_exteriorMargin.Right + _interiorMargin.Right), y + h - (_exteriorMargin.Bottom + _interiorMargin.Bottom), _interiorMargin.Right, _interiorMargin.Bottom);
		SetUVSquare(num + 4, x + w - _exteriorMargin.Right, y + h - (_exteriorMargin.Bottom + _interiorMargin.Bottom), _exteriorMargin.Right, _interiorMargin.Bottom);
		num = 20;
		SetUVSquare(num, x, y + h - _exteriorMargin.Bottom, _exteriorMargin.Left, _exteriorMargin.Bottom);
		SetUVSquare(num + 1, x + _exteriorMargin.Left, y + h - _exteriorMargin.Bottom, _interiorMargin.Left, _exteriorMargin.Bottom);
		SetUVSquare(num + 2, x + _exteriorMargin.Left + _interiorMargin.Left, y + h - _exteriorMargin.Bottom, _interiorWidth, _exteriorMargin.Bottom);
		SetUVSquare(num + 3, x + w - (_exteriorMargin.Right + _interiorMargin.Right), y + h - _exteriorMargin.Bottom, _interiorMargin.Right, _exteriorMargin.Bottom);
		SetUVSquare(num + 4, x + w - _exteriorMargin.Right, y + h - _exteriorMargin.Bottom, _exteriorMargin.Right, _exteriorMargin.Bottom);
	}

	private void SetUVSquare(int num, int x, int y, int w, int h)
	{
		_uvSquares[num] = new UVSquare((float)x / _textureWidth, (float)y / _textureHeight, (float)(x + w) / _textureWidth, (float)(y + h) / _textureHeight);
		_debugUVSquares[num] = new Rectangle(x, y, w, h);
	}

	private void DrawUVSquare(Intersect.Client.Framework.Gwen.Renderer.Base renderer, int uvSquareIndex, Rectangle targetBounds, Color color)
	{
		if (targetBounds.Width >= 1 && targetBounds.Height >= 1)
		{
			_debugDrawSquares[uvSquareIndex] = targetBounds;
			UVSquare uVSquare = _uvSquares[uvSquareIndex];
			renderer.DrawTexturedRect(_texture, targetBounds, color, uVSquare.U1, uVSquare.V1, uVSquare.U2, uVSquare.V2);
		}
	}

	public void Draw(Intersect.Client.Framework.Gwen.Renderer.Base renderer, Rectangle targetBounds, Color color)
	{
		renderer.DrawColor = color;
		if ((float)targetBounds.Width < _width && (float)targetBounds.Height < _height)
		{
			renderer.DrawTexturedRect(_texture, targetBounds, color, _uvSquares[12].U1, _uvSquares[12].V1, _uvSquares[12].U2, _uvSquares[12].V2);
			return;
		}
		int left = targetBounds.Left;
		int top = targetBounds.Top;
		int right = targetBounds.Right;
		int bottom = targetBounds.Bottom;
		float num = (float)_interiorMargin.Left / Math.Max(1f, _interiorMargin.Left + _interiorMargin.Right);
		float num2 = (float)_interiorMargin.Top / Math.Max(1f, _interiorMargin.Top + _interiorMargin.Bottom);
		Margin exteriorMargin = _exteriorMargin;
		int num3 = targetBounds.Width - (exteriorMargin.Left + exteriorMargin.Right + _interiorWidth);
		int num4 = targetBounds.Height - (exteriorMargin.Top + exteriorMargin.Bottom + _interiorHeight);
		Margin margin = new Margin((int)Math.Floor(num * (float)num3), (int)Math.Floor(num2 * (float)num4), (int)Math.Ceiling((1f - num) * (float)num3), (int)Math.Ceiling((1f - num2) * (float)num4));
		int num5 = 0;
		DrawUVSquare(renderer, num5, new Rectangle(left, top, exteriorMargin.Left, exteriorMargin.Top), color);
		DrawUVSquare(renderer, num5 + 1, new Rectangle(left + exteriorMargin.Left, top, margin.Left, exteriorMargin.Top), color);
		DrawUVSquare(renderer, num5 + 2, new Rectangle(left + exteriorMargin.Left + margin.Left, top, _interiorWidth, exteriorMargin.Top), color);
		DrawUVSquare(renderer, num5 + 3, new Rectangle(right - (exteriorMargin.Right + margin.Right), top, margin.Right, exteriorMargin.Top), color);
		DrawUVSquare(renderer, num5 + 4, new Rectangle(right - exteriorMargin.Right, top, exteriorMargin.Right, exteriorMargin.Top), color);
		num5 = 5;
		DrawUVSquare(renderer, num5, new Rectangle(left, top + exteriorMargin.Top, exteriorMargin.Left, margin.Top), color);
		DrawUVSquare(renderer, num5 + 1, new Rectangle(left + exteriorMargin.Left, top + exteriorMargin.Top, margin.Left, margin.Top), color);
		DrawUVSquare(renderer, num5 + 2, new Rectangle(left + exteriorMargin.Left + margin.Left, top + exteriorMargin.Top, _interiorWidth, margin.Top), color);
		DrawUVSquare(renderer, num5 + 3, new Rectangle(right - (exteriorMargin.Right + margin.Right), top + exteriorMargin.Top, margin.Right, margin.Top), color);
		DrawUVSquare(renderer, num5 + 4, new Rectangle(right - exteriorMargin.Right, top + exteriorMargin.Top, exteriorMargin.Right, margin.Top), color);
		num5 = 10;
		DrawUVSquare(renderer, num5, new Rectangle(left, top + exteriorMargin.Top + margin.Top, exteriorMargin.Left, _interiorHeight), color);
		DrawUVSquare(renderer, num5 + 1, new Rectangle(left + exteriorMargin.Left, top + exteriorMargin.Top + margin.Top, margin.Left, _interiorHeight), color);
		DrawUVSquare(renderer, num5 + 2, new Rectangle(left + exteriorMargin.Left + margin.Left, top + exteriorMargin.Top + margin.Top, _interiorWidth, _interiorHeight), color);
		DrawUVSquare(renderer, num5 + 3, new Rectangle(right - (exteriorMargin.Right + margin.Right), top + exteriorMargin.Top + margin.Top, margin.Right, _interiorHeight), color);
		DrawUVSquare(renderer, num5 + 4, new Rectangle(right - exteriorMargin.Right, top + exteriorMargin.Top + margin.Top, exteriorMargin.Right, _interiorHeight), color);
		num5 = 15;
		DrawUVSquare(renderer, num5, new Rectangle(left, bottom - (exteriorMargin.Bottom + margin.Bottom), exteriorMargin.Left, margin.Bottom), color);
		DrawUVSquare(renderer, num5 + 1, new Rectangle(left + exteriorMargin.Left, bottom - (exteriorMargin.Bottom + margin.Bottom), margin.Left, margin.Bottom), color);
		DrawUVSquare(renderer, num5 + 2, new Rectangle(left + exteriorMargin.Left + margin.Left, bottom - (exteriorMargin.Bottom + margin.Bottom), _interiorWidth, margin.Bottom), color);
		DrawUVSquare(renderer, num5 + 3, new Rectangle(right - (exteriorMargin.Right + margin.Right), bottom - (exteriorMargin.Bottom + margin.Bottom), margin.Right, margin.Bottom), color);
		DrawUVSquare(renderer, num5 + 4, new Rectangle(right - exteriorMargin.Right, bottom - (exteriorMargin.Bottom + margin.Bottom), exteriorMargin.Right, margin.Bottom), color);
		num5 = 20;
		DrawUVSquare(renderer, num5, new Rectangle(left, bottom - exteriorMargin.Bottom, exteriorMargin.Left, exteriorMargin.Bottom), color);
		DrawUVSquare(renderer, num5 + 1, new Rectangle(left + exteriorMargin.Left, bottom - exteriorMargin.Bottom, margin.Left, exteriorMargin.Bottom), color);
		DrawUVSquare(renderer, num5 + 2, new Rectangle(left + exteriorMargin.Left + margin.Left, bottom - exteriorMargin.Bottom, _interiorWidth, exteriorMargin.Bottom), color);
		DrawUVSquare(renderer, num5 + 3, new Rectangle(right - (exteriorMargin.Right + margin.Right), bottom - exteriorMargin.Bottom, margin.Right, exteriorMargin.Bottom), color);
		DrawUVSquare(renderer, num5 + 4, new Rectangle(right - exteriorMargin.Right, bottom - exteriorMargin.Bottom, exteriorMargin.Right, exteriorMargin.Bottom), color);
	}

	[CompilerGenerated]
	private bool PrintMembers(StringBuilder builder)
	{
		return false;
	}
}
