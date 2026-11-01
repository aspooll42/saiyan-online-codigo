using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control;

public class LabeledRadioButton : Base, ITextContainer
{
	private readonly Label mLabel;

	private readonly RadioButton mRadioButton;

	public string? Text
	{
		get
		{
			return mLabel.Text;
		}
		set
		{
			mLabel.Text = value;
		}
	}

	public Color? TextPaddingDebugColor { get; set; }

	internal RadioButton RadioButton => mRadioButton;

	public LabeledRadioButton(Base parent)
		: base(parent)
	{
		base.MouseInputEnabled = true;
		SetSize(100, 20);
		mRadioButton = new RadioButton(this)
		{
			InheritParentEnablementProperties = true,
			IsTabable = false,
			KeyboardInputEnabled = false
		};
		mLabel = new Label(this)
		{
			TextAlign = (Pos.Left | Pos.CenterV),
			InheritParentEnablementProperties = true,
			IsTabable = false,
			KeyboardInputEnabled = false
		};
		mLabel.Clicked += delegate(Base control, MouseButtonState args)
		{
			mRadioButton.Press(control);
		};
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (mLabel.Height > mRadioButton.Height)
		{
			mRadioButton.Y = (mLabel.Height - mRadioButton.Height) / 2;
		}
		Align.PlaceRightBottom(mLabel, mRadioButton);
		SizeToChildren();
		base.Layout(skin);
	}

	protected override void RenderFocus(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (InputHandler.KeyboardFocus == this && base.IsTabable)
		{
			skin.DrawKeyboardHighlight(this, base.RenderBounds, 0);
		}
	}

	protected override bool OnKeySpace(bool down)
	{
		if (down)
		{
			mRadioButton.IsChecked = !mRadioButton.IsChecked;
		}
		return true;
	}

	public virtual void Select()
	{
		mRadioButton.IsChecked = true;
	}
}
