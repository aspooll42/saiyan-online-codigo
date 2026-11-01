using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Localization;
using Intersect.Localization;

namespace Intersect.Client.Interface.Shared;

public class AlertWindow : InputBox
{
	private static readonly HashSet<AlertWindow> _instances = new HashSet<AlertWindow>();

	private AlertWindow(string title, string message, AlertType alertType, object? userData = null, GwenEventHandler<InputSubmissionEventArgs>? handleSubmit = null, GwenEventHandler<EventArgs>? handleCancel = null, InputType inputType = InputType.Okay)
		: base("AlertWindow", title, message, inputType, handleSubmit, handleCancel, userData)
	{
	}

	protected override void OnCanceled(Base sender, EventArgs args)
	{
		Remove();
	}

	protected override void OnSubmitted(Base sender, InputSubmissionEventArgs args)
	{
		Remove();
	}

	private void Remove()
	{
		_instances.Remove(this);
		Base parent = base.Parent;
		if (parent != null)
		{
			parent.RemoveChild(this, dispose: true);
		}
		else
		{
			Dispose();
		}
	}

	public static void OpenPendingAlertWindowsFrom(IProducerConsumerCollection<Alert> source)
	{
		Alert item;
		while (source.TryTake(out item))
		{
			Open(item.Message, item.Title, item.Type);
		}
	}

	public static void Open(string message, string? title = null, AlertType alertType = AlertType.Unknown, object? userData = null, GwenEventHandler<InputSubmissionEventArgs>? handleSubmit = null, GwenEventHandler<EventArgs>? handleCancel = null, InputType inputType = InputType.Okay)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			if (Strings.Alerts.Titles.TryGetValue(alertType, out LocalizedString value))
			{
				title = value;
			}
			if (string.IsNullOrWhiteSpace(title))
			{
				title = Strings.Alerts.FallbackTitle;
			}
		}
		_instances.Add(new AlertWindow(string.IsNullOrWhiteSpace(title) ? Strings.Errors.Title.ToString() : title, message, alertType, userData, handleSubmit, handleCancel, inputType));
	}

	public static void CloseAll()
	{
		AlertWindow[] array = _instances.ToArray();
		_instances.Clear();
		AlertWindow[] array2 = array;
		foreach (AlertWindow alertWindow in array2)
		{
			alertWindow.Remove();
		}
	}
}
