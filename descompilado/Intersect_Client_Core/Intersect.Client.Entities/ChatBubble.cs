using System;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Entities;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.General;
using Intersect.Framework.Core;

namespace Intersect.Client.Entities;

public class ChatBubble
{
	private readonly IGameTexture? mBubbleTex;

	private readonly Entity? mOwner;

	private readonly long mRenderTimer;

	private readonly string? mSourceText;

	private Point[,]? mTexSections;

	private string[]? mText;

	private Rectangle mTextBounds;

	private Rectangle mTextureBounds;

	private float mInkTop;

	private int mLinePitch;

	private const int TailOverhang = 5;

	private const int InkPadding = 5;

	public ChatBubble(Entity owner, string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			mOwner = owner;
			mSourceText = text;
			mRenderTimer = Timing.Global.MillisecondsUtc + 5000;
			mBubbleTex = Globals.ContentManager.GetTexture(TextureType.Misc, "chatbubble.png");
		}
	}

	public bool Update()
	{
		if (mRenderTimer < Timing.Global.MillisecondsUtc)
		{
			return false;
		}
		return true;
	}

	public float Draw(float yoffset = 0f)
	{
		if (mText == null)
		{
			string? text = mSourceText;
			if (text != null && text.Trim().Length > 0 && Graphics.ChatBubbleFont != null)
			{
				mText = Text.WrapText(mSourceText, 200, Graphics.ChatBubbleFont, Graphics.ChatBubbleFontSize, Graphics.Renderer ?? throw new InvalidOperationException("No renderer"));
			}
		}
		if (mText == null || Graphics.Renderer == null || mOwner == null)
		{
			return 0f;
		}
		int num = (int)Math.Ceiling(mOwner.Origin.X);
		int num2 = (int)Math.Ceiling(mOwner.GetLabelLocation(LabelType.ChatBubble));
		if (mTextureBounds.Width == 0)
		{
			float num3 = float.MaxValue;
			float num4 = float.MinValue;
			string[]? array = mText;
			for (int num5 = ((array != null) ? array.Length : 0) - 1; num5 > -1; num5--)
			{
				Vector2 vector = TexteMonde.Mesurer(mText[num5], Graphics.ChatBubbleFont, TexteMonde.Taille(Graphics.ChatBubbleFontSize));
				if (vector.X + 16f > (float)mTextureBounds.Width)
				{
					mTextureBounds.Width = (int)vector.X + 16;
				}
				if (vector.X > (float)mTextBounds.Width)
				{
					mTextBounds.Width = (int)vector.X;
				}
				(float Top, float Bottom) tuple = Graphics.Renderer.MeasureTextInk(mText[num5], Graphics.ChatBubbleFont, Graphics.ChatBubbleFontSize, 1f);
				float item = tuple.Top;
				float item2 = tuple.Bottom;
				num3 = Math.Min(num3, item);
				num4 = Math.Max(num4, item2);
			}
			if (num3 > num4)
			{
				num3 = 0f;
				num4 = 0f;
			}
			mInkTop = num3 - 1f;
			int num6 = (int)Math.Ceiling(num4 - num3) + 2;
			mLinePitch = num6 + 3;
			string[]? array2 = mText;
			int num7 = ((array2 == null) ? 1 : array2.Length);
			int num8 = num6 + (num7 - 1) * mLinePitch;
			mTextBounds.Height = num8;
			mTextureBounds.Height = 6 + num8 + 5 + 1 + 5;
			if (mTextureBounds.Width < 48)
			{
				mTextureBounds.Width = 48;
			}
			if (mTextureBounds.Height < 32)
			{
				mTextureBounds.Height = 32;
			}
			mTextureBounds.Width = (int)(Math.Round((double)mTextureBounds.Width / 8.0) * 8.0);
			mTextureBounds.Height = (int)(Math.Ceiling((double)mTextureBounds.Height / 8.0) * 8.0);
			if (mTextureBounds.Width / 8 % 2 != 0)
			{
				mTextureBounds.Width += 8;
			}
			mTexSections = new Point[mTextureBounds.Width / 8, mTextureBounds.Height / 8];
			for (int i = 0; i < mTextureBounds.Width / 8; i++)
			{
				for (int j = 0; j < mTextureBounds.Height / 8; j++)
				{
					switch (i)
					{
					case 0:
						mTexSections[i, j].X = 0;
						break;
					case 1:
						mTexSections[i, j].X = 1;
						break;
					default:
						if (i == mTextureBounds.Width / 16 - 1)
						{
							mTexSections[i, j].X = 3;
						}
						else if (i == mTextureBounds.Width / 16)
						{
							mTexSections[i, j].X = 4;
						}
						else if (i == mTextureBounds.Width / 8 - 1)
						{
							mTexSections[i, j].X = 7;
						}
						else if (i == mTextureBounds.Width / 8 - 2)
						{
							mTexSections[i, j].X = 6;
						}
						else
						{
							mTexSections[i, j].X = 2;
						}
						break;
					}
					switch (j)
					{
					case 0:
						mTexSections[i, j].Y = 0;
						continue;
					case 1:
						mTexSections[i, j].Y = 1;
						continue;
					}
					if (j == mTextureBounds.Height / 8 - 1)
					{
						mTexSections[i, j].Y = 3;
					}
					else if (j == mTextureBounds.Height / 8 - 2)
					{
						mTexSections[i, j].Y = 2;
					}
					else
					{
						mTexSections[i, j].Y = 1;
					}
				}
			}
		}
		if (mBubbleTex != null && mText != null && mTexSections != null)
		{
			for (int k = 0; k < mTextureBounds.Width / 8; k++)
			{
				for (int l = 0; l < mTextureBounds.Height / 8; l++)
				{
					Graphics.Renderer.DrawTexture(mBubbleTex, mTexSections[k, l].X * 8, mTexSections[k, l].Y * 8, 8f, 8f, num - mTextureBounds.Width / 2 + k * 8, (float)(num2 - mTextureBounds.Height) - yoffset + (float)(l * 8), 8f, 8f, Color.White);
				}
			}
			int num9 = mTextureBounds.Height - 5;
			float num10 = (float)(num9 - mTextBounds.Height) / 2f;
			for (int num11 = mText.Length - 1; num11 > -1; num11--)
			{
				Vector2 vector2 = TexteMonde.Mesurer(mText[num11], Graphics.ChatBubbleFont, TexteMonde.Taille(Graphics.ChatBubbleFontSize));
				TexteMonde.Ecrire(mText[num11], Graphics.ChatBubbleFont, TexteMonde.Taille(Graphics.ChatBubbleFontSize), (float)num - (float)mTextureBounds.Width / 2f + ((float)mTextureBounds.Width - vector2.X) / 2f, (float)(num2 - mTextureBounds.Height) - yoffset + num10 - mInkTop + (float)(num11 * mLinePitch), Color.FromArgb(CustomColors.Chat.ChatBubbleText.ToArgb()), Color.FromArgb(CustomColors.Chat.ChatBubbleTextOutline.ToArgb()));
			}
		}
		yoffset += (float)mTextureBounds.Height;
		return yoffset;
	}
}
