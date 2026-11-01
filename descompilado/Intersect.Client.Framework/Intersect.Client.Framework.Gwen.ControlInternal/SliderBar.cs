using System.Numerics;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.ControlInternal;

public class SliderBar : Dragger
{
	private Vector2? _anchorAxis;

	private Orientation _orientation;

	public Vector2? AnchorAxis
	{
		get
		{
			return _anchorAxis;
		}
		set
		{
			SetAndDoIfChanged(ref _anchorAxis, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate);
		}
	}

	public Orientation Orientation
	{
		get
		{
			return _orientation;
		}
		set
		{
			SetAndDoIfChanged(ref _orientation, value, Intersect.Client.Framework.Gwen.Control.Base.Invalidate);
		}
	}

	public SliderBar(Slider parent, string? name = null)
		: base(parent, name)
	{
		base.Target = this;
		base.RestrictToParent = true;
		Orientation = parent.Orientation;
	}

	protected override void Render(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		skin.DrawSliderButton(this);
	}

	public override bool SetBounds(int x, int y, int width, int height)
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = base.Parent;
		if (parent != null)
		{
			Vector2? anchorAxis = AnchorAxis;
			if (anchorAxis.HasValue)
			{
				Vector2 valueOrDefault = anchorAxis.GetValueOrDefault();
				switch (Orientation)
				{
				case Orientation.LeftToRight:
				case Orientation.RightToLeft:
					y = (int)(valueOrDefault.Y * (float)parent.Height - (float)base.Height) - 1;
					break;
				case Orientation.TopToBottom:
				case Orientation.BottomToTop:
					x = (int)(valueOrDefault.X * (float)parent.Width - (float)base.Width) - 1;
					break;
				}
			}
		}
		return base.SetBounds(x, y, width, height);
	}
}
