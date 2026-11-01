using System;
using Intersect.Client.Framework.Content;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.General;
using Intersect.Client.Networking;
using Intersect.Framework.Core;

namespace Intersect.Client.Interface.Game;

internal class PictureWindow
{
	private enum PictureSize
	{
		Original,
		FullScreen,
		HalfScreen,
		StretchToFit
	}

	private Canvas mGameCanvas;

	private ImagePanel mPicture;

	public string Picture { get; private set; }

	public int Size { get; private set; }

	public bool Clickable { get; private set; }

	public PictureWindow(Canvas gameCanvas)
	{
		mGameCanvas = gameCanvas;
		mPicture = new ImagePanel(gameCanvas);
		mPicture.Clicked += MPicture_Clicked;
	}

	public void Setup(string picture, int size, bool clickable)
	{
		Picture = picture;
		Size = size;
		Clickable = clickable;
		mPicture.Texture = Globals.ContentManager.GetTexture(TextureType.Image, picture);
		if (mPicture.Texture != null)
		{
			mPicture.SetSize(mPicture.Texture.Width, mPicture.Texture.Height);
			Align.Center(mPicture);
			switch (size)
			{
			case 3:
				mPicture.SetSize(mGameCanvas.Width, mGameCanvas.Height);
				Align.Center(mPicture);
				break;
			default:
			{
				int num = 1;
				if (size == 2)
				{
					num = 2;
				}
				float num2 = (float)mPicture.Width / (float)mPicture.Height;
				bool flag = true;
				if ((float)mGameCanvas.Width < (float)mGameCanvas.Height * num2)
				{
					flag = false;
				}
				if (flag)
				{
					int height = mGameCanvas.Height;
					float num3 = (float)mGameCanvas.Height * num2;
					mPicture.SetSize((int)(num3 / (float)num), height / num);
					Align.Center(mPicture);
				}
				else
				{
					int width = mGameCanvas.Width;
					float num4 = (float)width / num2;
					mPicture.SetSize(width / num, (int)(num4 / (float)num));
					Align.Center(mPicture);
				}
				break;
			}
			case 0:
				break;
			}
			mPicture.BringToFront();
			mPicture.Show();
		}
		else
		{
			Close();
		}
	}

	private void MPicture_Clicked(Base sender, MouseButtonState arguments)
	{
		if (Clickable)
		{
			Close();
		}
	}

	public void Close()
	{
		if (Picture != null)
		{
			PacketSender.SendClosePicture(Globals.Picture?.EventId ?? Guid.Empty);
			Globals.Picture = null;
			Picture = null;
			mPicture.Hide();
		}
	}

	public void Update()
	{
		if (Picture != null && Globals.Picture != null && Globals.Picture.HideTime > 0 && Timing.Global.Milliseconds > Globals.Picture.ReceiveTime + Globals.Picture.HideTime)
		{
			Close();
		}
	}
}
