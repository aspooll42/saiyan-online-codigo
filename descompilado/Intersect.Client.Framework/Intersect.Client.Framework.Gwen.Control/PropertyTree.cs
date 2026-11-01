using Intersect.Client.Framework.Gwen.ControlInternal;

namespace Intersect.Client.Framework.Gwen.Control;

public class PropertyTree : TreeControl
{
	public PropertyTree(Base parent)
		: base(parent)
	{
	}

	public Properties Add(string label)
	{
		TreeNode treeNode = new PropertyTreeNode(this)
		{
			Text = label,
			Dock = Pos.Top,
		};
		Properties properties = new Properties(treeNode)
		{
			Dock = Pos.Top,
		};
		return properties;
	}
}
