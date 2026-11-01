namespace Intersect.Client.Framework.Gwen.Control;

public interface ISmartAutoSizeToContents : IAutoSizeToContents
{
	bool IAutoSizeToContents.AutoSizeToContents
	{
		get
		{
			if (!AutoSizeToContentWidth)
			{
				return AutoSizeToContentHeight;
			}
			return true;
		}
		set
		{
			AutoSizeToContentWidth = value;
			AutoSizeToContentHeight = value;
		}
	}

	bool AutoSizeToContentWidth { get; set; }

	bool AutoSizeToContentHeight { get; set; }

	bool AutoSizeToContentWidthOnChildResize { get; set; }

	bool AutoSizeToContentHeightOnChildResize { get; set; }
}
