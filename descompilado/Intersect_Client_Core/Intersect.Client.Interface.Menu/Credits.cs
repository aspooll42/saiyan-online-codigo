using System.Collections.Generic;
using Intersect.Client.Framework.Gwen;

namespace Intersect.Client.Interface.Menu;

internal class Credits
{
	public struct CreditsLine
	{
		public string Text;

		public string Font;

		public int Size;

		public string Alignment;

		public Color TextColor;

		public Alignments GetAlignment()
		{
			string alignment = Alignment;
			if (!(alignment == "center"))
			{
				if (alignment == "right")
				{
					return Alignments.Right;
				}
				return Alignments.Left;
			}
			return Alignments.CenterH;
		}
	}

	public List<CreditsLine> Lines = new List<CreditsLine>();
}
