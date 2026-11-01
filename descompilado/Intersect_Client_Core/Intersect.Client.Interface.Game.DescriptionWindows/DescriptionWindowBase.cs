using System;
using System.Collections.Generic;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Game.DescriptionWindows.Components;

namespace Intersect.Client.Interface.Game.DescriptionWindows;

public class DescriptionWindowBase : ComponentBase
{
	private int _componentY;

	private readonly List<ComponentBase> _components = new List<ComponentBase>();

	public DescriptionWindowBase(Intersect.Client.Framework.Gwen.Control.Base parent, string name)
		: base(parent, name)
	{
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
	}

	protected override void OnPreDraw(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.OnPreDraw(skin);
		if (!base.IsOnTop)
		{
			BringToFront();
		}
	}

	public HeaderComponent AddHeader(string name = "DescriptionWindowHeader", bool loadLayout = true)
	{
		HeaderComponent headerComponent = new HeaderComponent(this, name);
		if (loadLayout)
		{
			headerComponent.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		}
		_components.Add(headerComponent);
		return headerComponent;
	}

	public DividerComponent AddDivider(string name = "DescriptionWindowDivider", bool loadLayout = true)
	{
		DividerComponent dividerComponent = new DividerComponent(this, name);
		if (loadLayout)
		{
			dividerComponent.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		}
		_components.Add(dividerComponent);
		return dividerComponent;
	}

	public DescriptionComponent AddDescription(string name = "DescriptionWindowDescription", bool loadLayout = true)
	{
		DescriptionComponent descriptionComponent = new DescriptionComponent(this, name);
		if (loadLayout)
		{
			descriptionComponent.LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		}
		_components.Add(descriptionComponent);
		return descriptionComponent;
	}

	public RowContainerComponent AddRowContainer(string name = "DescriptionWindowRowContainer")
	{
		RowContainerComponent rowContainerComponent = new RowContainerComponent(this, name);
		_components.Add(rowContainerComponent);
		return rowContainerComponent;
	}

	private void PositionComponent(ComponentBase component)
	{
		if (component != null)
		{
			Margin margin = component.Margin;
			component.SetPosition(component.X, _componentY + margin.Top);
			_componentY += margin.Top + component.Height + margin.Bottom;
		}
	}

	protected void FinalizeWindow()
	{
		_componentY = 0;
		SizeToChildren(resizeX: true, resizeY: false);
		foreach (ComponentBase component in _components)
		{
			component.CorrectWidth();
			PositionComponent(component);
		}
		SizeToChildren(resizeX: false);
		int num = 0;
		foreach (ComponentBase component2 in _components)
		{
			num = Math.Max(num, component2.Bottom + component2.Margin.Bottom);
		}
		int num2 = num + base.Padding.Bottom;
		if (num2 > base.Height)
		{
			SetSize(base.Width, num2);
		}
	}

	public void PositionToHoveredControl()
	{
		if (base.Canvas != null && InputHandler.HoveredControl != null)
		{
			Intersect.Client.Framework.Gwen.Control.Base hoveredControl = InputHandler.HoveredControl;
			Point point = hoveredControl.ToCanvas(new Point(0, 0));
			int num = point.X + hoveredControl.Width + 8;
			if (num + base.Width > base.Canvas.Width)
			{
				num = point.X - base.Width - 8;
			}
			num = Math.Clamp(num, 0, Math.Max(0, base.Canvas.Width - base.Width));
			int y = Math.Clamp(point.Y, 0, Math.Max(0, base.Canvas.Height - base.Height));
			MoveTo(num, y);
		}
	}
}
