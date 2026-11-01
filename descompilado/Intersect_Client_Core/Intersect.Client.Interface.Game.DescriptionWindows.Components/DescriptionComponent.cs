using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public class DescriptionComponent : ComponentBase
{
	private static readonly Color BodyColor = new Color(255, 196, 204, 220);

	private readonly RichLabel _description;

	public DescriptionComponent(Base parent, string name)
		: base(parent, name)
	{
		_description = new RichLabel(this, "Description");
		_description.SizeChanged += delegate
		{
			SizeToChildren();
		};
	}

	public void AddText(string description, Color? color = null)
	{
		_description.AddText(description, color ?? BodyColor);
		_description.SizeToChildren(resizeX: false);
		SizeToChildren(resizeX: false);
	}

	public void AddLineBreak()
	{
		_description.AddLineBreak();
	}

	public override void CorrectWidth()
	{
		base.CorrectWidth();
		if (base.Parent != null)
		{
			Margin margin = _description.Margin;
			_description.SetSize(base.InnerWidth - margin.Right, base.MaximumSize.Y);
			_description.ForceImmediateRebuild();
			_description.SizeToChildren(resizeX: false);
			_description.SetSize(_description.Width, _description.Height + margin.Bottom);
			SizeToChildren(resizeX: false);
		}
	}
}
