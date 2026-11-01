using Intersect.Framework;

namespace Intersect.Client.Framework.Gwen;

public static class OverflowBehaviorExtensions
{
	public static bool AllowsScrolling(this OverflowBehavior overflowBehavior)
	{
		return overflowBehavior switch
		{
			OverflowBehavior.Auto => true, 
			OverflowBehavior.Visible => false, 
			OverflowBehavior.Hidden => false, 
			OverflowBehavior.Scroll => true, 
			_ => throw Exceptions.UnreachableInvalidEnum(overflowBehavior), 
		};
	}

	public static OverflowBehavior SetAllowScrolling(this OverflowBehavior overflowBehavior, bool allowScrolling = true)
	{
		return overflowBehavior switch
		{
			OverflowBehavior.Auto => (!allowScrolling) ? OverflowBehavior.Hidden : OverflowBehavior.Auto, 
			OverflowBehavior.Visible => (!allowScrolling) ? OverflowBehavior.Visible : OverflowBehavior.Scroll, 
			OverflowBehavior.Hidden => allowScrolling ? OverflowBehavior.Scroll : OverflowBehavior.Hidden, 
			OverflowBehavior.Scroll => allowScrolling ? OverflowBehavior.Scroll : OverflowBehavior.Hidden, 
			_ => throw Exceptions.UnreachableInvalidEnum(overflowBehavior), 
		};
	}

	public static OverflowBehavior SetAutoHide(this OverflowBehavior overflowBehavior, bool autoHide = true)
	{
		return overflowBehavior switch
		{
			OverflowBehavior.Auto => (!autoHide) ? OverflowBehavior.Scroll : OverflowBehavior.Auto, 
			OverflowBehavior.Visible => OverflowBehavior.Visible, 
			OverflowBehavior.Hidden => OverflowBehavior.Hidden, 
			OverflowBehavior.Scroll => (!autoHide) ? OverflowBehavior.Scroll : OverflowBehavior.Auto, 
			_ => throw Exceptions.UnreachableInvalidEnum(overflowBehavior), 
		};
	}
}
