using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Input;
using Intersect.Client.Framework.Gwen.Renderer;
using Intersect.Client.Framework.Gwen.Skin;

namespace Intersect.Client.Framework.Gwen;

public static class ToolTip
{
	private static Intersect.Client.Framework.Gwen.Control.Base? _activeTooltipParent;

	public static bool IsActiveTooltip(Intersect.Client.Framework.Gwen.Control.Base tooltipComponent)
	{
		Intersect.Client.Framework.Gwen.Control.Base parent = tooltipComponent.Parent;
		if (parent == null)
		{
			return false;
		}
		return parent == _activeTooltipParent;
	}

	public static void Enable(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (control.Tooltip != null)
		{
			_activeTooltipParent = control;
		}
	}

	public static void Disable(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		if (_activeTooltipParent == control)
		{
			_activeTooltipParent = null;
		}
	}

	public static void ControlDeleted(Intersect.Client.Framework.Gwen.Control.Base control)
	{
		Disable(control);
	}

	public static void RenderToolTip(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		Intersect.Client.Framework.Gwen.Control.Base activeTooltipParent = _activeTooltipParent;
		if (activeTooltipParent?.Tooltip == null)
		{
			return;
		}
		Canvas canvas = activeTooltipParent.Canvas;
		if (canvas == null)
		{
			return;
		}
		Intersect.Client.Framework.Gwen.Renderer.Base renderer = skin.Renderer;
		Point renderOffset = renderer.RenderOffset;
		Point mousePosition = InputHandler.MousePosition;
		Rectangle bounds = activeTooltipParent.Tooltip.Bounds;
		Rectangle inside = Util.FloatRect((float)mousePosition.X - (float)bounds.Width * 0.5f, mousePosition.Y - bounds.Height, bounds.Width, bounds.Height);
		(Intersect.Client.Framework.Gwen.Control.Base, Intersect.Client.Framework.Gwen.Control.Base)? tuple = activeTooltipParent.FindMatchingNodes<ITextContainer>();
		if (tuple.HasValue)
		{
			(Intersect.Client.Framework.Gwen.Control.Base, Intersect.Client.Framework.Gwen.Control.Base) valueOrDefault = tuple.GetValueOrDefault();
			var (obj, _) = valueOrDefault;
			if (obj != null)
			{
				Intersect.Client.Framework.Gwen.Control.Base item = valueOrDefault.Item2;
				if (item != null)
				{
					Rectangle globalBounds = item.GlobalBounds;
					Rectangle globalBounds2 = obj.GlobalBounds;
					if (globalBounds2.Top > bounds.Height)
					{
						inside.Y = globalBounds2.Top - bounds.Height;
					}
					else
					{
						inside.Y = globalBounds2.Bottom;
					}
					inside.X = globalBounds2.X + (globalBounds2.Width - bounds.Width) / 2;
					float num = (float)globalBounds.X + (float)globalBounds.Width * 0.25f;
					if ((float)inside.X > num)
					{
						inside.X = globalBounds.X + (globalBounds.Width - bounds.Width) / 2;
					}
				}
			}
		}
		Rectangle offset = Util.ClampRectToRect(inside, canvas.Bounds);
		renderer.AddRenderOffset(offset);
		renderer.EndClip();
		skin.DrawToolTip(activeTooltipParent.Tooltip);
		activeTooltipParent.Tooltip.DoRender(skin);
		renderer.RenderOffset = renderOffset;
	}
}
