using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Renderer;

namespace Intersect.Client.Framework.Gwen.Skin.Texturing;

public struct Single : IAtlasDrawable
{
	private readonly IGameTexture mTexture;

	private readonly float[] mUv;

	private readonly int mWidth;

	private readonly int mHeight;

	public Single(IGameTexture texture, float x, float y, float w, float h)
	{
		mTexture = texture;
		float num = 1f;
		float num2 = 1f;
		if (mTexture != null)
		{
			num = mTexture.Width;
			num2 = mTexture.Height;
		}
		mUv = new float[4];
		mUv[0] = x / num;
		mUv[1] = y / num2;
		mUv[2] = (x + w) / num;
		mUv[3] = (y + h) / num2;
		mWidth = (int)w;
		mHeight = (int)h;
	}

	public void Draw(Intersect.Client.Framework.Gwen.Renderer.Base render, Rectangle r)
	{
		Draw(render, r, Color.White);
	}

	public void Draw(Intersect.Client.Framework.Gwen.Renderer.Base render, Rectangle r, Color col)
	{
		if (mTexture != null)
		{
			render.DrawColor = col;
			render.DrawTexturedRect(mTexture, r, col, mUv[0], mUv[1], mUv[2], mUv[3]);
		}
	}

	public void DrawCenter(Intersect.Client.Framework.Gwen.Renderer.Base render, Rectangle r)
	{
		if (mTexture != null)
		{
			DrawCenter(render, r, Color.White);
		}
	}

	public void DrawCenter(Intersect.Client.Framework.Gwen.Renderer.Base render, Rectangle r, Color col)
	{
		r.X += (int)((double)(r.Width - mWidth) * 0.5);
		r.Y += (int)((double)(r.Height - mHeight) * 0.5);
		r.Width = mWidth;
		r.Height = mHeight;
		Draw(render, r, col);
	}
}
