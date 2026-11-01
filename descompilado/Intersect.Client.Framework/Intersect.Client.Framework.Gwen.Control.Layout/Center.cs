namespace Intersect.Client.Framework.Gwen.Control.Layout;

public class Center : Positioner
{
	public Center(Base parent)
		: base(parent)
	{
		base.Pos = Pos.Center;
	}
}
