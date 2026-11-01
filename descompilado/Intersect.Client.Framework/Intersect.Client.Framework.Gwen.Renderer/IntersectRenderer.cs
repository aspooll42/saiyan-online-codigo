using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Configuration;
using Intersect.Core;
using Microsoft.Extensions.Logging;

namespace Intersect.Client.Framework.Gwen.Renderer;

public class IntersectRenderer : Base, ICacheToTexture
{
	private bool mClipping;

	private FloatRect mClipRect;

	private Color mColor;

	private GameRenderer mRenderer;

	private IGameRenderTexture? mRenderTarget;

	private Dictionary<Intersect.Client.Framework.Gwen.Control.Base, IGameRenderTexture> m_RT;

	private Stack<IGameRenderTexture> m_Stack;

	private IGameRenderTexture m_RealRT;

	public override Color DrawColor
	{
		get
		{
			return Color.FromArgb(mColor.A, mColor.R, mColor.G, mColor.B);
		}
		set
		{
			mColor = new Color(value.A, value.R, value.G, value.B);
		}
	}

	public GameBlendModes BlendMode { get; set; } = GameBlendModes.None;

	public override ICacheToTexture Ctt => this;

	public IntersectRenderer(IGameRenderTexture renderTarget, GameRenderer renderer)
	{
		mRenderer = renderer;
		mRenderTarget = renderTarget;
	}

	public override Color PixelColor(IGameTexture texture, uint x, uint y, Color defaultColor)
	{
		if (texture == null)
		{
			return defaultColor;
		}
		return texture.GetPixel((int)x, (int)y);
	}

	public override Point MeasureText(IFont? font, int fontSize, string? text, float scale = 1f)
	{
		if (font == null || string.IsNullOrEmpty(text))
		{
			return default(Point);
		}
		Vector2 vector = mRenderer.MeasureText(text, font, fontSize, scale * base.Scale);
		return new Point((int)vector.X, (int)vector.Y);
	}

	public override void RenderText(IFont font, int fontSize, Point pos, string text, float scale = 1f)
	{
		pos = Translate(pos);
		FloatRect clipRect = new FloatRect(base.ClipRegion.X, base.ClipRegion.Y, base.ClipRegion.Width, base.ClipRegion.Height);
		clipRect.X = (int)Math.Round(clipRect.X * base.Scale);
		clipRect.Y = (int)Math.Round(clipRect.Y * base.Scale);
		clipRect.Width = (int)Math.Round(clipRect.Width * base.Scale);
		clipRect.Height = (int)Math.Round(clipRect.Height * base.Scale);
		mRenderer.DrawString(text, font, fontSize, pos.X, pos.Y, base.Scale * scale, mColor, worldPos: false, mRenderTarget, clipRect);
	}

	public void RenderTextPrecise(IFont font, int fontSize, float x, float y, string text, float scale = 1f)
	{
		FloatRect clipRect = new FloatRect(base.ClipRegion.X, base.ClipRegion.Y, base.ClipRegion.Width, base.ClipRegion.Height);
		clipRect.X = (int)Math.Round(clipRect.X * base.Scale);
		clipRect.Y = (int)Math.Round(clipRect.Y * base.Scale);
		clipRect.Width = (int)Math.Round(clipRect.Width * base.Scale);
		clipRect.Height = (int)Math.Round(clipRect.Height * base.Scale);
		mRenderer.DrawString(text, font, fontSize, (x + (float)base.RenderOffset.X) * base.Scale, (y + (float)base.RenderOffset.Y) * base.Scale, base.Scale * scale, mColor, worldPos: false, mRenderTarget, clipRect);
	}

	public override void DrawFilledRect(Rectangle targetRect)
	{
		FloatRect floatRect = new FloatRect(Translate(targetRect).X, Translate(targetRect).Y, Translate(targetRect).Width, Translate(targetRect).Height);
		if (mClipping)
		{
			FloatRect floatRect2 = new FloatRect(base.ClipRegion.X, base.ClipRegion.Y, base.ClipRegion.Width, base.ClipRegion.Height);
			floatRect2.X = (int)Math.Round(floatRect2.X * base.Scale);
			floatRect2.Y = (int)Math.Round(floatRect2.Y * base.Scale);
			floatRect2.Width = (int)Math.Round(floatRect2.Width * base.Scale);
			floatRect2.Height = (int)Math.Round(floatRect2.Height * base.Scale);
			float num = 0f;
			if (floatRect.X < floatRect2.X)
			{
				num = floatRect2.X - floatRect.X;
				floatRect.X += num;
				floatRect.Width -= num;
			}
			if (floatRect.X + floatRect.Width > floatRect2.X + floatRect2.Width)
			{
				num = floatRect.X + floatRect.Width - (floatRect2.X + floatRect2.Width);
				floatRect.Width -= num;
			}
			if (floatRect.Y < floatRect2.Y)
			{
				num = floatRect2.Y - floatRect.Y;
				floatRect.Y += num;
				floatRect.Height -= num;
			}
			if (floatRect.Y + floatRect.Height > floatRect2.Y + floatRect2.Height)
			{
				num = floatRect.Y + floatRect.Height - (floatRect2.Y + floatRect2.Height);
				floatRect.Height -= num;
			}
			if (floatRect.Width <= 0f || floatRect.Height <= 0f)
			{
				return;
			}
		}
		if (mRenderTarget == null)
		{
			mRenderer.DrawTexture(mRenderer.WhitePixel, 0f, 0f, 1f, 1f, floatRect.X, floatRect.Y, floatRect.Width, floatRect.Height, mColor, mRenderTarget, GameBlendModes.None, null, 0f, isUi: true);
		}
		else
		{
			mRenderer.DrawTexture(mRenderer.WhitePixel, 0f, 0f, 1f, 1f, floatRect.X, floatRect.Y, floatRect.Width, floatRect.Height, mColor, mRenderTarget, GameBlendModes.None, null, 0f, isUi: true);
		}
	}

	public override void DrawTexturedRect(IGameTexture? tex, Rectangle targetRect, Color clr, float u1 = 0f, float v1 = 0f, float u2 = 1f, float v2 = 1f)
	{
		if (tex == null)
		{
			return;
		}
		Rectangle rectangle = Translate(targetRect);
		FloatRect floatRect = new FloatRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
		u1 *= (float)tex.Width;
		v1 *= (float)tex.Height;
		u2 *= (float)tex.Width;
		v2 *= (float)tex.Height;
		if (mClipping)
		{
			FloatRect floatRect2 = new FloatRect(base.ClipRegion.X, base.ClipRegion.Y, base.ClipRegion.Width, base.ClipRegion.Height);
			floatRect2.X = (int)Math.Round(floatRect2.X * base.Scale);
			floatRect2.Y = (int)Math.Round(floatRect2.Y * base.Scale);
			floatRect2.Width = (int)Math.Round(floatRect2.Width * base.Scale);
			floatRect2.Height = (int)Math.Round(floatRect2.Height * base.Scale);
			float num = (float)targetRect.Height * base.Scale / Math.Abs(v2 - v1);
			float num2 = (float)targetRect.Width * base.Scale / Math.Abs(u2 - u1);
			float num3 = 0f;
			float num4 = 0f;
			if (floatRect.X < floatRect2.X)
			{
				num3 = floatRect2.X - floatRect.X;
				num4 = (int)Math.Floor(num3 / num2);
				floatRect.X += num3;
				floatRect.Width -= num3;
				u1 += num4;
			}
			if (floatRect.X + floatRect.Width > floatRect2.X + floatRect2.Width)
			{
				num3 = floatRect.X + floatRect.Width - (floatRect2.X + floatRect2.Width);
				num4 = (int)Math.Floor(num3 / num2);
				floatRect.Width -= num3;
				u2 -= num4;
			}
			if (floatRect.Y < floatRect2.Y)
			{
				num3 = floatRect2.Y - floatRect.Y;
				num4 = (int)Math.Floor(num3 / num);
				floatRect.Y += num3;
				floatRect.Height -= num3;
				v1 += num4;
			}
			if (floatRect.Y + floatRect.Height > floatRect2.Y + floatRect2.Height)
			{
				num3 = floatRect.Y + floatRect.Height - (floatRect2.Y + floatRect2.Height);
				num4 = (int)Math.Floor(num3 / num);
				floatRect.Height -= num3;
				v2 -= num4;
			}
			if (floatRect.Width <= 0f || floatRect.Height <= 0f)
			{
				return;
			}
		}
		if (mRenderTarget == null)
		{
			mRenderer.DrawTexture(tex, u1, v1, u2 - u1, v2 - v1, floatRect.X, floatRect.Y, floatRect.Width, floatRect.Height, mColor, mRenderTarget, BlendMode, null, 0f, isUi: true);
		}
		else
		{
			mRenderer.DrawTexture(tex, u1, v1, u2 - u1, v2 - v1, floatRect.X, floatRect.Y, floatRect.Width, floatRect.Height, mColor, mRenderTarget, BlendMode, null, 0f, isUi: true);
		}
	}

	public void DrawTextureRotated(IGameTexture texture, float x, float y, float w, float h, float degrees)
	{
		mRenderer.DrawTexture(texture, 0f, 0f, texture.Width, texture.Height, (x + (float)base.RenderOffset.X) * base.Scale, (y + (float)base.RenderOffset.Y) * base.Scale, w * base.Scale, h * base.Scale, mColor, mRenderTarget, BlendMode, null, degrees, isUi: true);
	}

	public void DrawTextureBand(IGameTexture texture, float sx, float sy, float sw, float sh, float x, float y, float w, float h)
	{
		float tx = (x + (float)base.RenderOffset.X) * base.Scale;
		float ty = (y + (float)base.RenderOffset.Y) * base.Scale;
		mRenderer.DrawTexture(texture, sx, sy, sw, sh, tx, ty, w * base.Scale, h * base.Scale, mColor, mRenderTarget, GameBlendModes.None, null, 0f, isUi: true);
	}

	public override void StartClip()
	{
		mClipping = true;
	}

	public override void EndClip()
	{
		mClipping = false;
	}

	private string RemoveExtension(string fileName)
	{
		int num = fileName.LastIndexOf(".");
		if (num >= 0)
		{
			fileName = fileName.Substring(0, num);
		}
		return fileName;
	}

	private string RemoveResourcesSlash(string fileName)
	{
		string resourcesDirectory = ClientConfiguration.ResourcesDirectory;
		if (fileName.ToLower().StartsWith(resourcesDirectory + "/") || fileName.ToLower().StartsWith(resourcesDirectory + "\\"))
		{
			string text = fileName;
			int num = resourcesDirectory.Length + 1;
			fileName = text.Substring(num, text.Length - num);
		}
		return fileName;
	}

	public void Initialize()
	{
		m_RT = new Dictionary<Intersect.Client.Framework.Gwen.Control.Base, IGameRenderTexture>();
		m_Stack = new Stack<IGameRenderTexture>();
	}

	public void ShutDown()
	{
		m_RT.Clear();
		if (m_Stack.Count > 0)
		{
			throw new InvalidOperationException("Render stack not empty");
		}
	}

	public void SetupCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		m_RealRT = mRenderTarget;
		m_Stack.Push(mRenderTarget);
		if (!m_RT.ContainsKey(control))
		{
			IEnumerable<string> values = m_RT.Keys.Select((Intersect.Client.Framework.Gwen.Control.Base key) => key.ParentQualifiedName);
			ApplicationContext.Context.Value?.Logger.LogError(control.ParentQualifiedName + " not found in the list of render targets: " + string.Join(", ", values));
		}
		mRenderTarget = m_RT[control];
		mRenderTarget.Begin();
		mRenderTarget.Clear(Color.Transparent);
	}

	public void FinishCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		mRenderTarget.End();
		mRenderTarget = m_Stack.Pop();
	}

	public void DrawCachedControlTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		IGameRenderTexture texture = m_RT[control];
		IGameRenderTexture gameRenderTexture = mRenderTarget;
		mRenderTarget = m_RealRT;
		mColor = Color.White;
		DrawTexturedRect(texture, control.Bounds, Color.White);
		mRenderTarget = gameRenderTexture;
	}

	public void CreateControlCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (!m_RT.ContainsKey(control))
		{
			m_RT[control] = mRenderer.CreateRenderTexture(control.Width, control.Height);
			m_RT[control].Clear(Color.Transparent);
		}
		IGameRenderTexture gameRenderTexture = m_RT[control];
	}

	public void DisposeCachedTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (m_RT.ContainsKey(control))
		{
			IGameRenderTexture gameRenderTexture = m_RT[control];
			gameRenderTexture.Dispose();
			m_RT.Remove(control);
		}
	}

	public void UpdateControlCacheTexture(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		throw new NotImplementedException();
	}

	public void SetRenderer(Base renderer)
	{
		throw new NotImplementedException();
	}
}
