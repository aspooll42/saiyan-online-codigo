using Intersect.Client.Framework.Gwen.Control;

namespace Intersect.Client.Framework.Gwen;

public static class Align
{
	public static void AlignLeft(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			control.SetPosition(parent.Padding.Left + control.Margin.Left + control.AlignmentPadding.Left, control.Y);
		}
	}

	public static void AlignRight(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			int num = control.Width + control.Margin.Right + control.AlignmentPadding.Right + parent.Padding.Right;
			control.SetPosition(parent.Width - num, control.Y);
		}
	}

	public static void AlignTop(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			control.SetPosition(control.X, control.Margin.Top + parent.Padding.Top + control.AlignmentPadding.Top);
		}
	}

	public static void AlignBottom(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			int num = control.Height + control.Margin.Bottom + control.AlignmentPadding.Bottom + parent.Padding.Bottom;
			control.SetPosition(control.X, parent.Height - num);
		}
	}

	public static void Center(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			Padding padding = parent.Padding;
			int num = padding.Left + padding.Right;
			int num2 = padding.Top + padding.Bottom;
			Point point = new Point(control.AlignmentPadding.Left, control.AlignmentPadding.Top);
			point.X += padding.Left;
			point.Y += padding.Top;
			int num3 = parent.Height - num2;
			int num4 = parent.Width - num;
			Point point2 = new Point(num4 - control.Width, num3 - control.Height);
			point2 /= 2f;
			control.SetPosition(point + point2);
		}
	}

	public static void CenterHorizontally(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			Padding padding = parent.Padding;
			int num = padding.Left + padding.Right;
			Point point = new Point(control.AlignmentPadding.Left, control.Y);
			point.X += padding.Left;
			int num2 = parent.Width - num;
			Point point2 = new Point(num2 - control.Width, 0);
			point2 /= 2f;
			control.SetPosition(point + point2);
		}
	}

	public static void CenterVertically(Base control)
	{
		Base parent = control.Parent;
		if (parent != null)
		{
			Padding padding = parent.Padding;
			int num = padding.Top + padding.Bottom;
			Point point = new Point(control.X, control.AlignmentPadding.Top);
			point.Y += padding.Top;
			int num2 = parent.Height - num;
			Point point2 = new Point(0, num2 - control.Height);
			point2 /= 2f;
			control.SetPosition(point + point2);
		}
	}

	public static void PlaceDownLeft(Base control, Base anchor, int spacing = 0)
	{
		control.SetPosition(anchor.X, anchor.Bottom + spacing);
	}

	public static void PlaceRightBottom(Base control, Base anchor, int spacing = 0)
	{
		control.SetPosition(anchor.Right + spacing, anchor.Y - control.Height + anchor.Height);
	}
}
