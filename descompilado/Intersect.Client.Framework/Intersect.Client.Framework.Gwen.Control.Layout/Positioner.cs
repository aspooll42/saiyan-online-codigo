using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class Positioner : Base
{
	private Pos mPos;

	public Pos Pos
	{
		get
		{
			return mPos;
		}
		set
		{
			mPos = value;
		}
	}

	public Positioner(Base parent)
		: base(parent)
	{
		Pos = Pos.Left | Pos.Top;
	}

	protected override void DoPostlayout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		foreach (Base child in base.Children)
		{
			child.Position(mPos);
		}
	}
}
