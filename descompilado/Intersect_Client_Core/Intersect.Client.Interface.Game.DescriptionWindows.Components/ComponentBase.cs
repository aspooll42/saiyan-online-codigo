using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public class ComponentBase : ImagePanel
{
	public ComponentBase(Base parent, string name = "")
		: base(parent, name)
	{
	}

	public virtual void CorrectWidth()
	{
		if (base.Parent != null)
		{
			SetSize(base.Parent.InnerWidth, base.InnerHeight);
		}
	}
}
