using Intersect.Client.Framework.Gwen.Skin.Texturing;

namespace Intersect.Client.Framework.Gwen.Skin;

public struct SkinTextures
{
	public struct _Panel
	{
		public Bordered Control;

		public Bordered Normal;

		public Bordered Bright;

		public Bordered Dark;

		public Bordered Highlight;
	}

	public struct _Window
	{
		public Bordered Normal;

		public Bordered Inactive;

		public Bordered ActiveTitleBar;

		public Bordered InactiveTitleBar;

		public _Input._Button CloseButton;
	}

	public struct _FillableButton
	{
		public Single Box;

		public Single Fill;
	}

	public struct _CheckBox
	{
		public struct _Baked
		{
			public Single Normal;

			public Single Checked;
		}

		public _Baked Active_Baked;

		public _Baked Disabled_Baked;

		public _FillableButton Default;

		public _FillableButton Active;

		public _FillableButton Hovered;

		public _FillableButton Disabled;
	}

	public struct _RadioButton
	{
		public struct _Baked
		{
			public Single Normal;

			public Single Checked;
		}

		public _Baked Active_Baked;

		public _Baked Disabled_Baked;

		public _FillableButton Default;

		public _FillableButton Active;

		public _FillableButton Hovered;

		public _FillableButton Disabled;
	}

	public struct _TextBox
	{
		public Bordered Normal;

		public Bordered Focus;

		public Bordered Disabled;
	}

	public struct _Tree
	{
		public Bordered Background;

		public Single Minus;

		public Single Plus;
	}

	public struct _ProgressBar
	{
		public Bordered Back;

		public Bordered Front;
	}

	public struct _Scroller
	{
		public struct _Button
		{
			public Bordered[] Normal;

			public Bordered[] Hovered;

			public Bordered[] Active;

			public Bordered[] Disabled;
		}

		public Bordered TrackH;

		public Bordered TrackV;

		public _Input._Button BarH;

		public _Input._Button BarV;

		public _Button Button;
	}

	public struct _Menu
	{
		public Single RightArrow;

		public Single Check;

		public Bordered Strip;

		public Bordered Background;

		public Bordered BackgroundWithMargin;

		public Bordered Hovered;
	}

	public struct _Input
	{
		public struct _Button
		{
			public IAtlasDrawable Normal;

			public IAtlasDrawable Disabled;

			public IAtlasDrawable Hovered;

			public IAtlasDrawable Active;
		}

		public struct _ComboBox
		{
			public struct _Button
			{
				public Single Normal;

				public Single Hover;

				public Single Down;

				public Single Disabled;
			}

			public Bordered Normal;

			public Bordered Hover;

			public Bordered Down;

			public Bordered Disabled;

			public _Button Button;
		}

		public struct _Slider
		{
			public struct _SliderSource
			{
				public Bordered Normal;

				public Bordered Hover;

				public Bordered Active;

				public Bordered Disabled;
			}

			public _SliderSource H;

			public _SliderSource V;
		}

		public struct _ListBox
		{
			public Bordered Background;

			public Bordered Hovered;

			public Bordered EvenLine;

			public Bordered OddLine;

			public Bordered EvenLineSelected;

			public Bordered OddLineSelected;
		}

		public struct _UpDown
		{
			public struct _Up
			{
				public Single Normal;

				public Single Hover;

				public Single Down;

				public Single Disabled;
			}

			public struct _Down
			{
				public Single Normal;

				public Single Hover;

				public Single Down;

				public Single Disabled;
			}

			public _Up Up;

			public _Down Down;
		}

		public _Button Button;

		public _ComboBox ComboBox;

		public _Slider Slider;

		public _ListBox ListBox;

		public _UpDown UpDown;
	}

	public struct _Tab
	{
		public struct _Bottom
		{
			public Bordered Inactive;

			public Bordered Active;
		}

		public struct _Top
		{
			public Bordered Inactive;

			public Bordered Active;
		}

		public struct _Left
		{
			public Bordered Inactive;

			public Bordered Active;
		}

		public struct _Right
		{
			public Bordered Inactive;

			public Bordered Active;
		}

		public _Bottom Bottom;

		public _Top Top;

		public _Left Left;

		public _Right Right;

		public Bordered Control;

		public Bordered HeaderBar;
	}

	public struct _CategoryList
	{
		public Bordered Outer;

		public Bordered Inner;

		public Bordered Header;
	}

	public Bordered StatusBar;

	public Bordered Selection;

	public Bordered Shadow;

	public Bordered Tooltip;

	public _Panel Panel;

	public _Window Window;

	public _CheckBox CheckBox;

	public _RadioButton RadioButton;

	public _TextBox TextBox;

	public _Tree Tree;

	public _ProgressBar ProgressBar;

	public _Scroller Scroller;

	public _Menu Menu;

	public _Input Input;

	public _Tab Tab;

	public _CategoryList CategoryList;
}
