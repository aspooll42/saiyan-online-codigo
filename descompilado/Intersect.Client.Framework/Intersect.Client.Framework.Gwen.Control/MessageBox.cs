using System;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class MessageBox : WindowControl
{
	private readonly Button mButton;

	private readonly Label mLabel;

	public GwenEventHandler<EventArgs> Dismissed;

	public MessageBox(Base parent, string text, string caption = "")
		: base(parent, caption, modal: true)
	{
		base.DeleteOnClose = true;
		mLabel = new Label(_innerPanel);
		mLabel.Text = text;
		mLabel.Margin = Margin.Five;
		mLabel.Dock = Pos.Top;
		mLabel.TextAlign = Pos.Center;
		mButton = new Button(_innerPanel);
		mButton.Text = "OK";
		mButton.Clicked += OnClose;
		mButton.Clicked += DismissedHandler;
		mButton.Margin = Margin.Five;
		mButton.SetSize(50, 20);
		Align.Center(this);
	}

	private void DismissedHandler(Base control, EventArgs args)
	{
		if (Dismissed != null)
		{
			Dismissed(this, EventArgs.Empty);
		}
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		Align.PlaceDownLeft(mButton, mLabel, 10);
		Align.CenterHorizontally(mButton);
		_innerPanel.SizeToChildren();
		_innerPanel.Height += 10;
		SizeToChildren();
	}

	public void SetTextScale(float scale)
	{
		mLabel.SetTextScale(scale);
	}
}
