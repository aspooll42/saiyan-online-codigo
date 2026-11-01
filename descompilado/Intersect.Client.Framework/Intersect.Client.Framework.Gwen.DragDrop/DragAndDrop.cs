using System;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Platform;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen.DragDrop;

public static class DragAndDrop
{
	public static Package CurrentPackage;

	public static Intersect.Client.Framework.Gwen.Control.Base HoveredControl;

	private static Intersect.Client.Framework.Gwen.Control.Base sLastPressedControl;

	private static Point sLastPressedPos;

	private static int sMouseX;

	private static int sMouseY;

	private static Intersect.Client.Framework.Gwen.Control.Base sNewHoveredControl;

	public static Intersect.Client.Framework.Gwen.Control.Base SourceControl;

	public static bool IsDragging => CurrentPackage != null;

	private static bool OnDrop(int x, int y)
	{
		bool success = false;
		if (HoveredControl != null)
		{
			HoveredControl.DragAndDrop_HoverLeave(CurrentPackage);
			success = HoveredControl.DragAndDrop_HandleDrop(CurrentPackage, x, y);
		}
		SourceControl.DragAndDrop_EndDragging(success, x, y);
		CurrentPackage = null;
		SourceControl = null;
		return true;
	}

	private static bool ShouldStartDraggingControl(int x, int y)
	{
		if (sLastPressedControl == null)
		{
			return false;
		}
		int num = Math.Abs(x - sLastPressedPos.X) + Math.Abs(y - sLastPressedPos.Y);
		if (num < 5)
		{
			return false;
		}
		CurrentPackage = sLastPressedControl.DragAndDrop_GetPackage(sLastPressedPos.X, sLastPressedPos.Y);
		if (CurrentPackage == null)
		{
			sLastPressedControl = null;
			SourceControl = null;
			return false;
		}
		SourceControl = sLastPressedControl;
		InputHandler.MouseFocus = null;
		sLastPressedControl = null;
		if (!SourceControl.DragAndDrop_ShouldStartDrag())
		{
			SourceControl = null;
			CurrentPackage = null;
			return false;
		}
		SourceControl.DragAndDrop_StartDragging(CurrentPackage, sLastPressedPos.X, sLastPressedPos.Y);
		return true;
	}

	private static void UpdateHoveredControl(Intersect.Client.Framework.Gwen.Control.Base control, int x, int y)
	{
		sNewHoveredControl = control;
		if (HoveredControl == sNewHoveredControl)
		{
			return;
		}
		if (HoveredControl != null && HoveredControl != sNewHoveredControl)
		{
			HoveredControl.DragAndDrop_HoverLeave(CurrentPackage);
		}
		if (sNewHoveredControl == SourceControl)
		{
			sNewHoveredControl = null;
		}
		while (sNewHoveredControl != null && !sNewHoveredControl.DragAndDrop_CanAcceptPackage(CurrentPackage))
		{
			sNewHoveredControl = sNewHoveredControl.Parent;
			if (sNewHoveredControl == null)
			{
				Neutral.SetCursor(Cursors.No);
			}
		}
		HoveredControl = sNewHoveredControl;
		if (HoveredControl != null)
		{
			HoveredControl.DragAndDrop_HoverEnter(CurrentPackage, x, y);
		}
		sNewHoveredControl = null;
	}

	public static bool Start(Intersect.Client.Framework.Gwen.Control.Base control, Package package)
	{
		if (CurrentPackage != null)
		{
			return false;
		}
		CurrentPackage = package;
		SourceControl = control;
		return true;
	}

	public static bool OnMouseButton(Intersect.Client.Framework.Gwen.Control.Base hoveredControl, int x, int y, bool down)
	{
		if (!down)
		{
			sLastPressedControl = null;
			if (CurrentPackage == null)
			{
				return false;
			}
			OnDrop(x, y);
			return true;
		}
		if (hoveredControl == null)
		{
			return false;
		}
		if (!hoveredControl.DragAndDrop_Draggable())
		{
			return false;
		}
		sLastPressedPos = new Point(x, y);
		sLastPressedControl = hoveredControl;
		return false;
	}

	public static void OnMouseMoved(Intersect.Client.Framework.Gwen.Control.Base hoveredControl, int x, int y)
	{
		sMouseX = x;
		sMouseY = y;
		if (CurrentPackage != null || ShouldStartDraggingControl(x, y))
		{
			UpdateHoveredControl(hoveredControl, x, y);
			if (HoveredControl != null)
			{
				HoveredControl.DragAndDrop_Hover(CurrentPackage, x, y);
				Neutral.SetCursor(Cursors.Default);
				hoveredControl.Redraw();
			}
		}
	}

	public static void RenderOverlay(Canvas canvas, Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		if (CurrentPackage != null && CurrentPackage.DrawControl != null)
		{
			Point renderOffset = skin.Renderer.RenderOffset;
			skin.Renderer.AddRenderOffset(new Rectangle(sMouseX - SourceControl.X - CurrentPackage.HoldOffset.X, sMouseY - SourceControl.Y - CurrentPackage.HoldOffset.Y, 0, 0));
			CurrentPackage.DrawControl.DoRender(skin);
			skin.Renderer.RenderOffset = renderOffset;
		}
	}

	public static void ControlDeleted(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (SourceControl == control)
		{
			SourceControl = null;
			CurrentPackage = null;
			HoveredControl = null;
			sLastPressedControl = null;
		}
		if (sLastPressedControl == control)
		{
			sLastPressedControl = null;
		}
		if (HoveredControl == control)
		{
			HoveredControl = null;
		}
		if (sNewHoveredControl == control)
		{
			sNewHoveredControl = null;
		}
	}
}
