using System;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Interface.Game.DescriptionWindows.Components;

public class RowContainerComponent : ComponentBase
{
	private readonly JObject _keyValueRowLayout;

	private int _componentY;

	public RowContainerComponent(Base parent, string name)
		: base(parent, name)
	{
		KeyValueRowComponent keyValueRowComponent = new KeyValueRowComponent(this);
		LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
		_keyValueRowLayout = keyValueRowComponent.GetJson() ?? throw new Exception($"Failed to load {"KeyValueRowComponent"} layout for {base.Name}.");
		RemoveChild(keyValueRowComponent, dispose: true);
	}

	private void PositionComponent(ComponentBase component)
	{
		component.SetPosition(component.X, _componentY);
		_componentY += component.Height;
	}

	public override void CorrectWidth()
	{
		if (base.Parent == null)
		{
			return;
		}
		SetSize(base.Parent.InnerWidth, _componentY + base.Margin.Bottom);
		foreach (Base child in base.Children)
		{
			if (child is KeyValueRowComponent keyValueRowComponent)
			{
				keyValueRowComponent.SetSize(base.InnerWidth, keyValueRowComponent.Height);
				keyValueRowComponent.AlignValueRight(base.InnerWidth);
			}
		}
	}

	public KeyValueRowComponent AddKeyValueRow(string key, string value)
	{
		KeyValueRowComponent keyValueRowComponent = new KeyValueRowComponent(this, key, value);
		try
		{
			keyValueRowComponent.LoadJson(_keyValueRowLayout);
		}
		catch (Exception exception)
		{
			ApplicationContext.Context.Value?.Logger.LogError(exception, "An error occured while loading KeyValueRowComponent Json for " + base.Name);
		}
		keyValueRowComponent.SizeToChildren(resizeX: true, resizeY: false);
		PositionComponent(keyValueRowComponent);
		return keyValueRowComponent;
	}
}
