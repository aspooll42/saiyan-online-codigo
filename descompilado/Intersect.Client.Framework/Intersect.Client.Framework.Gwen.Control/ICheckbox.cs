using System;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Framework.Eventing;

namespace Intersect.Client.Framework.Gwen.Control;

public interface ICheckbox
{
	bool IsChecked { get; set; }

	event EventHandler<ICheckbox, EventArgs>? Checked;

	event EventHandler<ICheckbox, EventArgs>? Unchecked;

	event EventHandler<ICheckbox, ValueChangedEventArgs<bool>>? CheckChanged;
}
