using System;
using System.Numerics;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Skin;
using Intersect.Client.Interface.Data;
using Intersect.Client.Localization;
using Intersect.Client.Networking;

namespace Intersect.Client.Interface.Shared;

public class StatisticsPanel : Panel
{
	private readonly Label _labelFPS;

	private readonly Label _labelPing;

	public bool IsFPSEnabled
	{
		get
		{
			return _labelFPS.IsVisibleInParent;
		}
		set
		{
			_labelFPS.IsVisibleInParent = value;
		}
	}

	public bool IsPingEnabled
	{
		get
		{
			return _labelPing.IsVisibleInParent;
		}
		set
		{
			_labelPing.IsVisibleInParent = value;
		}
	}

	public StatisticsPanel(Intersect.Client.Framework.Gwen.Control.Base parent, string name = "StatisticsPanel")
		: base(parent, name)
	{
		base.Alignment = new Alignments[2]
		{
			Alignments.Top,
			Alignments.Right
		};
		base.BackgroundColor = new Color(127, 0, 0, 0);
		base.DockChildSpacing = new Padding(0, 4);
		base.Padding = new Padding(8, 4);
		base.RestrictToParent = true;
		IFont font = GameContentManager.Current.GetFont("rajdhanibold");
		_labelFPS = new Label(this, "_labelFPS")
		{
			AutoSizeToContents = false,
			Dock = Pos.Top,
			Font = font,
			FontSize = 10,
			Padding = new Padding(8, 4),
			Text = Strings.General.FpsLabelFormat.ToString(Graphics.Renderer.FPS),
			TextAlign = Pos.Center
		};
		Vector2 vector = Graphics.Renderer.MeasureText(Strings.General.FpsLabelFormat.ToString(10000), font, 10, 1f);
		_labelFPS.MinimumSize = vector;
		_labelPing = new Label(this, "_labelPing")
		{
			AutoSizeToContents = false,
			Dock = Pos.Top,
			Font = font,
			FontSize = 10,
			Padding = new Padding(8, 4),
			Text = Strings.General.PingLabelFormat.ToString(Intersect.Client.Networking.Network.Ping),
			TextAlign = Pos.Center
		};
		Vector2 vector2 = Graphics.Renderer.MeasureText(Strings.General.PingLabelFormat.ToString(10000), font, 10, 1f);
		_labelPing.MinimumSize = vector2;
		base.MinimumSize = new Vector2(16f + Math.Max(vector.X, vector2.X), 8f + Math.Max(vector.Y, vector2.Y));
		DelegateDataProvider<int> delegateDataProvider = new DelegateDataProvider<int>(() => Graphics.Renderer.FPS)
		{
			UserData = _labelFPS
		};
		AddDataProvider(delegateDataProvider);
		delegateDataProvider.ValueChanged += OnFPSChanged;
		DelegateDataProvider<int> delegateDataProvider2 = new DelegateDataProvider<int>(() => Intersect.Client.Networking.Network.Ping)
		{
			UserData = _labelPing
		};
		AddDataProvider(delegateDataProvider2);
		delegateDataProvider2.ValueChanged += OnPingChanged;
	}

	protected override void OnChildVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
	{
		base.OnChildVisibilityChanged(sender, eventArgs);
		base.IsVisibleInParent = IsFPSEnabled || IsPingEnabled;
	}

	private static void OnFPSChanged(IDataProvider dataProvider, ValueChangedEventArgs<int> args)
	{
		if (!(dataProvider is DataProvider<int> dataProvider2))
		{
			throw new InvalidOperationException("Received event from invalid data provider");
		}
		if (!(dataProvider2.UserData is Label label))
		{
			throw new InvalidOperationException("Data provider's user data is not a label as expected");
		}
		label.Text = Strings.General.FpsLabelFormat.ToString(args.Value);
	}

	private static void OnPingChanged(IDataProvider dataProvider, ValueChangedEventArgs<int> args)
	{
		if (!(dataProvider is DataProvider<int> dataProvider2))
		{
			throw new InvalidOperationException("Received event from invalid data provider");
		}
		if (!(dataProvider2.UserData is Label label))
		{
			throw new InvalidOperationException("Data provider's user data is not a label as expected");
		}
		label.Text = Strings.General.PingLabelFormat.ToString(args.Value);
	}

	protected override void Layout(Intersect.Client.Framework.Gwen.Skin.Base skin)
	{
		base.Layout(skin);
		SizeToChildren();
	}
}
